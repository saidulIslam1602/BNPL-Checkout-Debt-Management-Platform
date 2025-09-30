import { Routes } from '@angular/router';

export const transactionRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./transactions-list/transactions-list.component').then(m => m.TransactionsListComponent)
  },
  {
    path: ':id',
    loadComponent: () => import('./transaction-detail/transaction-detail.component').then(m => m.TransactionDetailComponent)
  }
];