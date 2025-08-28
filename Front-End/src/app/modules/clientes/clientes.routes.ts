import { Routes } from '@angular/router';
import { ClientesComponent } from './clientes.component';
import { ClientesListComponent } from './list/list.component';
import { ClientesDetailsComponent } from './details/details.component';
import { ClientesEmptyDetailsComponent } from './empty-details/empty-details.component';

export default [
  {
    path: '',
    component: ClientesComponent,
    children: [
      {
        path: '',
        component: ClientesListComponent,
        children: [
          {
            path: '',
            component: ClientesEmptyDetailsComponent
          },
          {
            path     : 'novo',
            component: ClientesDetailsComponent,
            },
            {
            path     : ':id',
            component: ClientesDetailsComponent,
            },
        ]
      }
    ]
  }
] as Routes;
