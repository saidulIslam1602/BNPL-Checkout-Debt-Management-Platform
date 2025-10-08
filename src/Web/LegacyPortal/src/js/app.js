/**
 * YourCompany BNPL Legacy Portal Application
 * Main application entry point using Knockout.js
 */

class LegacyPortalApp {
    constructor() {
        this.currentPage = ko.observable('dashboard');
        this.currentPageData = ko.observable({});
        this.currentUser = ko.observable({
            id: null,
            name: 'Loading...',
            email: '',
            role: '',
            permissions: []
        });
        
        this.isLoading = ko.observable(false);
        this.notifications = ko.observableArray([]);
        this.unreadNotifications = ko.computed(() => {
            return this.notifications().filter(n => !n.read);
        });
        
        this.initializeApp();
    }

    /**
     * Initialize the application
     */
    initializeApp() {
        this.setupEventHandlers();
        this.loadUserData();
        this.setupWebSocket();
        this.initializeRouting();
    }

    /**
     * Setup event handlers
     */
    setupEventHandlers() {
        // Handle window resize
        $(window).on('resize', () => {
            this.handleWindowResize();
        });

        // Handle beforeunload
        $(window).on('beforeunload', () => {
            this.handleBeforeUnload();
        });

        // Handle online/offline status
        $(window).on('online', () => {
            this.handleOnlineStatus();
        });

        $(window).on('offline', () => {
            this.handleOfflineStatus();
        });
    }

    /**
     * Load user data
     */
    async loadUserData() {
        try {
            this.isLoading(true);
            
            const token = Cookies.get('auth_token');
            if (!token) {
                this.redirectToLogin();
                return;
            }

            const response = await axios.get('/api/auth/userinfo', {
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });

            this.currentUser(response.data);
            this.loadNotifications();
        } catch (error) {
            console.error('Error loading user data:', error);
            this.handleAuthError(error);
        } finally {
            this.isLoading(false);
        }
    }

    /**
     * Load notifications
     */
    async loadNotifications() {
        try {
            const response = await axios.get('/api/notifications', {
                params: {
                    limit: 10,
                    unread: false
                }
            });

            this.notifications(response.data.notifications || []);
        } catch (error) {
            console.error('Error loading notifications:', error);
        }
    }

    /**
     * Setup WebSocket connection
     */
    setupWebSocket() {
        const token = Cookies.get('auth_token');
        if (!token) return;

        this.socket = io('/', {
            auth: {
                token: token
            }
        });

        this.socket.on('connect', () => {
            console.log('WebSocket connected');
            this.subscribeToNotifications();
        });

        this.socket.on('disconnect', () => {
            console.log('WebSocket disconnected');
        });

        this.socket.on('notification:created', (notification) => {
            this.handleNewNotification(notification);
        });

        this.socket.on('payment:updated', (payment) => {
            this.handlePaymentUpdate(payment);
        });

        this.socket.on('error', (error) => {
            console.error('WebSocket error:', error);
        });
    }

    /**
     * Subscribe to notifications
     */
    subscribeToNotifications() {
        if (this.socket) {
            this.socket.emit('subscribe:notifications');
        }
    }

    /**
     * Handle new notification
     */
    handleNewNotification(notification) {
        this.notifications.unshift(notification);
        
        // Show toast notification
        toastr.info(notification.message, notification.title, {
            timeOut: 5000,
            closeButton: true,
            progressBar: true
        });
    }

    /**
     * Handle payment update
     */
    handlePaymentUpdate(payment) {
        // Update payment data if current page is payments
        if (this.currentPage() === 'payments') {
            this.currentPageData().updatePayment(payment);
        }
    }

    /**
     * Initialize routing
     */
    initializeRouting() {
        // Handle hash changes
        $(window).on('hashchange', () => {
            this.handleRouteChange();
        });

        // Handle initial route
        this.handleRouteChange();
    }

    /**
     * Handle route changes
     */
    handleRouteChange() {
        const hash = window.location.hash.substring(1) || 'dashboard';
        this.navigateToPage(hash);
    }

    /**
     * Navigate to a specific page
     */
    navigateToPage(page, data = {}) {
        this.currentPage(page);
        this.currentPageData(data);
        window.location.hash = page;
    }

    /**
     * Navigation methods
     */
    navigateToDashboard() {
        this.navigateToPage('dashboard', new DashboardViewModel(this));
    }

