const gulp = require('gulp')
const concat = require('gulp-concat')
const uglify = require('gulp-uglify')
const sass = require('gulp-sass')(require('sass'))
const cleanCSS = require('gulp-clean-css')
const rename = require('gulp-rename')
const sourcemaps = require('gulp-sourcemaps')
const watch = require('gulp-watch')
const browserSync = require('browser-sync').create()
const eslint = require('gulp-eslint')
const del = require('del')
const runSequence = require('run-sequence')
const path = require('path')
const fs = require('fs')

// Configuration
const config = {
  src: {
    // .NET Services
    dotnetServices: 'src/Services/**/*.cs',
    dotnetProjects: 'src/Services/**/*.csproj',
    
    // Web Applications
    webApps: 'src/Web/**/*',
    
    // Frontend Assets
    js: [
      'src/Web/**/*.js',
      '!src/Web/**/node_modules/**',
      '!src/Web/**/dist/**'
    ],
    scss: 'src/Web/**/*.scss',
    css: 'src/Web/**/*.css',
    html: 'src/Web/**/*.html',
    
    // Configuration Files
    config: [
      'src/**/*.json',
      'src/**/*.config',
      'src/**/*.xml'
    ],
    
    // Documentation
    docs: 'docs/**/*',
    
    // Scripts
    scripts: 'scripts/**/*'
  },
  
  dist: {
    root: 'dist',
    services: 'dist/Services',
    web: 'dist/Web',
    docs: 'dist/docs',
    scripts: 'dist/scripts'
  },
  
  // Build targets
  targets: {
    development: {
      minify: false,
      sourcemaps: true,
      watch: true
    },
    production: {
      minify: true,
      sourcemaps: false,
      watch: false
    }
  }
}

// Current build target
let buildTarget = 'development'

// Set build target
function setBuildTarget(target) {
  buildTarget = target
  console.log(`Build target set to: ${buildTarget}`)
}

// Clean task
gulp.task('clean', () => {
  return del([config.dist.root])
})

// Copy .NET services
gulp.task('copy:dotnet-services', () => {
  return gulp.src([
    'src/Services/**/*',
    '!src/Services/**/bin/**',
    '!src/Services/**/obj/**',
    '!src/Services/**/node_modules/**'
  ])
    .pipe(gulp.dest(config.dist.services))
})

// Copy web applications
gulp.task('copy:web-apps', () => {
  return gulp.src([
    'src/Web/**/*',
    '!src/Web/**/node_modules/**',
    '!src/Web/**/dist/**',
    '!src/Web/**/build/**'
  ])
    .pipe(gulp.dest(config.dist.web))
})

// Copy configuration files
gulp.task('copy:config', () => {
  return gulp.src(config.src.config)
    .pipe(gulp.dest(config.dist.root))
})

// Copy documentation
gulp.task('copy:docs', () => {
  return gulp.src(config.src.docs)
    .pipe(gulp.dest(config.dist.docs))
})

// Copy scripts
gulp.task('copy:scripts', () => {
  return gulp.src(config.src.scripts)
    .pipe(gulp.dest(config.dist.scripts))
})

// Process JavaScript files
gulp.task('js', () => {
  const stream = gulp.src(config.src.js)
  
  if (config.targets[buildTarget].sourcemaps) {
    stream.pipe(sourcemaps.init())
  }
  
  stream.pipe(concat('app.js'))
  
  if (config.targets[buildTarget].minify) {
    stream.pipe(uglify())
  }
  
  if (config.targets[buildTarget].sourcemaps) {
    stream.pipe(sourcemaps.write('.'))
  }
  
  return stream.pipe(gulp.dest(config.dist.web))
})

// Process SCSS files
gulp.task('scss', () => {
  const stream = gulp.src(config.src.scss)
  
  if (config.targets[buildTarget].sourcemaps) {
    stream.pipe(sourcemaps.init())
  }
  
  stream.pipe(sass().on('error', sass.logError))
  
  if (config.targets[buildTarget].minify) {
    stream.pipe(cleanCSS())
  }
  
  if (config.targets[buildTarget].sourcemaps) {
    stream.pipe(sourcemaps.write('.'))
  }
  
  return stream.pipe(gulp.dest(config.dist.web))
})

// Process CSS files
gulp.task('css', () => {
  const stream = gulp.src(config.src.css)
  
  if (config.targets[buildTarget].minify) {
    stream.pipe(cleanCSS())
  }
  
  return stream.pipe(gulp.dest(config.dist.web))
})

// Lint JavaScript files
gulp.task('lint:js', () => {
  return gulp.src(config.src.js)
    .pipe(eslint())
    .pipe(eslint.format())
    .pipe(eslint.failAfterError())
})

// Lint .NET files (basic check)
gulp.task('lint:dotnet', (done) => {
  // This would typically use a .NET linter like StyleCop or similar
  console.log('Linting .NET files...')
  done()
})

// Build .NET services
gulp.task('build:dotnet', (done) => {
  const { exec } = require('child_process')
  
  console.log('Building .NET services...')
  
  // Build each service
  const services = [
    'Payment.API',
    'RiskAssessment.API',
    'Notification.API',
    'Settlement.API',
    'RealTime.Node.API'
  ]
  
  let completed = 0
  const total = services.length
  
  services.forEach(service => {
    const servicePath = path.join('src/Services', service)
    
    if (fs.existsSync(servicePath)) {
      exec(`dotnet build "${servicePath}" --configuration Release`, (error, stdout, stderr) => {
        if (error) {
          console.error(`Error building ${service}:`, error)
          done(error)
          return
        }
        
        console.log(`Built ${service} successfully`)
        completed++
        
        if (completed === total) {
          done()
        }
      })
    } else {
      console.log(`Service ${service} not found, skipping...`)
      completed++
      
      if (completed === total) {
        done()
      }
    }
  })
})

