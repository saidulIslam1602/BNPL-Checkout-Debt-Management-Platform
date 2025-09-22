export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api/v1',
  wsUrl: 'wss://localhost:5001/ws',
  appName: 'Riverty Merchant Portal',
  version: '1.0.0',
  supportedLanguages: ['no', 'en'],
  defaultLanguage: 'no',
  currency: 'NOK',
  dateFormat: 'dd.MM.yyyy',
  timeFormat: 'HH:mm',
  pagination: {
    defaultPageSize: 20,
    pageSizeOptions: [10, 20, 50, 100]
  },
  charts: {
    colors: {
      primary: '#1e3a8a',
      secondary: '#3b82f6',
      success: '#059669',
      warning: '#ea580c',
      danger: '#dc2626',
      info: '#0891b2'
    }
  },
  features: {
    realTimeUpdates: true,
    advancedAnalytics: true,
    exportData: true,
    bulkOperations: true,
    riskManagement: true,
    norwegianIntegrations: true
  },
  integrations: {
    vipps: {
      enabled: true,
      testMode: true
    },
    bankId: {
      enabled: true,
      testMode: true
    },
    norwegianTaxAuthority: {
      enabled: true,
      testMode: true
    }
  },
  monitoring: {
    enableErrorReporting: true,
    enablePerformanceTracking: true,
    enableUserAnalytics: false // GDPR compliance
  }
};