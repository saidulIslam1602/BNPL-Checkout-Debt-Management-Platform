export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api',
  wsUrl: 'wss://localhost:5001/ws',
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