    navigateToPayments() {
        this.navigateToPage('payments', new PaymentsViewModel(this));
    }

    navigateToCustomers() {
        this.navigateToPage('customers', new CustomersViewModel(this));
    }

    navigateToMerchants() {
        this.navigateToPage('merchants', new MerchantsViewModel(this));
    }

    navigateToReports() {
        this.navigateToPage('reports', new ReportsViewModel(this));
    }

    navigateToLogs() {
        this.navigateToPage('logs', new LogsViewModel(this));
    }

    navigateToMonitoring() {
        this.navigateToPage('monitoring', new MonitoringViewModel(this));
    }

    navigateToProfile() {
        this.navigateToPage('profile', new ProfileViewModel(this));
    }

    navigateToSettings() {
        this.navigateToPage('settings', new SettingsViewModel(this));
    }

    /**
     * Logout
     */
    async logout() {
        try {
            await axios.post('/api/auth/logout');
            Cookies.remove('auth_token');
            this.redirectToLogin();
        } catch (error) {
            console.error('Error during logout:', error);
            // Still redirect to login even if logout fails
            this.redirectToLogin();
        }
    }

    /**
     * Redirect to login page
     */
    redirectToLogin() {
        window.location.href = '/login';
    }

    /**
     * Handle authentication error
     */
    handleAuthError(error) {
        if (error.response && error.response.status === 401) {
            this.redirectToLogin();
        } else {
            toastr.error('Authentication error. Please try again.');
        }
    }

    /**
     * Handle window resize
     */
    handleWindowResize() {
        // Update any responsive components
        if (this.currentPageData() && this.currentPageData().handleResize) {
            this.currentPageData().handleResize();
        }
    }

    /**
     * Handle before unload
     */
    handleBeforeUnload() {
        // Save any unsaved data
        if (this.currentPageData() && this.currentPageData().saveData) {
            this.currentPageData().saveData();
        }
    }

    /**
     * Handle online status
     */
    handleOnlineStatus() {
        toastr.success('Connection restored', 'Online');
    }

    /**
     * Handle offline status
     */
    handleOfflineStatus() {
        toastr.warning('Connection lost', 'Offline');
    }

    /**
     * Show loading state
     */
    showLoading() {
        this.isLoading(true);
    }

    /**
     * Hide loading state
     */
    hideLoading() {
        this.isLoading(false);
    }

    /**
     * Show error message
     */
    showError(message, title = 'Error') {
        toastr.error(message, title);
    }

    /**
     * Show success message
     */
    showSuccess(message, title = 'Success') {
        toastr.success(message, title);
    }

    /**
     * Show info message
     */
    showInfo(message, title = 'Info') {
        toastr.info(message, title);
    }

    /**
     * Show warning message
     */
    showWarning(message, title = 'Warning') {
        toastr.warning(message, title);
    }

    /**
     * Confirm action
     */
    confirmAction(message, title = 'Confirm') {
        return new Promise((resolve) => {
            Swal.fire({
                title: title,
                text: message,
                icon: 'question',
                showCancelButton: true,
                confirmButtonText: 'Yes',
                cancelButtonText: 'No'
            }).then((result) => {
                resolve(result.isConfirmed);
            });
        });
    }

    /**
     * Format currency
     */
    formatCurrency(amount, currency = 'NOK') {
        return numeral(amount).format('0,0.00') + ' ' + currency;
    }

    /**
     * Format date
     */
    formatDate(date, format = 'DD/MM/YYYY') {
        return moment(date).format(format);
    }

    /**
     * Format datetime
     */
    formatDateTime(date, format = 'DD/MM/YYYY HH:mm') {
        return moment(date).format(format);
    }

    /**
     * Get relative time
     */
    getRelativeTime(date) {
        return moment(date).fromNow();
    }

    /**
     * Make API request
     */
    async apiRequest(method, url, data = null) {
        try {
            const config = {
                method,
                url,
                headers: {
                    'Authorization': `Bearer ${Cookies.get('auth_token')}`
                }
            };

            if (data) {
                config.data = data;
            }

            const response = await axios(config);
            return response.data;
        } catch (error) {
            console.error('API request error:', error);
            throw error;
        }
    }
}

// Initialize the application when DOM is ready
$(document).ready(() => {
    window.app = new LegacyPortalApp();
});
