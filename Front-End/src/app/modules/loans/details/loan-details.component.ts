import { Component, ChangeDetectorRef, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { NgxMaskDirective, NgxMaskPipe } from 'ngx-mask';
import { SimulationService } from 'app/core/simulation/simulation.service';
import { Simulation, SimulationInput, SimulationResult } from 'app/core/simulation/simulation.types';
import { Installment } from 'app/core/installment/installment.types';
import { InstallmentService } from 'app/core/installment/installment.service';
import { finalize, switchMap, take, of, tap } from 'rxjs';
import { ExcelService } from 'app/modules/excel/excel.service';
import { PaymentDialogComponent } from 'app/modules/simulations/details/payment-dialog/payment-dialog.component';
import { RefinanceDialogComponent } from '../refinance/refinance-dialog.component';
import { items } from 'app/mock-api/apps/file-manager/data';

@Component({
  selector: 'app-loan-details',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
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
    NgxMaskDirective,
    NgxMaskPipe
  ],
  templateUrl: './loan-details.component.html'
})
export class LoanDetailsComponent implements OnInit {

  input: SimulationInput = {
    product: '',
    rateTable: '',
    rate: 0,
    term: 0,
    requestedAmount: 0,
    financeIOF: false,
    gracePeriodDays: 0,
    frequencyDays: 30,
    includeInsurance: false,
    insuranceRate: 0,
    tacAmount: 0,
    financeTac: false,
    refinancedFromId: null
  };

  result: SimulationResult | null = null;
  simulationId: number | null = null;

  refinancedToId: number | null = null;

  possuiPagamento: boolean = false;

  isLoading = false;

  showInstallments = true;
  isLoadingInstallments = false;
  dataSource = new MatTableDataSource<Installment>([]);
  displayedColumns: string[] = [
    'installmentNumber', 'dueDate', 'openingBalance', 'interest', 'amortization', 'originalAmount', 'finalBalance', 'actions'
  ];
  totalInstallments = 0;

  @ViewChild(MatPaginator) private _paginator: MatPaginator;

  constructor(
    private _simulationService: SimulationService,
    private _installmentService: InstallmentService,
    private _excelService: ExcelService,
    private _snackBar: MatSnackBar,
    private _changeDetectorRef: ChangeDetectorRef,
    private _route: ActivatedRoute,
    private _router: Router,
    private _dialog: MatDialog
  ) { }

  ngOnInit(): void {
    this.loadLoanData();
  }

  ngAfterViewInit(): void {
    if (this._paginator) {
      this._paginator.page
        .pipe(
          tap(() => this.loadInstallments())
        )
        .subscribe();
    }
  }

  loadLoanData(): void {
    this._route.paramMap.pipe(
      switchMap(params => {
        const idStr = params.get('id');
        if (!idStr) return of(null);

        this.simulationId = Number(idStr);
        this.isLoading = true;
        return this._simulationService.getSimulationById(this.simulationId);
      }),
      finalize(() => {
        this.isLoading = false;
        this._changeDetectorRef.markForCheck();
      })
    ).subscribe((simulation: Simulation | null) => {
      if (simulation) {

        console.log('Simulação carregada:', simulation);

        const simAny = simulation as any;
        this.refinancedToId = simAny.refinancedToId || simAny.RefinancedToId || null;

        console.log('ID do novo contrato (refinancedToId):', this.refinancedToId);

        const refinId = simulation.refinancedFromId || simAny.RefinancedFromId || undefined;

        this.input = {
          product: simulation.product,
          rateTable: simulation.rateTable,
          rate: simulation.rate,
          term: simulation.term,
          requestedAmount: 0,
          financeIOF: simulation.iofFinanced,
          gracePeriodDays: simulation.gracePeriodDays,
          frequencyDays: 30,

          includeInsurance: simulation.includeInsurance,
          insuranceRate: simulation.insuranceRate,
          tacAmount: simulation.tacAmount,
          financeTac: simulation.tacFinanced,

          refinancedFromId: refinId
        };

        const released = Number(simulation.releasedAmount);
        const iof = Number((simulation as any).totalIOF || (simulation as any).TotalIOF || 0);
        const payoff = Number(simulation.payoffAmount || (simulation as any).PayoffAmount || 0);

        let originalRequested = released;

        if (refinId) {
          originalRequested += payoff;
        }

        if (!simulation.iofFinanced) {
          originalRequested += iof;
        }

        if (!simulation.tacFinanced) {
          originalRequested += Number(simulation.tacAmount || 0);
        }

        this.input.requestedAmount = originalRequested;

        this.result = simulation as unknown as SimulationResult;

        if (this.result) {
          this.result.payoffAmount = payoff;
        }


        this._changeDetectorRef.detectChanges();

        this.loadInstallments();
      }
    });
  }

