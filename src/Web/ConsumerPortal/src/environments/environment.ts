export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api',
  wsUrl: 'wss://localhost:5001/ws',
  appName: 'YourCompany Consumer Portal',
  version: '1.0.0',
  features: {
    realTimeUpdates: true,
    pushNotifications: true,
    norwegianIntegration: true,
    mockData: true,
    pwaEnabled: true
  },
  external: {
    vippsApiUrl: 'https://apitest.vipps.no',
    dnbApiUrl: 'https://developer-api-testmode.dnb.no',
    experianApiUrl: 'https://sandbox-api.experian.no',
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