import { Component, ChangeDetectorRef, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { NgxMaskDirective, NgxMaskPipe } from 'ngx-mask';

import { PaymentDialogComponent } from './payment-dialog/payment-dialog.component';
import { SimulationService } from 'app/core/simulation/simulation.service';
import { RateTableService } from 'app/core/simulation/ratetable.service';
import { Simulation, SimulationInput, SimulationResult, RateTable } from 'app/core/simulation/simulation.types';
import { Installment } from 'app/core/installment/installment.types';
import { InstallmentService } from 'app/core/installment/installment.service';
import { finalize, switchMap, take, of, Observable, tap } from 'rxjs';

@Component({
  selector: 'app-details',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatIconModule,
    MatSnackBarModule,
    RouterLink,
    MatTableModule,
    MatPaginatorModule,
    MatCheckboxModule,
    MatDialogModule,
    MatTooltipModule,
    MatSlideToggleModule,
    MatCardModule,
    MatDividerModule,
    NgxMaskDirective,
    NgxMaskPipe
  ],
  templateUrl: './details.component.html',
})
export class SimulationDetailsComponent implements OnInit {

  tacRate = 3.0;

  input: SimulationInput = {
    product: 'INSS - Novo',
    rateTable: '',
    rate: 0,
    term: 0,
    requestedAmount: 0,
    financeIOF: false,
    gracePeriodDays: 0,
    frequencyDays: 30,
    includeInsurance: false,
    insuranceRate: 2.50,
    tacAmount: 0,
    financeTac: true,
    refinancedFromId: undefined
  };

  result: SimulationResult | null = null;
  isCalculating = false;
  isConfirming = false;
  errorMessage: string | null = null;
  refinanceId: number | null = null;

  isEditMode = false;
  isRefinancing = false;
  simulationId: number | null = null;

  allTables: RateTable[] = [];
  selectedTable: RateTable | null = null;

  taxasDisponiveis: number[] = [];
  prazosDisponiveis: number[] = [];

  minInstallment = 0;
  maxInstallment = 0;

  showInstallments = false;
  isLoadingInstallments = false;
  dataSource = new MatTableDataSource<Installment>([]);
  displayedColumns: string[] = [
    'installmentNumber',
    'dueDate',
    'balance',
    'interest',
    'amortization',
    'originalAmount',
    'finalBalance'
  ];
  totalInstallments = 0;

  @ViewChild(MatPaginator) private _paginator: MatPaginator;

  id: string

  constructor(
    private _simulationService: SimulationService,
    private _rateTableService: RateTableService,
    private _installmentService: InstallmentService,
    private _snackBar: MatSnackBar,
    private _changeDetectorRef: ChangeDetectorRef,
    private _route: ActivatedRoute,
    private _router: Router,
    private _dialog: MatDialog
  ) { }

  ngOnInit(): void {
    this._rateTableService.getTablesForSimulation().pipe(take(1)).subscribe(tables => {
      this.allTables = tables;
      this.loadSimulationDataFromRoute();
    });
  }

  get valorTacCalculado(): number {
    if (!this.input.requestedAmount) return 0;
    return this.input.requestedAmount * (this.tacRate / 100);
  }

  get ValorSeguroEstimado(): number {
    if (!this.input.includeInsurance || !this.input.requestedAmount) return 0;
    let base = this.input.requestedAmount;
    if (!this.input.financeIOF) base += (this.input.tacAmount || 0);
    return base * ((this.input.insuranceRate || 0) / 100);
  }

