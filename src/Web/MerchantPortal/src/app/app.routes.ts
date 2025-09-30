import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/dashboard',
    pathMatch: 'full'
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/enhanced-dashboard.component').then(m => m.EnhancedDashboardComponent),
    canActivate: [authGuard]
  },
  {
    path: 'transactions',
    loadChildren: () => import('./features/transactions/transactions.routes').then(m => m.transactionRoutes),
    canActivate: [authGuard]
  },
  {
    path: 'settlements',
    loadChildren: () => import('./features/settlements/settlements.routes').then(m => m.settlementRoutes),
    canActivate: [authGuard]
  },
  {
    path: 'customers',
    loadChildren: () => import('./features/customers/customers.routes').then(m => m.customerRoutes),
    canActivate: [authGuard]
  },
  {
    path: 'analytics',
    loadChildren: () => import('./features/analytics/analytics.routes').then(m => m.analyticsRoutes),
    canActivate: [authGuard]
  },
  {
    path: 'risk-management',
    loadChildren: () => import('./features/risk-management/risk-management.routes').then(m => m.riskManagementRoutes),
    canActivate: [authGuard]
  },
  {
    path: 'integration',
    loadComponent: () => import('./features/integration/integration.component').then(m => m.IntegrationComponent),
    canActivate: [authGuard]
  },
  {
    path: 'settings',
    loadComponent: () => import('./features/settings/settings.component').then(m => m.SettingsComponent),
    canActivate: [authGuard]
  },
  {
    path: 'profile',
    loadComponent: () => import('./features/profile/profile.component').then(m => m.ProfileComponent),
    canActivate: [authGuard]
  },
  {
    path: '**',
    loadComponent: () => import('./shared/components/not-found/not-found.component').then(m => m.NotFoundComponent)
  }
];