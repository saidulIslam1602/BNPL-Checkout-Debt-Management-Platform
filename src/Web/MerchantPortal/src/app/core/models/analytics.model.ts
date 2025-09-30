export interface AnalyticsSummary {
  totalRevenue: number;
  revenueTrend: number;
  approvedApplications: number;
  approvalRate: number;
  approvalRateTrend: number;
  fraudIncidents: number;
  fraudRate: number;
  fraudRateTrend: number;
  averageCreditScore: number;
  creditScoreTrend: number;
}

export interface ChartData {
  labels: string[];
  data: number[];
}

export interface TimeSeriesData {
  timestamp: Date;
  value: number;
  label?: string;
}

export interface MetricCard {
  title: string;
  value: number | string;
  trend?: number;
  trendLabel?: string;
  icon: string;
  color: 'primary' | 'accent' | 'warn' | 'success';
  format: 'currency' | 'percentage' | 'number' | 'text';
}

export interface DashboardMetrics {
  revenue: {
    total: number;
    trend: number;
    chartData: ChartData;
  };
  transactions: {
    total: number;
    approved: number;
    declined: number;
    pending: number;
    approvalRate: number;
    chartData: ChartData;
  };
  risk: {
    averageScore: number;
    highRiskCount: number;
    fraudIncidents: number;
    fraudRate: number;
    distribution: ChartData;
  };
  customers: {
    total: number;
    active: number;
    new: number;
    retention: number;
  };
}

export interface ReportFilter {
  dateRange: {
    start: Date;
    end: Date;
  };
  merchantId?: string;
  riskLevel?: 'low' | 'medium' | 'high';
  status?: 'approved' | 'declined' | 'pending';
  paymentMethod?: string;
}

export interface ExportOptions {
  format: 'csv' | 'xlsx' | 'pdf';
  includeCharts: boolean;
  dateRange: {
    start: Date;
    end: Date;
  };
  sections: string[];
}