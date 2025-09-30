import { Routes } from '@angular/router';

export const settlementRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./settlements-list/settlements-list.component').then(m => m.SettlementsListComponent)
  },
  {
    path: ':id',
    loadComponent: () => import('./settlement-detail/settlement-detail.component').then(m => m.SettlementDetailComponent)
  }
];