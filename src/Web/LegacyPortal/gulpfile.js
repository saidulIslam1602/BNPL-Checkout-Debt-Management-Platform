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
const jasmine = require('gulp-jasmine')
const karma = require('gulp-karma')
const del = require('del')

// Configuration
const config = {
  src: {
    js: 'src/js/**/*.js',
    scss: 'src/scss/**/*.scss',
    html: 'src/**/*.html',
    assets: 'src/assets/**/*'
  },
  dist: {
    js: 'dist/js',
    css: 'dist/css',
    assets: 'dist/assets',
    root: 'dist'
  },
  vendor: {
    js: [
      'node_modules/jquery/dist/jquery.min.js',
      'node_modules/knockout/build/output/knockout-latest.js',
      'node_modules/bootstrap/dist/js/bootstrap.bundle.min.js',
      'node_modules/moment/min/moment.min.js',
      'node_modules/lodash/lodash.min.js',
      'node_modules/axios/dist/axios.min.js',
      'node_modules/chart.js/dist/chart.min.js',
      'node_modules/datatables.net/js/jquery.dataTables.min.js',
      'node_modules/datatables.net-bs5/js/dataTables.bootstrap5.min.js',
      'node_modules/select2/dist/js/select2.min.js',
      'node_modules/sweetalert2/dist/sweetalert2.min.js',
      'node_modules/toastr/build/toastr.min.js',
      'node_modules/js-cookie/dist/js.cookie.min.js',
      'node_modules/crypto-js/crypto-js.min.js',
      'node_modules/numeral/min/numeral.min.js',
      'node_modules/date-fns/index.min.js'
    ],
    css: [
      'node_modules/bootstrap/dist/css/bootstrap.min.css',
      'node_modules/datatables.net-bs5/css/dataTables.bootstrap5.min.css',
      'node_modules/select2/dist/css/select2.min.css',
      'node_modules/select2-bootstrap-5-theme/dist/select2-bootstrap-5-theme.min.css',
      'node_modules/sweetalert2/dist/sweetalert2.min.css',
      'node_modules/toastr/build/toastr.min.css'
    ]
  }
}

// Clean task
gulp.task('clean', () => {
  return del([config.dist.root])
})

// Vendor JavaScript task
gulp.task('vendor:js', () => {
  return gulp.src(config.vendor.js)
    .pipe(concat('vendor.js'))
    .pipe(gulp.dest(config.dist.js))
    .pipe(rename('vendor.min.js'))
    .pipe(uglify())
    .pipe(gulp.dest(config.dist.js))
})

// Vendor CSS task
gulp.task('vendor:css', () => {
  return gulp.src(config.vendor.css)
    .pipe(concat('vendor.css'))
    .pipe(gulp.dest(config.dist.css))
    .pipe(rename('vendor.min.css'))
    .pipe(cleanCSS())
    .pipe(gulp.dest(config.dist.css))
})

// Application JavaScript task
gulp.task('js', () => {
  return gulp.src(config.src.js)
    .pipe(sourcemaps.init())
    .pipe(concat('app.js'))
    .pipe(gulp.dest(config.dist.js))
    .pipe(rename('app.min.js'))
    .pipe(uglify())
    .pipe(sourcemaps.write('.'))
    .pipe(gulp.dest(config.dist.js))
})

// SCSS task
gulp.task('scss', () => {
  return gulp.src(config.src.scss)
    .pipe(sourcemaps.init())
    .pipe(sass().on('error', sass.logError))
    .pipe(concat('app.css'))
    .pipe(gulp.dest(config.dist.css))
    .pipe(rename('app.min.css'))
    .pipe(cleanCSS())
    .pipe(sourcemaps.write('.'))
    .pipe(gulp.dest(config.dist.css))
})

// HTML task
gulp.task('html', () => {
  return gulp.src(config.src.html)
    .pipe(gulp.dest(config.dist.root))
})

// Assets task
gulp.task('assets', () => {
  return gulp.src(config.src.assets)
    .pipe(gulp.dest(config.dist.assets))
})

// Lint task
gulp.task('lint', () => {
  return gulp.src(config.src.js)
    .pipe(eslint())
    .pipe(eslint.format())
    .pipe(eslint.failAfterError())
})

// Test task
gulp.task('test', () => {
  return gulp.src('tests/**/*.js')
    .pipe(jasmine())
})

// Karma test task
gulp.task('test:karma', () => {
  return gulp.src('tests/**/*.js')
    .pipe(karma({
      configFile: 'karma.conf.js',
      action: 'run'
    }))
})

// Watch task
gulp.task('watch', () => {
  gulp.watch(config.src.js, gulp.series('js'))
  gulp.watch(config.src.scss, gulp.series('scss'))
  gulp.watch(config.src.html, gulp.series('html'))
  gulp.watch(config.src.assets, gulp.series('assets'))
})

// Browser sync task
gulp.task('serve', () => {
  browserSync.init({
    server: {
      baseDir: config.dist.root
    },
    port: 4203,
    open: true,
    notify: false
  })

  gulp.watch(config.src.js, gulp.series('js', 'reload'))
  gulp.watch(config.src.scss, gulp.series('scss', 'reload'))
  gulp.watch(config.src.html, gulp.series('html', 'reload'))
  gulp.watch(config.src.assets, gulp.series('assets', 'reload'))
})

// Reload task
gulp.task('reload', (done) => {
  browserSync.reload()
  done()
})

// Build task
gulp.task('build', gulp.series(
  'clean',
  'vendor:js',
  'vendor:css',
  'js',
  'scss',
  'html',
  'assets'
))

// Production build task
gulp.task('build:prod', gulp.series(
  'clean',
  'vendor:js',
  'vendor:css',
  'js',
  'scss',
  'html',
  'assets'
))

// Development task
gulp.task('dev', gulp.series('build', 'serve'))

// Default task
gulp.task('default', gulp.series('build'))

// Export tasks for external use
module.exports = {
  clean: 'clean',
  build: 'build',
  serve: 'serve',
  watch: 'watch',
  lint: 'lint',
  test: 'test'
}
