using CadastroCliente.Models.DTOs;
using CadastroCliente.Models.DTOs.Dashboard;
using CadastroCliente.Models.DTOs.Simulation;
using CadastroCliente.Models.Entities;
using CadastroCliente.Repositories;
using CadastroCliente.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;

public class SimulationService : ISimulationService
{
    private readonly ISimulationRepository _simRepository;
    private readonly IRateTableRepository _rateTableRepository;
    private readonly ILogger<SimulationService> _logger;
    private readonly IInstallmentRepository _installmentRepository;

    private const decimal IOF_DIARIO = 0.000082m; // 0.0082% ao dia
    private const decimal IOF_ADICIONAL = 0.0038m; // 0.38% ao mês

    public SimulationService(ISimulationRepository simRepository, IRateTableRepository rateTableRepository, ILogger<SimulationService> logger, IInstallmentRepository installmentRepository)
    {
        _simRepository = simRepository;
        _rateTableRepository = rateTableRepository;
        _logger = logger;
        _installmentRepository = installmentRepository;
    }

    public async Task<SimulationResultDto> CalculateAsync(SimulationInputDto input)
    {
        // Validar Tabela de Taxas
        var tableRules = await _rateTableRepository.GetByNameAsync(input.RateTable);
        if (tableRules == null) throw new ArgumentException("Tabela de taxas inválida.");

        // Verifica se a taxa informada existe na tabela
        var validRates = tableRules.GetRatesList();
        if (!validRates.Contains(input.Rate)) throw new ArgumentException($"Taxa {input.Rate}% não permitida para esta tabela.");

        var validTerms = tableRules.GetTermsList();
        if (!validTerms.Contains(input.Term)) throw new ArgumentException($"Prazo {input.Term} meses não permitido.");

        // Variáveis de cálculo
        decimal valorSolicitado = input.RequestedAmount;
        decimal taxaMensal = input.Rate / 100m;
        double rateDouble = (double)(input.Rate / 100m);
        int prazo = input.Term;

        DateTime dataLiberacao = DateTime.Today.AddHours(12);

        int frequencia = input.FrequencyDays > 0 ? input.FrequencyDays : 30;

        int carenciaDiasInput = input.GracePeriodDays;

        DateTime dataPrimeiroVencimento = dataLiberacao.AddDays(carenciaDiasInput);

        if (carenciaDiasInput == 0)
        {
            dataPrimeiroVencimento = dataLiberacao.AddDays(frequencia);
        }

        decimal fatorIOF = IOF_ADICIONAL + (IOF_DIARIO * 365);
        decimal valorBaseDivida;
        decimal totalIOF;
        decimal releasedAmount;

        if (input.FinanceIOF)
        {
            valorBaseDivida = valorSolicitado / (1 - fatorIOF);
            totalIOF = valorBaseDivida - valorSolicitado;
            releasedAmount = valorSolicitado;
        }

        else
        {
            releasedAmount = valorSolicitado - (valorSolicitado * fatorIOF);
            valorBaseDivida = valorSolicitado;
            totalIOF = valorSolicitado * fatorIOF;
        }

        // Tratamento de Carência (Juros sobre dias excedentes)
        decimal saldoParaPrice = valorBaseDivida;
        decimal valorJurosCarencia = 0;

        double rateDailyDouble = Math.Pow(1 + rateDouble, 1.0 / 30.0) - 1;
        decimal rateDaily = (decimal)(rateDailyDouble * 100);

        int gracePeriodDays = input.GracePeriodDays; 

        if (gracePeriodDays > 0)
        {
            valorJurosCarencia = saldoParaPrice * (rateDaily / 100m) * gracePeriodDays;

            valorJurosCarencia = Math.Round(valorJurosCarencia, 2);

            saldoParaPrice += valorJurosCarencia;
        }

        // Cálculo da PMT (Prestação - Tabela Price)
        double i = (double)taxaMensal;
        double fatorPrice = (Math.Pow(1 + i, prazo) * i) / (Math.Pow(1 + i, prazo) - 1);

        decimal valorParcela = saldoParaPrice * (decimal)fatorPrice;
        valorParcela = Math.Round(valorParcela, 2, MidpointRounding.AwayFromZero);

        var parcelas = GerarTabelaPrice(
            saldoParaPrice,
            valorParcela,
            taxaMensal,
            prazo,
            dataLiberacao, 
            frequencia,
            dataPrimeiroVencimento 
        );

        return new SimulationResultDto
        {
            ReleasedAmount = Math.Round(releasedAmount, 2),
            TotalIOF = Math.Round(totalIOF, 2),
            GracePeriodInterest = Math.Round(valorJurosCarencia, 2),
            TotalFinancedAmount = Math.Round(saldoParaPrice, 2),

            InstallmentAmount = valorParcela,
            ContractValue = Math.Round(valorParcela * prazo, 2),

            Product = input.Product,
            RateTable = input.RateTable,
            Rate = input.Rate,
            Term = input.Term,
            IOFFinanced = input.FinanceIOF,
            GracePeriodDays = input.GracePeriodDays,
            FirstDueDate = dataPrimeiroVencimento,

            Installments = parcelas
        };
    }

