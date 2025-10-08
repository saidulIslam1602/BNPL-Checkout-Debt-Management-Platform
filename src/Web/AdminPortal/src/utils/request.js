import axios from 'axios'
import { ElMessage, ElMessageBox } from 'element-plus'
import store from '@/store'
import { getToken } from '@/utils/auth'
import router from '@/router'

// Create axios instance
const service = axios.create({
  baseURL: process.env.VUE_APP_BASE_API || '/api',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json'
  }
})

// Request interceptor
service.interceptors.request.use(
  config => {
    // Add token to request headers
    const token = getToken()
    if (token) {
      config.headers['Authorization'] = `Bearer ${token}`
    }

    // Add request timestamp
    config.metadata = { startTime: new Date() }

    // Add request ID for tracking
    config.headers['X-Request-ID'] = generateRequestId()

    return config
  },
  error => {
    console.error('Request error:', error)
    return Promise.reject(error)
  }
)

// Response interceptor
service.interceptors.response.use(
  response => {
    // Calculate request duration
    const duration = new Date() - response.config.metadata.startTime
    console.log(`Request ${response.config.url} completed in ${duration}ms`)

    const res = response.data

    // Handle different response codes
    if (res.code !== undefined) {
      // Custom response format
      if (res.code === 200) {
        return res
      } else if (res.code === 401) {
        // Unauthorized
        handleUnauthorized()
        return Promise.reject(new Error(res.message || 'Unauthorized'))
      } else if (res.code === 403) {
        // Forbidden
        ElMessage.error(res.message || 'Access denied')
        return Promise.reject(new Error(res.message || 'Access denied'))
      } else if (res.code === 404) {
        // Not found
        ElMessage.error(res.message || 'Resource not found')
        return Promise.reject(new Error(res.message || 'Resource not found'))
      } else if (res.code === 500) {
        // Server error
        ElMessage.error(res.message || 'Server error')
        return Promise.reject(new Error(res.message || 'Server error'))
      } else {
        // Other errors
        ElMessage.error(res.message || 'Request failed')
        return Promise.reject(new Error(res.message || 'Request failed'))
      }
    } else {
      // Direct response
      return res
    }
  },
  error => {
    console.error('Response error:', error)

    let message = 'Network error'
    let code = 0

    if (error.response) {
      // Server responded with error status
      code = error.response.status
      const data = error.response.data

      switch (code) {
        case 400:
          message = data?.message || 'Bad request'
          break
        case 401:
          message = data?.message || 'Unauthorized'
          handleUnauthorized()
          break
        case 403:
          message = data?.message || 'Access denied'
          break
        case 404:
          message = data?.message || 'Resource not found'
          break
        case 408:
          message = data?.message || 'Request timeout'
          break
        case 409:
          message = data?.message || 'Conflict'
          break
        case 422:
          message = data?.message || 'Validation error'
          break
        case 429:
          message = data?.message || 'Too many requests'
          break
        case 500:
          message = data?.message || 'Internal server error'
          break
        case 502:
          message = data?.message || 'Bad gateway'
          break
        case 503:
          message = data?.message || 'Service unavailable'
          break
        case 504:
          message = data?.message || 'Gateway timeout'
          break
        default:
          message = data?.message || `Request failed with status ${code}`
      }
    } else if (error.request) {
      // Request was made but no response received
      message = 'Network error - no response received'
    } else {
      // Something else happened
      message = error.message || 'Request failed'
    }

    // Show error message
    if (code !== 401) {
      ElMessage.error(message)
    }

    return Promise.reject(error)
  }
)

// Handle unauthorized access
function handleUnauthorized() {
  ElMessageBox.confirm(
    'Your session has expired. Please login again.',
    'Session Expired',
    {
      confirmButtonText: 'Login',
      cancelButtonText: 'Cancel',
      type: 'warning'
    }
  ).then(() => {
    store.dispatch('user/logout').then(() => {
      router.push('/login')
    })
  }).catch(() => {
    // User cancelled, still redirect to login
    store.dispatch('user/logout').then(() => {
      router.push('/login')
    })
  })
}

// Generate unique request ID
function generateRequestId() {
  return 'req_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9)
}

// Request methods
export const request = {
  get(url, params = {}, config = {}) {
    return service.get(url, { params, ...config })
  },
  post(url, data = {}, config = {}) {
    return service.post(url, data, config)
  },
  put(url, data = {}, config = {}) {
    return service.put(url, data, config)
  },
  delete(url, config = {}) {
    return service.delete(url, config)
  },
  patch(url, data = {}, config = {}) {
    return service.patch(url, data, config)
  },
  upload(url, formData, config = {}) {
    return service.post(url, formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      },
      ...config
    })
  },
  download(url, params = {}, config = {}) {
    return service.get(url, {
      params,
      responseType: 'blob',
      ...config
    })
  }
}

export default service
