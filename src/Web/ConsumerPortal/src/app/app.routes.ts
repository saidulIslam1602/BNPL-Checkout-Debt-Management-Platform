import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/home',
    pathMatch: 'full'
  },
  {
    path: 'home',
    loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent)
  },
  {
    path: 'checkout',
    loadComponent: () => import('./features/checkout/checkout.component').then(m => m.CheckoutComponent)
  },
  {
    path: 'my-payments',
    loadComponent: () => import('./features/payments/my-payments.component').then(m => m.MyPaymentsComponent)
  },
  {
    path: 'how-it-works',
    loadComponent: () => import('./features/info/how-it-works.component').then(m => m.HowItWorksComponent)
  },
  {
    path: 'merchants',
    loadComponent: () => import('./features/info/merchants.component').then(m => m.MerchantsComponent)
  },
  {
    path: 'calculator',
    loadComponent: () => import('./features/calculator/payment-calculator.component').then(m => m.PaymentCalculatorComponent)
  },
  {
    path: 'faq',
    loadComponent: () => import('./features/support/faq.component').then(m => m.FaqComponent)
  },
  {
    path: 'contact',
    loadComponent: () => import('./features/support/contact.component').then(m => m.ContactComponent)
  },
  {
    path: 'terms',
    loadComponent: () => import('./features/legal/terms.component').then(m => m.TermsComponent)
  },
  {
    path: 'privacy',
    loadComponent: () => import('./features/legal/privacy.component').then(m => m.PrivacyComponent)
  },
  {
    path: 'cookies',
    loadComponent: () => import('./features/legal/cookies.component').then(m => m.CookiesComponent)
  },
  {
    path: '**',
    loadComponent: () => import('./shared/components/not-found/not-found.component').then(m => m.NotFoundComponent)
  }
];