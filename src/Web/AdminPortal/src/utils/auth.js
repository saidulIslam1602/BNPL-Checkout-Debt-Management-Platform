import Cookies from 'js-cookie'

const TokenKey = 'yourcompany_bnpl_admin_token'
const RefreshTokenKey = 'yourcompany_bnpl_admin_refresh_token'
const UserInfoKey = 'yourcompany_bnpl_admin_user_info'

// Token management
export function getToken() {
  return Cookies.get(TokenKey) || localStorage.getItem(TokenKey)
}

export function setToken(token) {
  // Store in both cookies and localStorage for reliability
  Cookies.set(TokenKey, token, { expires: 7 }) // 7 days
  localStorage.setItem(TokenKey, token)
}

export function removeToken() {
  Cookies.remove(TokenKey)
  localStorage.removeItem(TokenKey)
}

// Refresh token management
export function getRefreshToken() {
  return localStorage.getItem(RefreshTokenKey)
}

export function setRefreshToken(token) {
  localStorage.setItem(RefreshTokenKey, token)
}

export function removeRefreshToken() {
  localStorage.removeItem(RefreshTokenKey)
}

// User info management
export function getUserInfo() {
  const userInfo = localStorage.getItem(UserInfoKey)
  return userInfo ? JSON.parse(userInfo) : null
}

export function setUserInfo(userInfo) {
  localStorage.setItem(UserInfoKey, JSON.stringify(userInfo))
}

export function removeUserInfo() {
  localStorage.removeItem(UserInfoKey)
}

// Clear all auth data
export function clearAuthData() {
  removeToken()
  removeRefreshToken()
  removeUserInfo()
}

// Check if user is authenticated
export function isAuthenticated() {
  const token = getToken()
  if (!token) return false

  try {
    // Check if token is expired
    const payload = JSON.parse(atob(token.split('.')[1]))
    const currentTime = Date.now() / 1000
    
    if (payload.exp && payload.exp < currentTime) {
      // Token is expired
      clearAuthData()
      return false
    }
    
    return true
  } catch (error) {
    // Invalid token format
    clearAuthData()
    return false
  }
}

// Get token payload
export function getTokenPayload() {
  const token = getToken()
  if (!token) return null

  try {
    return JSON.parse(atob(token.split('.')[1]))
  } catch (error) {
    return null
  }
}

// Check if token is expired
export function isTokenExpired() {
  const payload = getTokenPayload()
  if (!payload || !payload.exp) return true

  const currentTime = Date.now() / 1000
  return payload.exp < currentTime
}

// Get token expiration time
export function getTokenExpiration() {
  const payload = getTokenPayload()
  if (!payload || !payload.exp) return null

  return new Date(payload.exp * 1000)
}

// Get time until token expires
export function getTimeUntilExpiration() {
  const expiration = getTokenExpiration()
  if (!expiration) return 0

  return Math.max(0, expiration.getTime() - Date.now())
}

// Check if token needs refresh (expires in next 5 minutes)
export function needsRefresh() {
  const timeUntilExpiration = getTimeUntilExpiration()
  return timeUntilExpiration < 5 * 60 * 1000 // 5 minutes
}

// Session management
export function setSessionData(key, value) {
  sessionStorage.setItem(key, JSON.stringify(value))
}

export function getSessionData(key) {
  const data = sessionStorage.getItem(key)
  return data ? JSON.parse(data) : null
}

export function removeSessionData(key) {
  sessionStorage.removeItem(key)
}

export function clearSessionData() {
  sessionStorage.clear()
}

// Remember me functionality
export function setRememberMe(remember) {
  if (remember) {
    localStorage.setItem('remember_me', 'true')
  } else {
    localStorage.removeItem('remember_me')
  }
}

export function getRememberMe() {
  return localStorage.getItem('remember_me') === 'true'
}

// Language preference
export function setLanguage(lang) {
  localStorage.setItem('language', lang)
}

export function getLanguage() {
  return localStorage.getItem('language') || 'en'
}

// Theme preference
export function setTheme(theme) {
  localStorage.setItem('theme', theme)
}

export function getTheme() {
  return localStorage.getItem('theme') || 'light'
}

// Last login time
export function setLastLoginTime(time) {
  localStorage.setItem('last_login_time', time)
}

export function getLastLoginTime() {
  return localStorage.getItem('last_login_time')
}

// Login attempts tracking
export function setLoginAttempts(attempts) {
  sessionStorage.setItem('login_attempts', attempts.toString())
}

export function getLoginAttempts() {
  const attempts = sessionStorage.getItem('login_attempts')
  return attempts ? parseInt(attempts) : 0
}

export function incrementLoginAttempts() {
  const attempts = getLoginAttempts() + 1
  setLoginAttempts(attempts)
  return attempts
}

export function resetLoginAttempts() {
  sessionStorage.removeItem('login_attempts')
}

// Account lockout
export function isAccountLocked() {
  const attempts = getLoginAttempts()
  return attempts >= 5 // Lock after 5 failed attempts
}

export function getLockoutTime() {
  const lockoutTime = sessionStorage.getItem('lockout_time')
  return lockoutTime ? new Date(lockoutTime) : null
}

export function setLockoutTime() {
  const lockoutTime = new Date(Date.now() + 15 * 60 * 1000) // 15 minutes
  sessionStorage.setItem('lockout_time', lockoutTime.toISOString())
}

export function isLockoutExpired() {
  const lockoutTime = getLockoutTime()
  if (!lockoutTime) return true
  
  return new Date() > lockoutTime
}

export function clearLockout() {
  sessionStorage.removeItem('lockout_time')
  resetLoginAttempts()
}