  loadSimulationDataFromRoute(): void {
    this._route.paramMap.pipe(
      switchMap(params => {
        this.id = params.get('id');

        if (this.id === 'new') {
          this.isEditMode = false;
          this.simulationId = null;
          this.setupDefaultTable();

          const paramsId = this._route.snapshot.queryParamMap.get('refinance');
          if (paramsId) {
            this.refinanceId = Number(paramsId);
            this.isRefinancing = true;
            this.input.refinancedFromId = this.refinanceId;
            this.input.product = 'Refinanciamento';
            this._snackBar.open(`Refinanciando contrato ${paramsId}`, 'OK', { duration: 3000 });
          } else {
            this.isRefinancing = false;
            this.input.refinancedFromId = undefined;
          }
          this.setupDefaultTable();

          return of(null);
        }

        this.isEditMode = true;
        this.simulationId = Number(this.id);
        return this._simulationService.getSimulationById(this.simulationId);
      })
    ).subscribe((simulation: Simulation | null) => {
      if (simulation) {

        const released = simulation.releasedAmount ? Number(simulation.releasedAmount) : 0;
        const iof = simulation.totalIOF ? Number(simulation.totalIOF) : 0;
        const tac = simulation.tacAmount ? Number(simulation.tacAmount) : 0;

        let originalRequested = released;

        if (!simulation.iofFinanced) {
          originalRequested += iof;
        }

        if (!simulation.tacFinanced) {
          originalRequested += tac;
        }

        originalRequested = Math.round(originalRequested * 100) / 100;

        if (originalRequested > 0 && tac > 0) {
          this.tacRate = (tac / originalRequested) * 100;
        }

        this.input = {
          product: simulation.product,
          rateTable: simulation.rateTable,
          rate: simulation.rate,
          term: simulation.term,
          financeIOF: simulation.iofFinanced,
          gracePeriodDays: simulation.gracePeriodDays,
          frequencyDays: 30,

          includeInsurance: simulation.includeInsurance,
          insuranceRate: simulation.insuranceRate || 2.50,
          tacAmount: tac,
          financeTac: simulation.tacFinanced,

          requestedAmount: originalRequested
        };

        this.selectedTable = this.allTables.find(t => t.name === simulation.rateTable) || null;

        if (this.selectedTable) {
          this.updateTableOptionsOnly();
        }

        this.result = simulation as unknown as SimulationResult;

        this._changeDetectorRef.markForCheck();
      }
    });
  }

  populateForm(simulation: Simulation): void {
    this.input.product = simulation.product;
    this.input.rateTable = simulation.rateTable;
    this.input.rate = simulation.rate;
    this.input.term = simulation.term;
    this.input.financeIOF = simulation.iofFinanced;
    this.input.gracePeriodDays = simulation.gracePeriodDays;

    this.input.includeInsurance = simulation.includeInsurance;
    this.input.insuranceRate = simulation.insuranceRate;
    this.input.tacAmount = simulation.tacAmount;
    this.input.financeTac = simulation.tacFinanced;

    this.input.refinancedFromId = simulation.refinancedFromId;
    if (this.input.refinancedFromId) {
      this.isRefinancing = true;
    }

    const released = Number(simulation.releasedAmount);
    const iof = Number((simulation as any).totalIOF || (simulation as any).iof || 0);
    const payoff = Number(simulation.payoffAmount || 0);

    let originalRequested = released;

    if (!simulation.iofFinanced) {
      originalRequested += payoff;
    }

    if (!simulation.iofFinanced) {
      originalRequested += iof;
    }

    if (!simulation.tacFinanced) {
      originalRequested += (simulation.tacAmount || 0);
    }

    this.input.requestedAmount = Math.round(originalRequested * 100) / 100;

    this.selectedTable = this.allTables.find(t => t.name === simulation.rateTable) || null;

    if (this.selectedTable) {
      this.updateTableOptionsOnly();
    }

    this.result = simulation as unknown as SimulationResult;

    this._changeDetectorRef.markForCheck();
  }

  setupDefaultTable(): void {
    if (this.allTables.length > 0) {
      const defaultTable = this.allTables[0];
      this.selectedTable = defaultTable;
      this.onTableChange();
    }
  }

  updateTableOptionsOnly(): void {
    const selectedTable = this.selectedTable;
    if (!selectedTable) return;

    this.taxasDisponiveis = selectedTable.availableRates;
    this.prazosDisponiveis = selectedTable.availableTerms;
    this.minInstallment = selectedTable.minInstallmentAmount;
    this.maxInstallment = selectedTable.maxInstallmentAmount;

    this.input.rateTable = selectedTable.name;
  }

  onTableChange(): void {
    this.updateTableOptionsOnly();

    const selectedTable = this.selectedTable;
    if (selectedTable) {
      this.input.rate = selectedTable.availableRates[0];
      this.input.term = selectedTable.availableTerms[0];

      if (!this.isEditMode && !this.input.requestedAmount) {
        this.input.requestedAmount = 10000;
      }
    }

    if (this.isRefinancing) {
      this.input.product = 'Refinanciamento';
    } else if (!this.isEditMode) {
      this.input.product = 'INSS - Novo';
    }

    this.result = null;
    this.errorMessage = null;
    this._changeDetectorRef.markForCheck();
  }

