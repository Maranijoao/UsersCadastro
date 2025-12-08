import { Routes } from '@angular/router';
import { SimulationListComponent } from 'app/modules/simulations/list/list.component';
import { SimulationDetailsComponent } from 'app/modules/simulations/details/details.component';
import { AuthGuard } from 'app/core/auth/guards/auth.guard'; // Verifique se o caminho do seu AuthGuard está correto

export default [
    
    {
        path: '',
        component: SimulationListComponent,
        canActivate: [AuthGuard] 
    },

    {
        path: ':id',
        component: SimulationDetailsComponent,
        canActivate: [AuthGuard] 
    },

    // {
    //     path: 'details/:id', 
    //     component: SimulationDetailsComponent
    // },

] as Routes;