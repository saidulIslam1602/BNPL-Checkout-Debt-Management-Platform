<template>
  <div class="login-container">
    <div class="login-form">
      <div class="login-header">
        <img src="@/assets/logo.png" alt="YourCompany" class="logo" />
        <h1>YourCompany BNPL</h1>
        <p>Admin Portal</p>
      </div>

      <el-form
        ref="loginFormRef"
        :model="loginForm"
        :rules="loginRules"
        class="login-form-content"
        autocomplete="on"
        label-position="left"
      >
        <div class="title-container">
          <h3 class="title">Login to Admin Portal</h3>
        </div>

        <!-- Username/Email -->
        <el-form-item prop="username">
          <span class="svg-container">
            <el-icon><User /></el-icon>
          </span>
          <el-input
            ref="username"
            v-model="loginForm.username"
            placeholder="Username or Email"
            name="username"
            type="text"
            tabindex="1"
            autocomplete="on"
          />
        </el-form-item>

        <!-- Password -->
        <el-tooltip v-model="capsTooltip" content="Caps lock is On" placement="right" manual>
          <el-form-item prop="password">
            <span class="svg-container">
              <el-icon><Lock /></el-icon>
            </span>
            <el-input
              :key="passwordType"
              ref="passwordRef"
              v-model="loginForm.password"
              :type="passwordType"
              placeholder="Password"
              name="password"
              tabindex="2"
              autocomplete="on"
              @keyup="checkCapslock"
              @blur="capsTooltip = false"
              @keyup.enter="handleLogin"
            />
            <span class="show-pwd" @click="showPwd">
              <el-icon v-if="passwordType === 'password'"><View /></el-icon>
              <el-icon v-else><Hide /></el-icon>
            </span>
          </el-form-item>
        </el-tooltip>

        <!-- Captcha -->
        <el-form-item v-if="showCaptcha" prop="captcha">
          <span class="svg-container">
            <el-icon><Picture /></el-icon>
          </span>
          <el-input
            v-model="loginForm.captcha"
            placeholder="Captcha"
            name="captcha"
            type="text"
            tabindex="3"
            autocomplete="off"
            style="width: 60%"
            @keyup.enter="handleLogin"
          />
          <div class="captcha-image" @click="refreshCaptcha">
            <img :src="captchaImage" alt="Captcha" />
          </div>
        </el-form-item>

        <!-- Remember Me -->
        <el-form-item>
          <el-checkbox v-model="loginForm.rememberMe" label="Remember me" />
        </el-form-item>

        <!-- Login Button -->
        <el-button
          :loading="loading"
          type="primary"
          style="width: 100%; margin-bottom: 30px"
          @click.prevent="handleLogin"
        >
          Login
        </el-button>

        <!-- Alternative Login Methods -->
        <div class="alternative-login">
          <el-divider>Or login with</el-divider>
          
          <div class="login-methods">
            <!-- SAML Login -->
            <el-button
              v-if="samlProviders.length > 0"
              v-for="provider in samlProviders"
              :key="`saml-${provider.id}`"
              :icon="getProviderIcon(provider.id)"
              class="login-method-btn"
              @click="handleSamlLogin(provider.id)"
            >
              {{ provider.name }}
            </el-button>

            <!-- OpenID Connect Login -->
            <el-button
              v-if="oidcProviders.length > 0"
              v-for="provider in oidcProviders"
              :key="`oidc-${provider.id}`"
              :icon="getProviderIcon(provider.id)"
              class="login-method-btn"
              @click="handleOidcLogin(provider.id)"
            >
              {{ provider.name }}
            </el-button>

            <!-- Azure AD Login -->
            <el-button
              v-if="azureAdEnabled"
              icon="Microsoft"
              class="login-method-btn"
              @click="handleAzureAdLogin"
            >
              Microsoft
            </el-button>
          </div>
        </div>

        <!-- Forgot Password -->
        <div class="forgot-password">
          <el-link type="primary" @click="handleForgotPassword">
            Forgot your password?
          </el-link>
        </div>
      </el-form>
    </div>

    <!-- Background -->
    <div class="login-background">
      <div class="background-shapes">
        <div class="shape shape-1"></div>
        <div class="shape shape-2"></div>
        <div class="shape shape-3"></div>
      </div>
    </div>
  </div>
</template>

<script>
import { reactive, ref, onMounted, nextTick } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useStore } from 'vuex'
import { ElMessage, ElMessageBox } from 'element-plus'
import { User, Lock, View, Hide, Picture } from '@element-plus/icons-vue'
import { getSamlProviders, getOidcProviders } from '@/api/auth'
import { isAccountLocked, isLockoutExpired, clearLockout, incrementLoginAttempts, resetLoginAttempts } from '@/utils/auth'