  onSimular(): void {
    if (!this.input.product) {
      this.errorMessage = 'O campo Produto é obrigatório.'; return;
    }
    if (this.input.requestedAmount <= 0) {
      this.errorMessage = 'O valor solicitado deve ser maior que zero.'; return;
    }

    this.input.tacAmount = this.valorTacCalculado;

    this.isCalculating = true;
    this.errorMessage = null;
    this.result = null;

    this.isCalculating = true;
    this.errorMessage = null;
    this.result = null;

    this._simulationService.calculate(this.input).pipe(
      finalize(() => {
        this.isCalculating = false;
        this._changeDetectorRef.markForCheck();
      })
    ).subscribe({
      next: (calcResult) => {
        this.result = calcResult;
      },
      error: (err) => {
        this.errorMessage = err.error.message || 'Erro ao calcular a simulação.';
      }
    });
  }

  onConfirmar(): void {
    if (!this.result) {
      this.errorMessage = 'Você precisa calcular a simulação antes de confirmar.';
      return;
    }

    this.isConfirming = true;
    this.errorMessage = null;

    let saveObservable: Observable<any>;

    const simulationData = {
      ...this.result,

      requestedAmount: this.input.requestedAmount,
      financeIOF: this.input.financeIOF,
      gracePeriodDays: this.input.gracePeriodDays,
      includeInsurance: this.input.includeInsurance,
      insuranceRate: this.input.insuranceRate,
      tacAmount: this.input.tacAmount,
      financeTac: this.input.financeTac,
      refinancedFromId: this.refinanceId
    };

    console.log('Dados da simulação a salvar:', this.id);

    if (this.isEditMode) {

      const simulationToUpdate: any = {
        id: this.simulationId,
        ...simulationData,
        simulationDate: new Date().toISOString(),
        createdAt: new Date().toISOString(),
        createdBy: 'Usuário Editado'
      };
      saveObservable = this._simulationService.updateSimulation(this.simulationId, simulationToUpdate);

    } else {
      saveObservable = this._simulationService.confirm(simulationData);
    }

    saveObservable.pipe(
      finalize(() => this.isConfirming = false)
    ).subscribe({
      next: (savedSimulation) => {
        this._snackBar.open('Simulação salva com sucesso!', 'Fechar', { duration: 3000 });

        if (!this.isEditMode) {
          const newId = savedSimulation.id || this.simulationId;
          this._router.navigate(['../', newId], { relativeTo: this._route });
        } else {
          this.loadSimulationDataFromRoute();
        }
      },
      error: (err) => {
        this.errorMessage = err?.error?.message || err?.message || 'Erro ao salvar a simulação.';
      }
    });
  }

  onViewInstallments(): void {
    this.showInstallments = !this.showInstallments;

    if (this.showInstallments && this.dataSource.data.length === 0) {

      setTimeout(() => {
        if (this._paginator) {

          this._paginator.page.pipe(
            tap(() => this.loadInstallments())
          ).subscribe();

          this.loadInstallments();
        }
      }, 0);
    }
  }

  loadInstallments(): void {
    if (!this.simulationId) return;

    this.isLoadingInstallments = true;

    const pageIndex = this._paginator ? this._paginator.pageIndex + 1 : 1;
    const pageSize = this._paginator ? this._paginator.pageSize : 10;

    this._installmentService.getInstallmentsForSimulation(this.simulationId, pageIndex, pageSize)
      .pipe(finalize(() => this.isLoadingInstallments = false))
      .subscribe(response => {
        if (response) {
          this.dataSource.data = response.items;
          this.totalInstallments = response.totalCount;
          this._changeDetectorRef.markForCheck();
        }
      });
  }

  openPaymentDialog(installment: Installment): void {
    const dialogRef = this._dialog.open(PaymentDialogComponent, {
      panelClass: 'custom-dialog-container',
      autoFocus: false,
      data: {
        installmentNumber: installment.installmentNumber,
        originalAmount: installment.originalAmount
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      console.log('Modal fechado. Resultado:', result);
      if (result) {
        this.processPayment(installment.id, result.amount, result.date);
      }
    });
  }

  processPayment(id: number, amount: number, date: Date): void {
    this.isLoadingInstallments = true;
    this._installmentService.payInstallment(id, amount, date)
      .pipe(finalize(() => this.isLoadingInstallments = false))
      .subscribe({
        next: () => {
          this._snackBar.open('Pagamento registrado com sucesso!', 'OK', { duration: 3000 });
          this.loadInstallments();
        },
        error: (err) => {
          this._snackBar.open('Erro ao registrar pagamento: ' + err.message, 'Fechar', { duration: 5000 });
        }
      });
  }

  get valorParcelaTexto(): string {
    if (!this.result) return '';
    const valorFormatado = (this.result.installmentAmount || 0).toLocaleString('pt-BR', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    });
    return `${this.result.term}x de R$ ${valorFormatado}`;
  }
}
