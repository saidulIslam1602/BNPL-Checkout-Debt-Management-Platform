export const environment = {
  production: true,
  apiUrl: 'https://api.yourcompany.no/api',
  wsUrl: 'wss://api.yourcompany.no/ws',
  appName: 'YourCompany Consumer Portal',
  version: '1.0.0',
  features: {
    realTimeUpdates: true,
    pushNotifications: true,
    norwegianIntegration: true,
    mockData: false,
    pwaEnabled: true
  },
  external: {
    vippsApiUrl: 'https://api.vipps.no',
    dnbApiUrl: 'https://developer-api.dnb.no',
    experianApiUrl: 'https://api.experian.no',
    googleAnalyticsId: 'GA_MEASUREMENT_ID',
    hotjarId: 'HOTJAR_ID'
  },
  payment: {
    maxInstallmentAmount: 50000,
    minInstallmentAmount: 100,
    maxInstallmentPeriod: 24,
    interestRates: {
      low: 0.0,
      medium: 0.05,
      high: 0.12
    }
  }
};