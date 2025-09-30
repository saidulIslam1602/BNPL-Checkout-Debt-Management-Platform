export const environment = {
  production: true,
  apiUrl: 'https://api.yourcompany.no/api',
  wsUrl: 'wss://api.yourcompany.no/ws',
  appName: 'YourCompany Merchant Portal',
  version: '1.0.0',
  features: {
    realTimeUpdates: true,
    advancedAnalytics: true,
    norwegianIntegration: true,
    mockData: false
  },
  external: {
    vippsApiUrl: 'https://api.vipps.no',
    dnbApiUrl: 'https://developer-api.dnb.no',
    experianApiUrl: 'https://api.experian.no'
  }
};