export default {
  name: 'Login',
  components: {
    User,
    Lock,
    View,
    Hide,
    Picture
  },
  setup() {
    const router = useRouter()
    const route = useRoute()
    const store = useStore()

    // Form data
    const loginForm = reactive({
      username: '',
      password: '',
      captcha: '',
      rememberMe: false
    })

    // Form validation rules
    const loginRules = {
      username: [
        { required: true, message: 'Please enter your username or email', trigger: 'blur' },
        { min: 3, max: 50, message: 'Length should be 3 to 50', trigger: 'blur' }
      ],
      password: [
        { required: true, message: 'Please enter your password', trigger: 'blur' },
        { min: 6, max: 20, message: 'Length should be 6 to 20', trigger: 'blur' }
      ],
      captcha: [
        { required: true, message: 'Please enter the captcha', trigger: 'blur' }
      ]
    }

    // Component state
    const loading = ref(false)
    const passwordType = ref('password')
    const capsTooltip = ref(false)
    const showCaptcha = ref(false)
    const captchaImage = ref('')
    const samlProviders = ref([])
    const oidcProviders = ref([])
    const azureAdEnabled = ref(false)

    // Form references
    const loginFormRef = ref(null)
    const passwordRef = ref(null)

    // Methods
    const showPwd = () => {
      if (passwordType.value === 'password') {
        passwordType.value = ''
      } else {
        passwordType.value = 'password'
      }
      nextTick(() => {
        passwordRef.value.focus()
      })
    }

    const checkCapslock = (e) => {
      const { key } = e
      capsTooltip.value = key && key.length === 1 && (key >= 'A' && key <= 'Z')
    }

    const refreshCaptcha = () => {
      captchaImage.value = `/api/auth/captcha?t=${Date.now()}`
    }

    const getProviderIcon = (providerId) => {
      const iconMap = {
        'google': 'Google',
        'microsoft': 'Microsoft',
        'azuread': 'Microsoft',
        'bankid': 'CreditCard',
        'feide': 'School',
        'generic': 'User'
      }
      return iconMap[providerId] || 'User'
    }

    const handleLogin = () => {
      // Check if account is locked
      if (isAccountLocked() && !isLockoutExpired()) {
        ElMessage.error('Account is temporarily locked due to too many failed login attempts. Please try again later.')
        return
      }

      // Clear lockout if expired
      if (isLockoutExpired()) {
        clearLockout()
      }

      loginFormRef.value.validate((valid) => {
        if (valid) {
          loading.value = true
          store.dispatch('user/login', loginForm)
            .then(() => {
              ElMessage.success('Login successful')
              resetLoginAttempts()
              
              // Redirect to intended page or dashboard
              const redirect = route.query.redirect || '/'
              router.push(redirect)
            })
            .catch((error) => {
              console.error('Login error:', error)
              
              // Increment login attempts
              const attempts = incrementLoginAttempts()
              
              if (attempts >= 3) {
                showCaptcha.value = true
                refreshCaptcha()
              }
              
              if (attempts >= 5) {
                setLockoutTime()
                ElMessage.error('Too many failed login attempts. Account locked for 15 minutes.')
              } else {
                ElMessage.error(error.message || 'Login failed')
              }
            })
            .finally(() => {
              loading.value = false
            })
        } else {
          ElMessage.error('Please fill in all required fields')
          return false
        }
      })
    }

    const handleSamlLogin = (providerId) => {
      const returnUrl = route.query.redirect || '/'
      window.location.href = `/api/saml/login/${providerId}?returnUrl=${encodeURIComponent(returnUrl)}`
    }

    const handleOidcLogin = (providerId) => {
      const returnUrl = route.query.redirect || '/'
      window.location.href = `/api/oidc/login/${providerId}?returnUrl=${encodeURIComponent(returnUrl)}`
    }

    const handleAzureAdLogin = () => {
      const returnUrl = route.query.redirect || '/'
      window.location.href = `/api/azuread/login?returnUrl=${encodeURIComponent(returnUrl)}`
    }

    const handleForgotPassword = () => {
      ElMessageBox.prompt('Please enter your email address', 'Forgot Password', {
        confirmButtonText: 'Send Reset Email',
        cancelButtonText: 'Cancel',
        inputPattern: /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/,
        inputErrorMessage: 'Please enter a valid email address'
      }).then(({ value }) => {
        // Call forgot password API
        ElMessage.success('Password reset email sent')
      }).catch(() => {
        // User cancelled
      })
    }

    const loadProviders = async () => {
      try {
        // Load SAML providers
        const samlResponse = await getSamlProviders()
        samlProviders.value = samlResponse.data?.providers || []

        // Load OpenID Connect providers
        const oidcResponse = await getOidcProviders()
        oidcProviders.value = oidcResponse.data?.providers || []

        // Check if Azure AD is enabled
        azureAdEnabled.value = true // This should be determined from API response
      } catch (error) {
        console.error('Error loading providers:', error)
      }
    }

    // Lifecycle
    onMounted(() => {
      loadProviders()
      
      // Check if user is already logged in
      if (store.getters['user/isLoggedIn']) {
        router.push('/')
      }
    })

    return {
      loginForm,
      loginRules,
      loading,
      passwordType,
      capsTooltip,
      showCaptcha,
      captchaImage,
      samlProviders,
      oidcProviders,
      azureAdEnabled,
      loginFormRef,
      passwordRef,
      showPwd,
      checkCapslock,
      refreshCaptcha,
      getProviderIcon,
      handleLogin,
      handleSamlLogin,
      handleOidcLogin,
      handleAzureAdLogin,
      handleForgotPassword
    }
  }
}
</script>

