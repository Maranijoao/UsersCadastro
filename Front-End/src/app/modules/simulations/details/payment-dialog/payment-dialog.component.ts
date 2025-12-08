import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { NgxMaskDirective, NgxMaskPipe } from 'ngx-mask';

export interface PaymentDialogData {
  installmentNumber: number;
  originalAmount: number;
}

@Component({
  selector: 'app-payment-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatDatepickerModule,
    MatNativeDateModule,
    NgxMaskDirective,
    NgxMaskPipe,
    MatIconModule
  ],
  templateUrl: './payment-dialog.component.html'
})
export class PaymentDialogComponent implements OnInit {
    paymentForm: FormGroup;

    constructor(
        private fb: FormBuilder,
        public dialogRef: MatDialogRef<PaymentDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: PaymentDialogData
    ) {}

    ngOnInit(): void {
        this.paymentForm = this.fb.group({
            amount: [this.data.originalAmount, [Validators.required, Validators.min(0.01)]],
            paymentDate: [new Date(), Validators.required]
        });
    }

    onCancel(): void {
        this.dialogRef.close();
    }

    onConfirm(): void {
    if (this.paymentForm.valid) {
      const formValue = this.paymentForm.value;
      
      // Retorna o objeto com valor e data para quem chamou o modal
      this.dialogRef.close({
        amount: Number(formValue.amount),
        date: formValue.paymentDate
      });
    }
  }
}