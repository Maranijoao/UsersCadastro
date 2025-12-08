import { Routes } from '@angular/router';
import { LoanListComponent } from './list/loan-list.component';
import { LoanDetailsComponent } from './details/loan-details.component';
import { AuthGuard } from 'app/core/auth/guards/auth.guard';

export default [
    {
        path: '',
        component: LoanListComponent,
        canActivate: [AuthGuard]
    },
    {
        path: ':id',
        component: LoanDetailsComponent,
        canActivate: [AuthGuard]
    },
] as Routes;