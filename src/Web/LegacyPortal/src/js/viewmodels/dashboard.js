/**
 * Dashboard ViewModel
 * Handles the dashboard page functionality
 */

class DashboardViewModel {
    constructor(app) {
        this.app = app;
        this.isLoading = ko.observable(true);
        this.stats = ko.observable({});
        this.recentPayments = ko.observableArray([]);
        this.recentCustomers = ko.observableArray([]);
        this.chartData = ko.observable({});
        this.alerts = ko.observableArray([]);
        
        this.initialize();
    }

    /**
     * Initialize the dashboard
     */
    async initialize() {
        try {
            this.app.showLoading();
            await Promise.all([
                this.loadStats(),
                this.loadRecentPayments(),
                this.loadRecentCustomers(),
                this.loadChartData(),
                this.loadAlerts()
            ]);
        } catch (error) {
            this.app.showError('Failed to load dashboard data');
            console.error('Dashboard initialization error:', error);
        } finally {
            this.app.hideLoading();
            this.isLoading(false);
        }
    }

    /**
     * Load dashboard statistics
     */
    async loadStats() {
        try {
            const response = await this.app.apiRequest('GET', '/api/dashboard/stats');
            this.stats(response);
        } catch (error) {
            console.error('Error loading stats:', error);
        }
    }

    /**
     * Load recent payments
     */
    async loadRecentPayments() {
        try {
            const response = await this.app.apiRequest('GET', '/api/payments', {
                params: {
                    limit: 10,
                    sort: 'createdAt',
                    order: 'desc'
                }
            });
            this.recentPayments(response.data || []);
        } catch (error) {
            console.error('Error loading recent payments:', error);
        }
    }

    /**
     * Load recent customers
     */
    async loadRecentCustomers() {
        try {
            const response = await this.app.apiRequest('GET', '/api/customers', {
                params: {
                    limit: 10,
                    sort: 'createdAt',
                    order: 'desc'
                }
            });
            this.recentCustomers(response.data || []);
        } catch (error) {
            console.error('Error loading recent customers:', error);
        }
    }

    /**
     * Load chart data
     */
    async loadChartData() {
        try {
            const response = await this.app.apiRequest('GET', '/api/dashboard/charts');
            this.chartData(response);
            this.initializeCharts();
        } catch (error) {
            console.error('Error loading chart data:', error);
        }
    }

    /**
     * Load alerts
     */
    async loadAlerts() {
        try {
            const response = await this.app.apiRequest('GET', '/api/alerts');
            this.alerts(response.data || []);
        } catch (error) {
            console.error('Error loading alerts:', error);
        }
    }

    /**
     * Initialize charts
     */
    initializeCharts() {
        this.initializePaymentChart();
        this.initializeCustomerChart();
        this.initializeRevenueChart();
    }

    /**
     * Initialize payment volume chart
     */
    initializePaymentChart() {
        const ctx = document.getElementById('paymentChart');
        if (!ctx) return;

        const data = this.chartData().paymentVolume || {};
        new Chart(ctx, {
            type: 'line',
            data: {
                labels: data.labels || [],
                datasets: [{
                    label: 'Payment Volume',
                    data: data.values || [],
                    borderColor: 'rgb(75, 192, 192)',
                    backgroundColor: 'rgba(75, 192, 192, 0.2)',
                    tension: 0.1
                }]
            },
            options: {
                responsive: true,
                plugins: {
                    legend: {
                        position: 'top',
                    },
                    title: {
                        display: true,
                        text: 'Payment Volume Over Time'
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true
                    }
                }
            }
        });
    }

    /**
     * Initialize customer growth chart
     */
    initializeCustomerChart() {
        const ctx = document.getElementById('customerChart');
        if (!ctx) return;

        const data = this.chartData().customerGrowth || {};
        new Chart(ctx, {
            type: 'bar',
            data: {
                labels: data.labels || [],
                datasets: [{
                    label: 'New Customers',
                    data: data.values || [],
                    backgroundColor: 'rgba(54, 162, 235, 0.2)',
                    borderColor: 'rgba(54, 162, 235, 1)',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                plugins: {
                    legend: {
                        position: 'top',
                    },
                    title: {
                        display: true,
                        text: 'Customer Growth'
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true
                    }
                }
            }
        });
    }

    /**
     * Initialize revenue chart
     */
    initializeRevenueChart() {
        const ctx = document.getElementById('revenueChart');
        if (!ctx) return;

        const data = this.chartData().revenue || {};
        new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: data.labels || [],
                datasets: [{
                    data: data.values || [],
                    backgroundColor: [
                        'rgba(255, 99, 132, 0.2)',
                        'rgba(54, 162, 235, 0.2)',
                        'rgba(255, 205, 86, 0.2)',
                        'rgba(75, 192, 192, 0.2)',
                        'rgba(153, 102, 255, 0.2)'
                    ],
                    borderColor: [
                        'rgba(255, 99, 132, 1)',
                        'rgba(54, 162, 235, 1)',
                        'rgba(255, 205, 86, 1)',
                        'rgba(75, 192, 192, 1)',
                        'rgba(153, 102, 255, 1)'
                    ],
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                plugins: {
                    legend: {
                        position: 'bottom',
                    },
                    title: {
                        display: true,
                        text: 'Revenue by Category'
                    }
                }
            }
        });
    }

    /**
     * Refresh dashboard data
     */
    async refresh() {
        await this.initialize();
    }

    /**
     * View payment details
     */
    viewPayment(payment) {
        this.app.navigateToPage('payment-details', { paymentId: payment.id });
    }

    /**
     * View customer details
     */
    viewCustomer(customer) {
        this.app.navigateToPage('customer-details', { customerId: customer.id });
    }

    /**
     * Dismiss alert
     */
    async dismissAlert(alert) {
        try {
            await this.app.apiRequest('DELETE', `/api/alerts/${alert.id}`);
            this.alerts.remove(alert);
            this.app.showSuccess('Alert dismissed');
        } catch (error) {
            this.app.showError('Failed to dismiss alert');
        }
    }

    /**
     * Handle resize
     */
    handleResize() {
        // Resize charts if needed
        if (this.chartData()) {
            this.initializeCharts();
        }
    }

    /**
     * Save data
     */
    saveData() {
        // No data to save on dashboard
    }
}