  loadInstallments(): void {
    if (!this.simulationId) return;

    this.isLoadingInstallments = true;
    const pageIndex = this._paginator ? this._paginator.pageIndex + 1 : 1;
    const pageSize = this._paginator ? this._paginator.pageSize : 12;

    this._installmentService.getInstallmentsForSimulation(this.simulationId, pageIndex, pageSize)
      .pipe(finalize(() => this.isLoadingInstallments = false))
      .subscribe(response => {
        if (response) {
          this.dataSource.data = response.items;
          this.totalInstallments = response.totalCount;
          this._changeDetectorRef.markForCheck();

          this.possuiPagamento = response.items.some(x => x.paidAmount && x.paidAmount > 0);
        }
      });
  }

  dis() {
    console.log(this.possuiPagamento)
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
          this._snackBar.open('Pagamento registrado e saldo recalculado!', 'OK', { duration: 4000 });
          this.loadInstallments();
        },
        error: (err) => {
          console.error('Erro no pagamento:', err);

          const backendMessage = err.error?.message || (typeof err.error === 'string' ? err.error : null);
          const errorMessage = backendMessage || err.message || 'Erro ao registrar pagamento.';

          this._snackBar.open(errorMessage, 'Fechar', {
            duration: 8000,
            panelClass: ['warning-snackbar']
          });
        }
      });
  }

  exportToExcel(): void {
    if (!this.simulationId) return;

    this.isLoadingInstallments = true;

    this._installmentService.getInstallmentsForSimulation(this.simulationId, 1, 99999)
      .pipe(finalize(() => this.isLoadingInstallments = false))
      .subscribe({
        next: (response: any) => {
          const allInstallments = response.items || [];

          if (!allInstallments || allInstallments.length === 0) {
            this._snackBar.open('Não há dados para exportar.', 'OK', { duration: 3000 });
            return;
          }

          const dataToExport = allInstallments.map((item: any) => {
            return {
              'Parcela': item.installmentNumber,
              'Vencimento': new Date(item.dueDate).toLocaleDateString('pt-BR'),
              'Saldo Devedor': item.openingBalance,
              'Juros': item.interest,
              'Amortização': item.amortization,
              'Valor Parcela': item.originalAmount,
              'Saldo Final': item.openingBalance
            };
          });

          const fileName = `Emprestimo_${this.simulationId}_Parcelas_Completo`;
          this._excelService.exportAsExcelFile(dataToExport, fileName);

        },
        error: (error) => {
          console.error('Erro ao exportar:', error);
          this._snackBar.open('Erro ao baixar os dados para exportação.', 'Fechar', { duration: 3000 });
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

  onRefinance(): void {
    if (!this.simulationId) return;

    if (!this.possuiPagamento) {
      this._snackBar.open('O refinanciamento só pode ser realizado após o pagamento de ao menos uma parcela.', 'Fechar', {
        duration: 4000,
      })
      return;
    }

    this._router.navigate(['/simulations/new'], {
      queryParams: { refinance: this.simulationId }
    });
  }

  openRefinanceDetails(): void {
    if (!this.result) return;

    this._dialog.open(RefinanceDialogComponent, {
      width: '800px',
      maxWidth: '95vw',
      autoFocus: false,
      data: {
        simulation: this.result
      }
    });
  }

  goToNewContract(): void {
    if (this.refinancedToId) {
      console.log('Navegando para o contrato refinanciado com ID:', this.refinancedToId);
      this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => {
        this._router.navigate(['/loans', this.refinancedToId]);
      });
    }
  }
}