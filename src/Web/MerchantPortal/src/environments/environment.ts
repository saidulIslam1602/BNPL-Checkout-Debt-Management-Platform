export const environment = {
  production: false,
  apiUrl: 'https://localhost:7000',
  wsUrl: 'ws://localhost:5005',
  appName: 'YourCompany Merchant Portal',
  version: '1.0.0',
  features: {
    realTimeUpdates: true,
    advancedAnalytics: true,
    norwegianIntegration: true,
    mockData: true
  },
  external: {
    vippsApiUrl: 'https://apitest.vipps.no',
    dnbApiUrl: 'https://developer-api-testmode.dnb.no',
    experianApiUrl: 'https://sandbox-api.experian.no'
  }
};