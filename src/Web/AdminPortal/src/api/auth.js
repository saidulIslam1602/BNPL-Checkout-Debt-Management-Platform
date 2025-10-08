import request from '@/utils/request'

// Authentication API endpoints
export function login(data) {
  return request({
    url: '/api/auth/login',
    method: 'post',
    data
  })
}

export function logout() {
  return request({
    url: '/api/auth/logout',
    method: 'post'
  })
}

export function getUserInfo(token) {
  return request({
    url: '/api/auth/userinfo',
    method: 'get',
    headers: {
      'Authorization': `Bearer ${token}`
    }
  })
}

export function refreshToken(refreshToken) {
  return request({
    url: '/api/auth/refresh',
    method: 'post',
    data: {
      refreshToken
    }
  })
}

export function changePassword(data) {
  return request({
    url: '/api/auth/change-password',
    method: 'post',
    data
  })
}

export function resetPassword(data) {
  return request({
    url: '/api/auth/reset-password',
    method: 'post',
    data
  })
}

export function forgotPassword(email) {
  return request({
    url: '/api/auth/forgot-password',
    method: 'post',
    data: { email }
  })
}

export function verifyEmail(token) {
  return request({
    url: '/api/auth/verify-email',
    method: 'post',
    data: { token }
  })
}

export function resendVerificationEmail(email) {
  return request({
    url: '/api/auth/resend-verification',
    method: 'post',
    data: { email }
  })
}

// SAML Authentication
export function samlLogin(provider, returnUrl) {
  return request({
    url: `/api/saml/login/${provider}`,
    method: 'get',
    params: { returnUrl }
  })
}

export function samlLogout() {
  return request({
    url: '/api/saml/logout',
    method: 'post'
  })
}

export function getSamlProviders() {
  return request({
    url: '/api/saml/providers',
    method: 'get'
  })
}

// OpenID Connect Authentication
export function oidcLogin(provider, returnUrl) {
  return request({
    url: `/api/oidc/login/${provider}`,
    method: 'get',
    params: { returnUrl }
  })
}

export function oidcLogout(provider) {
  return request({
    url: `/api/oidc/logout/${provider}`,
    method: 'post'
  })
}

export function getOidcProviders() {
  return request({
    url: '/api/oidc/providers',
    method: 'get'
  })
}

export function getOidcUserInfo() {
  return request({
    url: '/api/oidc/userinfo',
    method: 'get'
  })
}

// Azure AD Authentication
export function azureAdLogin() {
  return request({
    url: '/api/azuread/login',
    method: 'get'
  })
}

export function azureAdLogout() {
  return request({
    url: '/api/azuread/logout',
    method: 'post'
  })
}

export function getAzureAdUserInfo() {
  return request({
    url: '/api/azuread/me',
    method: 'get'
  })
}

export function getAzureAdUserGroups() {
  return request({
    url: '/api/azuread/me/groups',
    method: 'get'
  })
}

export function getAzureAdUserManager() {
  return request({
    url: '/api/azuread/me/manager',
    method: 'get'
  })
}

export function getAzureAdDirectReports() {
  return request({
    url: '/api/azuread/me/direct-reports',
    method: 'get'
  })
}

export function searchAzureAdUsers(searchTerm) {
  return request({
    url: '/api/azuread/users/search',
    method: 'get',
    params: { searchTerm }
  })
}

export function getAllAzureAdUsers(top = 100, skip = 0) {
  return request({
    url: '/api/azuread/users',
    method: 'get',
    params: { top, skip }
  })
}

export function getAllAzureAdGroups(top = 100, skip = 0) {
  return request({
    url: '/api/azuread/groups',
    method: 'get',
    params: { top, skip }
  })
}

export function getAzureAdUserCalendar(startTime, endTime) {
  return request({
    url: '/api/azuread/me/calendar',
    method: 'get',
    params: { startTime, endTime }
  })
}

export function getAzureAdUserPhoto() {
  return request({
    url: '/api/azuread/me/photo',
    method: 'get',
    responseType: 'blob'
  })
}

export function getAzureAdOrganization() {
  return request({
    url: '/api/azuread/organization',
    method: 'get'
  })
}
