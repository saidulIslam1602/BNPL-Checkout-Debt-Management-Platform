import { Routes } from '@angular/router';

export const customerRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./customers-list/customers-list.component').then(m => m.CustomersListComponent)
  },
  {
    path: ':id',
    loadComponent: () => import('./customer-detail/customer-detail.component').then(m => m.CustomerDetailComponent)
  }
];