<style lang="scss" scoped>
.login-container {
  min-height: 100vh;
  width: 100%;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  overflow: hidden;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
}

.login-form {
  width: 520px;
  max-width: 100%;
  padding: 160px 35px 0;
  margin: 0 auto;
  overflow: hidden;
  background: rgba(255, 255, 255, 0.95);
  backdrop-filter: blur(10px);
  border-radius: 20px;
  box-shadow: 0 20px 40px rgba(0, 0, 0, 0.1);
  position: relative;
  z-index: 2;
}

.login-header {
  text-align: center;
  margin-bottom: 40px;

  .logo {
    width: 80px;
    height: 80px;
    margin-bottom: 20px;
  }

  h1 {
    font-size: 32px;
    font-weight: 600;
    color: #2c3e50;
    margin: 0 0 10px 0;
  }

  p {
    font-size: 16px;
    color: #7f8c8d;
    margin: 0;
  }
}

.login-form-content {
  .title-container {
    position: relative;
    text-align: center;
    margin-bottom: 30px;

    .title {
      font-size: 26px;
      color: #2c3e50;
      margin: 0;
      font-weight: 500;
    }
  }

  .el-form-item {
    border: 1px solid rgba(255, 255, 255, 0.1);
    background: rgba(255, 255, 255, 0.1);
    border-radius: 5px;
    color: #454545;
    position: relative;
    margin-bottom: 20px;

    .svg-container {
      padding: 6px 5px 6px 15px;
      color: #889aa4;
      vertical-align: middle;
      width: 30px;
      display: inline-block;
    }

    .el-input {
      display: inline-block;
      height: 47px;
      width: 85%;

      :deep(.el-input__wrapper) {
        background: transparent;
        border: none;
        border-radius: 0;
        box-shadow: none;
        padding: 0;

        .el-input__inner {
          background: transparent;
          border: none;
          border-radius: 0;
          padding: 12px 5px 12px 15px;
          color: #2c3e50;
          height: 47px;
          caret-color: #2c3e50;

          &:-webkit-autofill {
            box-shadow: 0 0 0px 1000px transparent inset !important;
            -webkit-text-fill-color: #2c3e50 !important;
          }
        }
      }
    }

    .show-pwd {
      position: absolute;
      right: 10px;
      top: 7px;
      font-size: 16px;
      color: #889aa4;
      cursor: pointer;
      user-select: none;
    }
  }

  .captcha-image {
    width: 35%;
    height: 47px;
    float: right;
    cursor: pointer;
    border-radius: 4px;
    overflow: hidden;

    img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }
  }
}

.alternative-login {
  margin: 20px 0;

  .el-divider {
    margin: 20px 0;
    color: #909399;
  }

  .login-methods {
    display: flex;
    flex-wrap: wrap;
    gap: 10px;
    justify-content: center;

    .login-method-btn {
      flex: 1;
      min-width: 120px;
      margin: 0;
    }
  }
}

.forgot-password {
  text-align: center;
  margin-top: 20px;
}

.login-background {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  z-index: 1;

  .background-shapes {
    position: relative;
    width: 100%;
    height: 100%;

    .shape {
      position: absolute;
      border-radius: 50%;
      background: rgba(255, 255, 255, 0.1);
      animation: float 6s ease-in-out infinite;

      &.shape-1 {
        width: 200px;
        height: 200px;
        top: 10%;
        left: 10%;
        animation-delay: 0s;
      }

      &.shape-2 {
        width: 150px;
        height: 150px;
        top: 60%;
        right: 10%;
        animation-delay: 2s;
      }

      &.shape-3 {
        width: 100px;
        height: 100px;
        bottom: 20%;
        left: 20%;
        animation-delay: 4s;
      }
    }
  }
}

@keyframes float {
  0%, 100% {
    transform: translateY(0px);
  }
  50% {
    transform: translateY(-20px);
  }
}

// Responsive design
@media (max-width: 768px) {
  .login-form {
    width: 90%;
    padding: 120px 20px 0;
  }

  .login-header {
    h1 {
      font-size: 24px;
    }

    p {
      font-size: 14px;
    }
  }

  .login-form-content {
    .title-container .title {
      font-size: 20px;
    }
  }
}

// Dark theme support
.dark-theme {
  .login-form {
    background: rgba(45, 45, 45, 0.95);
    color: #e6e6e6;
  }

  .login-header {
    h1 {
      color: #e6e6e6;
    }

    p {
      color: #a8a8a8;
    }
  }

  .login-form-content {
    .title-container .title {
      color: #e6e6e6;
    }

    .el-form-item {
      background: rgba(58, 58, 58, 0.1);
      border-color: rgba(64, 64, 64, 0.1);

      .svg-container {
        color: #a8a8a8;
      }

      .el-input :deep(.el-input__wrapper .el-input__inner) {
        color: #e6e6e6;
        caret-color: #e6e6e6;
      }

      .show-pwd {
        color: #a8a8a8;
      }
    }
  }
}
</style>