// Build web applications
gulp.task('build:web', (done) => {
  const { exec } = require('child_process')
  
  console.log('Building web applications...')
  
  // Build each web application
  const webApps = [
    'AdminPortal',
    'LegacyPortal'
  ]
  
  let completed = 0
  const total = webApps.length
  
  webApps.forEach(app => {
    const appPath = path.join('src/Web', app)
    
    if (fs.existsSync(appPath)) {
      exec(`npm run build`, { cwd: appPath }, (error, stdout, stderr) => {
        if (error) {
          console.error(`Error building ${app}:`, error)
          done(error)
          return
        }
        
        console.log(`Built ${app} successfully`)
        completed++
        
        if (completed === total) {
          done()
        }
      })
    } else {
      console.log(`Web app ${app} not found, skipping...`)
      completed++
      
      if (completed === total) {
        done()
      }
    }
  })
})

// Watch task
gulp.task('watch', () => {
  if (config.targets[buildTarget].watch) {
    gulp.watch(config.src.js, gulp.series('js'))
    gulp.watch(config.src.scss, gulp.series('scss'))
    gulp.watch(config.src.css, gulp.series('css'))
    gulp.watch(config.src.html, gulp.series('copy:web-apps'))
    gulp.watch(config.src.dotnetServices, gulp.series('copy:dotnet-services'))
    gulp.watch(config.src.config, gulp.series('copy:config'))
  }
})

// Browser sync for development
gulp.task('serve', () => {
  if (buildTarget === 'development') {
    browserSync.init({
      server: {
        baseDir: config.dist.web
      },
      port: 3000,
      open: true,
      notify: false
    })

    gulp.watch(config.src.js, gulp.series('js', 'reload'))
    gulp.watch(config.src.scss, gulp.series('scss', 'reload'))
    gulp.watch(config.src.css, gulp.series('css', 'reload'))
    gulp.watch(config.src.html, gulp.series('copy:web-apps', 'reload'))
  }
})

// Reload task
gulp.task('reload', (done) => {
  browserSync.reload()
  done()
})

// Development build
gulp.task('build:dev', (done) => {
  setBuildTarget('development')
  runSequence(
    'clean',
    'copy:dotnet-services',
    'copy:web-apps',
    'copy:config',
    'copy:docs',
    'copy:scripts',
    'js',
    'scss',
    'css',
    'lint:js',
    'lint:dotnet',
    done
  )
})

// Production build
gulp.task('build:prod', (done) => {
  setBuildTarget('production')
  runSequence(
    'clean',
    'copy:dotnet-services',
    'copy:web-apps',
    'copy:config',
    'copy:docs',
    'copy:scripts',
    'js',
    'scss',
    'css',
    'build:dotnet',
    'build:web',
    done
  )
})

// Full build (development)
gulp.task('build', gulp.series('build:dev'))

// Full build (production)
gulp.task('build:production', gulp.series('build:prod'))

// Development task
gulp.task('dev', gulp.series('build:dev', 'serve'))

// Test task
gulp.task('test', (done) => {
  console.log('Running tests...')
  // This would typically run unit tests, integration tests, etc.
  done()
})

// Deploy task
gulp.task('deploy', (done) => {
  console.log('Deploying application...')
  // This would typically deploy to staging/production environments
  done()
})

// Docker build task
gulp.task('docker:build', (done) => {
  const { exec } = require('child_process')
  
  console.log('Building Docker images...')
  
  exec('docker-compose build', (error, stdout, stderr) => {
    if (error) {
      console.error('Error building Docker images:', error)
      done(error)
      return
    }
    
    console.log('Docker images built successfully')
    done()
  })
})

// Docker run task
gulp.task('docker:run', (done) => {
  const { exec } = require('child_process')
  
  console.log('Running Docker containers...')
  
  exec('docker-compose up -d', (error, stdout, stderr) => {
    if (error) {
      console.error('Error running Docker containers:', error)
      done(error)
      return
    }
    
    console.log('Docker containers started successfully')
    done()
  })
})

// Docker stop task
gulp.task('docker:stop', (done) => {
  const { exec } = require('child_process')
  
  console.log('Stopping Docker containers...')
  
  exec('docker-compose down', (error, stdout, stderr) => {
    if (error) {
      console.error('Error stopping Docker containers:', error)
      done(error)
      return
    }
    
    console.log('Docker containers stopped successfully')
    done()
  })
})

// Health check task
gulp.task('health:check', (done) => {
  const { exec } = require('child_process')
  
  console.log('Running health checks...')
  
  // Check if services are running
  const services = [
    'http://localhost:5000/health',
    'http://localhost:5001/health',
    'http://localhost:5002/health',
    'http://localhost:5003/health',
    'http://localhost:5004/health',
    'http://localhost:5005/health'
  ]
  
  let completed = 0
  const total = services.length
  
  services.forEach(service => {
    exec(`curl -f ${service}`, (error, stdout, stderr) => {
      if (error) {
        console.error(`Health check failed for ${service}:`, error)
      } else {
        console.log(`Health check passed for ${service}`)
      }
      
      completed++
      if (completed === total) {
        done()
      }
    })
  })
})

// Default task
gulp.task('default', gulp.series('build'))

// Export tasks for external use
module.exports = {
  clean: 'clean',
  build: 'build',
  'build:dev': 'build:dev',
  'build:prod': 'build:prod',
  'build:production': 'build:production',
  dev: 'dev',
  serve: 'serve',
  watch: 'watch',
  test: 'test',
  deploy: 'deploy',
  'docker:build': 'docker:build',
  'docker:run': 'docker:run',
  'docker:stop': 'docker:stop',
  'health:check': 'health:check'
}
