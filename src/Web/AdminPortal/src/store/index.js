import { createStore } from 'vuex'
import user from './modules/user'
import settings from './modules/settings'
import app from './modules/app'
import permission from './modules/permission'
import tagsView from './modules/tagsView'

const store = createStore({
  modules: {
    user,
    settings,
    app,
    permission,
    tagsView
  },
  strict: process.env.NODE_ENV !== 'production'
})

export default store
