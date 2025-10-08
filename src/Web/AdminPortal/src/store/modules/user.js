import { login, logout, getUserInfo, refreshToken } from '@/api/auth'
import { getToken, setToken, removeToken } from '@/utils/auth'
import { resetRouter } from '@/router'

const state = {
  token: getToken(),
  userInfo: null,
  roles: [],
  permissions: [],
  avatar: '',
  name: '',
  email: '',
  department: '',
  position: '',
  lastLoginTime: null
}

const mutations = {
  SET_TOKEN: (state, token) => {
    state.token = token
  },
  SET_USER_INFO: (state, userInfo) => {
    state.userInfo = userInfo
    state.name = userInfo.name || userInfo.displayName || ''
    state.email = userInfo.email || userInfo.mail || ''
    state.avatar = userInfo.avatar || userInfo.picture || ''
    state.department = userInfo.department || ''
    state.position = userInfo.position || userInfo.jobTitle || ''
    state.lastLoginTime = userInfo.lastLoginTime || new Date().toISOString()
  },
  SET_ROLES: (state, roles) => {
    state.roles = roles
  },
  SET_PERMISSIONS: (state, permissions) => {
    state.permissions = permissions
  },
  SET_AVATAR: (state, avatar) => {
    state.avatar = avatar
  },
  SET_NAME: (state, name) => {
    state.name = name
  },
  SET_EMAIL: (state, email) => {
    state.email = email
  },
  SET_DEPARTMENT: (state, department) => {
    state.department = department
  },
  SET_POSITION: (state, position) => {
    state.position = position
  },
  SET_LAST_LOGIN_TIME: (state, time) => {
    state.lastLoginTime = time
  }
}

const actions = {
  // User login
  login({ commit }, userInfo) {
    const { username, password, captcha, rememberMe } = userInfo
    return new Promise((resolve, reject) => {
      login({
        username: username.trim(),
        password: password,
        captcha: captcha,
        rememberMe: rememberMe
      }).then(response => {
        const { data } = response
        commit('SET_TOKEN', data.token)
        setToken(data.token)
        
        // Set refresh token if provided
        if (data.refreshToken) {
          localStorage.setItem('refreshToken', data.refreshToken)
        }
        
        resolve(data)
      }).catch(error => {
        reject(error)
      })
    })
  },

  // Get user info
  getUserInfo({ commit, state }) {
    return new Promise((resolve, reject) => {
      getUserInfo(state.token).then(response => {
        const { data } = response

        if (!data) {
          reject('Verification failed, please login again.')
        }

        const { roles, permissions, ...userInfo } = data

        // Roles must be a non-empty array
        if (!roles || roles.length <= 0) {
          reject('getInfo: roles must be a non-null array!')
        }

        commit('SET_ROLES', roles)
        commit('SET_PERMISSIONS', permissions)
        commit('SET_USER_INFO', userInfo)
        
        resolve(data)
      }).catch(error => {
        reject(error)
      })
    })
  },

  // Refresh token
  refreshToken({ commit, state }) {
    return new Promise((resolve, reject) => {
      const refreshTokenValue = localStorage.getItem('refreshToken')
      
      if (!refreshTokenValue) {
        reject('No refresh token available')
        return
      }

      refreshToken(refreshTokenValue).then(response => {
        const { data } = response
        commit('SET_TOKEN', data.token)
        setToken(data.token)
        
        if (data.refreshToken) {
          localStorage.setItem('refreshToken', data.refreshToken)
        }
        
        resolve(data)
      }).catch(error => {
        reject(error)
      })
    })
  },

  // User logout
  logout({ commit, state }) {
    return new Promise((resolve, reject) => {
      logout(state.token).then(() => {
        commit('SET_TOKEN', '')
        commit('SET_ROLES', [])
        commit('SET_PERMISSIONS', [])
        commit('SET_USER_INFO', null)
        removeToken()
        localStorage.removeItem('refreshToken')
        resetRouter()
        resolve()
      }).catch(error => {
        reject(error)
      })
    })
  },

  // Reset token
  resetToken({ commit }) {
    return new Promise(resolve => {
      commit('SET_TOKEN', '')
      commit('SET_ROLES', [])
      commit('SET_PERMISSIONS', [])
      commit('SET_USER_INFO', null)
      removeToken()
      localStorage.removeItem('refreshToken')
      resetRouter()
      resolve()
    })
  },

  // Update user info
  updateUserInfo({ commit }, userInfo) {
    commit('SET_USER_INFO', userInfo)
  },

  // Update avatar
  updateAvatar({ commit }, avatar) {
    commit('SET_AVATAR', avatar)
  },

  // Update name
  updateName({ commit }, name) {
    commit('SET_NAME', name)
  },

  // Update email
  updateEmail({ commit }, email) {
    commit('SET_EMAIL', email)
  },

  // Update department
  updateDepartment({ commit }, department) {
    commit('SET_DEPARTMENT', department)
  },

  // Update position
  updatePosition({ commit }, position) {
    commit('SET_POSITION', position)
  }
}

const getters = {
  token: state => state.token,
  userInfo: state => state.userInfo,
  roles: state => state.roles,
  permissions: state => state.permissions,
  avatar: state => state.avatar,
  name: state => state.name,
  email: state => state.email,
  department: state => state.department,
  position: state => state.position,
  lastLoginTime: state => state.lastLoginTime,
  isLoggedIn: state => !!state.token,
  hasRole: (state) => (role) => {
    return state.roles.includes(role)
  },
  hasAnyRole: (state) => (roles) => {
    return roles.some(role => state.roles.includes(role))
  },
  hasPermission: (state) => (permission) => {
    return state.permissions.includes(permission)
  },
  hasAnyPermission: (state) => (permissions) => {
    return permissions.some(permission => state.permissions.includes(permission))
  }
}

export default {
  namespaced: true,
  state,
  mutations,
  actions,
  getters
}
