import { Routes } from '@angular/router';

export const riskManagementRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./risk-dashboard/risk-dashboard.component').then(m => m.RiskDashboardComponent)
  },
  {
    path: 'assessments',
    loadComponent: () => import('./risk-assessments/risk-assessments.component').then(m => m.RiskAssessmentsComponent)
  }
];