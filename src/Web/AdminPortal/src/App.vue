<template>
  <div id="app" :class="{ 'dark-theme': isDarkTheme }">
    <router-view />
  </div>
</template>

<script>
import { computed } from 'vue'
import { useStore } from 'vuex'

export default {
  name: 'App',
  setup() {
    const store = useStore()
    
    const isDarkTheme = computed(() => store.getters['settings/isDarkTheme'])
    
    return {
      isDarkTheme
    }
  },
  mounted() {
    // Initialize app
    this.$store.dispatch('settings/initSettings')
    this.$store.dispatch('user/getUserInfo')
  }
}
</script>

<style lang="scss">
#app {
  font-family: 'Helvetica Neue', Helvetica, 'PingFang SC', 'Hiragino Sans GB', 'Microsoft YaHei', '微软雅黑', Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  color: #2c3e50;
  height: 100vh;
  overflow: hidden;
}

* {
  box-sizing: border-box;
}

html, body {
  margin: 0;
  padding: 0;
  height: 100%;
  font-size: 14px;
  line-height: 1.5;
}

// Dark theme
.dark-theme {
  color: #e6e6e6;
  background-color: #1a1a1a;
  
  .el-card {
    background-color: #2d2d2d;
    border-color: #404040;
  }
  
  .el-table {
    background-color: #2d2d2d;
    color: #e6e6e6;
  }
  
  .el-table th {
    background-color: #3a3a3a;
    color: #e6e6e6;
  }
  
  .el-table tr {
    background-color: #2d2d2d;
  }
  
  .el-table tr:hover {
    background-color: #3a3a3a;
  }
  
  .el-form-item__label {
    color: #e6e6e6;
  }
  
  .el-input__inner {
    background-color: #3a3a3a;
    border-color: #404040;
    color: #e6e6e6;
  }
  
  .el-button {
    background-color: #3a3a3a;
    border-color: #404040;
    color: #e6e6e6;
  }
  
  .el-button:hover {
    background-color: #4a4a4a;
    border-color: #505050;
  }
}

// Scrollbar styling
::-webkit-scrollbar {
  width: 8px;
  height: 8px;
}

::-webkit-scrollbar-track {
  background: #f1f1f1;
  border-radius: 4px;
}

::-webkit-scrollbar-thumb {
  background: #c1c1c1;
  border-radius: 4px;
}

::-webkit-scrollbar-thumb:hover {
  background: #a8a8a8;
}

.dark-theme {
  ::-webkit-scrollbar-track {
    background: #2d2d2d;
  }
  
  ::-webkit-scrollbar-thumb {
    background: #555;
  }
  
  ::-webkit-scrollbar-thumb:hover {
    background: #777;
  }
}

// Loading animation
.loading {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 100%;
  
  .el-loading-spinner {
    .el-loading-text {
      color: #409eff;
      margin: 3px 0;
      font-size: 14px;
    }
    
    .circular {
      width: 42px;
      height: 42px;
    }
  }
}

// Fade transition
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

// Slide transition
.slide-enter-active,
.slide-leave-active {
  transition: transform 0.3s ease;
}

.slide-enter-from {
  transform: translateX(-100%);
}

.slide-leave-to {
  transform: translateX(100%);
}

// Responsive utilities
@media (max-width: 768px) {
  .hidden-xs-only {
    display: none !important;
  }
}

@media (min-width: 769px) and (max-width: 1024px) {
  .hidden-sm-and-down {
    display: none !important;
  }
}

@media (min-width: 1025px) {
  .hidden-md-and-up {
    display: none !important;
  }
}

// Print styles
@media print {
  .no-print {
    display: none !important;
  }
  
  .print-only {
    display: block !important;
  }
}
</style>
