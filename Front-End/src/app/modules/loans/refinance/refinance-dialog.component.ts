import { Component, inject, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { Simulation } from 'app/core/simulation/simulation.types';

@Component({
  selector: 'app-refinance-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule
  ],
  templateUrl: './refinance-dialog.component.html'
})
export class RefinanceDialogComponent implements OnInit {

    totalDebitos = 0;
    totalCreditos = 0;
    saldoFinal = 0;

    constructor(
        @Inject(MAT_DIALOG_DATA) public data: { simulation: Simulation },
        public dialogRef: MatDialogRef<RefinanceDialogComponent>
    ) {}

    ngOnInit(): void {
        if (this.data && this.data.simulation) {
            this.calcularResumo();
        }
    }

    calcularResumo(): void {
        const sim = this.data.simulation;

        this.totalCreditos = sim.totalFinancedAmount;

        const quitacao = sim.payoffAmount || 0;

        const iof = sim.totalIOF || 0;

        const tac = sim.tacAmount || 0;

        this.totalDebitos = quitacao + iof + tac;

        this.saldoFinal = sim.releasedAmount;
        
    }

    get hasPayoff(): boolean {
        return (this.data.simulation.payoffAmount || 0) > 0;
    }
}