    private List<InstallmentDetailDto> GerarTabelaPrice(
        decimal saldoInicial,
        decimal valorParcela,
        decimal taxaJuros,
        int prazo,
        DateTime dataLiberacao,
        int frequenciaDias,
        DateTime primeiroVencimentoReal)
    {
        var lista = new List<InstallmentDetailDto>();
        decimal saldo = saldoInicial;

        int diaBase = primeiroVencimentoReal.Day;

        int mesesIniciais = (primeiroVencimentoReal.Year - dataLiberacao.Year) * 12 + primeiroVencimentoReal.Month - dataLiberacao.Month;
        if (mesesIniciais < 1) mesesIniciais = 1;

        //DateTime dataVencimento = dataPrimeiraParcela;

        for (int n = 1; n <= prazo; n++)
        {

            decimal juros = Math.Round(saldo * taxaJuros, 2, MidpointRounding.AwayFromZero);
            decimal amortizacao = Math.Round(valorParcela - juros, 2);

            if (n == prazo)
            {
                amortizacao = saldo;
                valorParcela = amortizacao + juros;
            }

            decimal saldoFinal = saldo - amortizacao;
            if (saldoFinal < 0) saldoFinal = 0;

            DateTime dataVencimentoCalculada;

            if (frequenciaDias == 30)
            {
                DateTime dataMesAlvo = primeiroVencimentoReal.AddMonths(n - 1);
 
                int diasNoMes = DateTime.DaysInMonth(dataMesAlvo.Year, dataMesAlvo.Month);
                int diaCorrigido = Math.Min(diaBase, diasNoMes);

                dataVencimentoCalculada = new DateTime(dataMesAlvo.Year, dataMesAlvo.Month, diaCorrigido, 12, 0, 0);
            }
            else
            {
                dataVencimentoCalculada = primeiroVencimentoReal.AddDays((n - 1) * frequenciaDias);
            }

            lista.Add(new InstallmentDetailDto()
            {
                Number = n,
                DueDate = dataVencimentoCalculada,
                Value = valorParcela,
                Interest = juros,
                Amortization = amortizacao,
                Balance = saldoFinal
            });

            saldo = saldoFinal;
        }
        return lista;
    }

    public async Task<Simulation> ConfirmAsync(SimulationResultDto resultDto, string loggedInUser, int userId)
    {
        var simulation = new Simulation
        {
            Product = resultDto.Product,
            RateTable = resultDto.RateTable,
            Rate = resultDto.Rate,
            Term = resultDto.Term,
            InstallmentAmount = resultDto.InstallmentAmount,
            ReleasedAmount = resultDto.ReleasedAmount,
            ContractValue = resultDto.ContractValue,

            TotalFinancedAmount = resultDto.TotalFinancedAmount,
            TotalIOF = resultDto.TotalIOF,
            IOFFinanced = resultDto.IOFFinanced,
            GracePeriodDays = resultDto.GracePeriodDays,
            HasGracePeriod = resultDto.GracePeriodDays > 0,
            FrequencyDays = 30,

            SimulationDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = loggedInUser
        };

        var savedSimulation = await _simRepository.AddAsync(simulation, loggedInUser, userId);

        var installments = resultDto.Installments.Select(i => new Installment
        {
            SimulationId = savedSimulation.Id,
            InstallmentNumber = i.Number,
            DueDate = i.DueDate.Date.AddHours(12),

            Status = "Pending",

            OriginalAmount = i.Value,
            Interest = i.Interest,
            Amortization = i.Amortization,
            Balance = i.Balance,

            OpeningBalance = i.Balance + i.Amortization,

        }).ToList();

        if (installments.Any())
        {
            await _installmentRepository.AddBulkAsync(installments);
        }

        return savedSimulation;
    }

    public async Task<Simulation> UpdateAsync(int id, Simulation simulation, string loggedInUser)
    {
        var updatedSim = await _simRepository.UpdateAsync(id, simulation, loggedInUser);

        DateTime dataLiberacao = DateTime.Today.AddHours(12);

        int frequencia = simulation.FrequencyDays > 0 ? simulation.FrequencyDays : 30;
        int diasParaVencimento = simulation.GracePeriodDays > 0 ? simulation.GracePeriodDays : frequencia;

        DateTime dataPrimeiroVencimento;

        if (frequencia == 30 && (diasParaVencimento % 30 == 0))
        {
            int meses = diasParaVencimento / 30;
            if (meses < 1) meses = 1;
            dataPrimeiroVencimento = dataLiberacao.AddMonths(meses);
        }
        else
        {
            dataPrimeiroVencimento = dataLiberacao.AddDays(diasParaVencimento);
        }

        dataPrimeiroVencimento = new DateTime(dataPrimeiroVencimento.Year, dataPrimeiroVencimento.Month, dataPrimeiroVencimento.Day, 12, 0, 0);

        var installmentsDto = GerarTabelaPrice(
            simulation.TotalFinancedAmount,
            simulation.InstallmentAmount,
            simulation.Rate / 100m,
            simulation.Term,
            dataLiberacao, 
            frequencia,
            dataPrimeiroVencimento 
        );

        var installmentsEntities = installmentsDto.Select(i => new Installment
        {
            SimulationId = id,
            InstallmentNumber = i.Number,
            DueDate = i.DueDate.Date.AddHours(12),
            Status = "Pending",
            OriginalAmount = i.Value,
            Interest = i.Interest,
            Amortization = i.Amortization,
            Balance = i.Balance,
            OpeningBalance = i.Balance + i.Amortization
        }).ToList();

        await _installmentRepository.DeleteBySimulationIdAsync(id);
        await _installmentRepository.AddBulkAsync(installmentsEntities);

        return updatedSim;
    }

    public Task<PagedSimulationResult> GetAllAsync(string term, int pageNumber, int pageSize)
    {
        return _simRepository.GetAllAsync(term, pageNumber, pageSize);
    }

    public Task<Simulation> GetByIdAsync(int id)
    {
        return _simRepository.GetByIdAsync(id);
    }

    public Task<DashboardTotalsDtos> GetDashboardTotalsAsync()
    {
        return _simRepository.GetDashboardTotalsAsync();
    }
}
