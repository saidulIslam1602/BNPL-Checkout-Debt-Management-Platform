import { createRouter, createWebHistory } from 'vue-router'
import store from '@/store'
import NProgress from 'nprogress'
import 'nprogress/nprogress.css'

// Configure NProgress
NProgress.configure({ showSpinner: false })

// Route components
const Layout = () => import('@/layout/index.vue')
const Login = () => import('@/views/login/index.vue')
const Dashboard = () => import('@/views/dashboard/index.vue')
const Users = () => import('@/views/users/index.vue')
const Merchants = () => import('@/views/merchants/index.vue')
const Payments = () => import('@/views/payments/index.vue')
const RiskManagement = () => import('@/views/risk-management/index.vue')
const Reports = () => import('@/views/reports/index.vue')
const Settings = () => import('@/views/settings/index.vue')
const SystemLogs = () => import('@/views/system-logs/index.vue')
const NotFound = () => import('@/views/error/404.vue')

// Route configuration
const routes = [
  {
    path: '/login',
    name: 'Login',
    component: Login,
    meta: {
      title: 'Login',
      requiresAuth: false,
      hideInMenu: true
    }
  },
  {
    path: '/',
    component: Layout,
    redirect: '/dashboard',
    meta: {
      requiresAuth: true
    },
    children: [
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: Dashboard,
        meta: {
          title: 'Dashboard',
          icon: 'Odometer',
          requiresAuth: true
        }
      }
    ]
  },
  {
    path: '/users',
    component: Layout,
    meta: {
      title: 'User Management',
      icon: 'User',
      requiresAuth: true,
      roles: ['admin', 'user-manager']
    },
    children: [
      {
        path: '',
        name: 'Users',
        component: Users,
        meta: {
          title: 'Users',
          requiresAuth: true,
          roles: ['admin', 'user-manager']
        }
      }
    ]
  },
  {
    path: '/merchants',
    component: Layout,
    meta: {
      title: 'Merchant Management',
      icon: 'Shop',
      requiresAuth: true,
      roles: ['admin', 'merchant-manager']
    },
    children: [
      {
        path: '',
        name: 'Merchants',
        component: Merchants,
        meta: {
          title: 'Merchants',
          requiresAuth: true,
          roles: ['admin', 'merchant-manager']
        }
      }
    ]
  },
  {
    path: '/payments',
    component: Layout,
    meta: {
      title: 'Payment Management',
      icon: 'CreditCard',
      requiresAuth: true,
      roles: ['admin', 'payment-manager']
    },
    children: [
      {
        path: '',
        name: 'Payments',
        component: Payments,
        meta: {
          title: 'Payments',
          requiresAuth: true,
          roles: ['admin', 'payment-manager']
        }
      }
    ]
  },
  {
    path: '/risk-management',
    component: Layout,
    meta: {
      title: 'Risk Management',
      icon: 'Warning',
      requiresAuth: true,
      roles: ['admin', 'risk-manager']
    },
    children: [
      {
        path: '',
        name: 'RiskManagement',
        component: RiskManagement,
        meta: {
          title: 'Risk Management',
          requiresAuth: true,
          roles: ['admin', 'risk-manager']
        }
      }
    ]
  },
  {
    path: '/reports',
    component: Layout,
    meta: {
      title: 'Reports & Analytics',
      icon: 'DataAnalysis',
      requiresAuth: true,
      roles: ['admin', 'analyst']
    },
    children: [
      {
        path: '',
        name: 'Reports',
        component: Reports,
        meta: {
          title: 'Reports',
          requiresAuth: true,
          roles: ['admin', 'analyst']
        }
      }
    ]
  },
  {
    path: '/settings',
    component: Layout,
    meta: {
      title: 'System Settings',
      icon: 'Setting',
      requiresAuth: true,
      roles: ['admin']
    },
    children: [
      {
        path: '',
        name: 'Settings',
        component: Settings,
        meta: {
          title: 'Settings',
          requiresAuth: true,
          roles: ['admin']
        }
      }
    ]
  },
  {
    path: '/system-logs',
    component: Layout,
    meta: {
      title: 'System Logs',
      icon: 'Document',
      requiresAuth: true,
      roles: ['admin', 'system-admin']
    },
    children: [
      {
        path: '',
        name: 'SystemLogs',
        component: SystemLogs,
        meta: {
          title: 'System Logs',
          requiresAuth: true,
          roles: ['admin', 'system-admin']
        }
      }
    ]
  },
  {
    path: '/404',
    name: 'NotFound',
    component: NotFound,
    meta: {
      title: 'Page Not Found',
      hideInMenu: true
    }
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: '/404'
  }
]

// Create router instance
const router = createRouter({
  history: createWebHistory(process.env.BASE_URL),
  routes,
  scrollBehavior(to, from, savedPosition) {
    if (savedPosition) {
      return savedPosition
    } else {
      return { top: 0 }
    }
  }
})

// Global navigation guards
router.beforeEach(async (to, from, next) => {
  // Start progress bar
  NProgress.start()

  // Get user info from store
  const userInfo = store.getters['user/userInfo']
  const token = store.getters['user/token']

  // Check if route requires authentication
  if (to.meta.requiresAuth) {
    if (!token) {
      // No token, redirect to login
      next({
        path: '/login',
        query: { redirect: to.fullPath }
      })
      return
    }

    // Check if user info is loaded
    if (!userInfo || !userInfo.id) {
      try {
        await store.dispatch('user/getUserInfo')
      } catch (error) {
        // Failed to get user info, redirect to login
        store.dispatch('user/logout')
        next({
          path: '/login',
          query: { redirect: to.fullPath }
        })
        return
      }
    }

    // Check role-based access
    if (to.meta.roles && to.meta.roles.length > 0) {
      const userRoles = store.getters['user/roles']
      const hasRole = to.meta.roles.some(role => userRoles.includes(role))
      
      if (!hasRole) {
        // No permission, redirect to 404 or dashboard
        next({ path: '/404' })
        return
      }
    }
  } else if (to.path === '/login' && token) {
    // Already logged in, redirect to dashboard
    next({ path: '/' })
    return
  }

  // Set page title
  if (to.meta.title) {
    document.title = `${to.meta.title} - YourCompany BNPL Admin Portal`
  }

  next()
})

router.afterEach(() => {
  // Finish progress bar
  NProgress.done()
})

// Export router
export default router
