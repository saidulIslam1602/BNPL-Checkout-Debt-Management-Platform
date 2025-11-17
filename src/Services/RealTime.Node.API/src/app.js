const express = require('express')
const http = require('http')
const socketIo = require('socket.io')
const cors = require('cors')
const helmet = require('helmet')
const morgan = require('morgan')
const compression = require('compression')
const rateLimit = require('express-rate-limit')
const swaggerUi = require('swagger-ui-express')
const swaggerJsdoc = require('swagger-jsdoc')
require('express-async-errors')
require('dotenv').config()

// Import custom modules
const logger = require('./utils/logger')
const errorHandler = require('./middleware/errorHandler')
const authMiddleware = require('./middleware/auth')
const rateLimitMiddleware = require('./middleware/rateLimit')
const { connectRedis } = require('./config/redis')
const { connectMongoDB } = require('./config/mongodb')
const { initializeQueues } = require('./services/queueService')

// Import routes
const authRoutes = require('./routes/auth')
const notificationRoutes = require('./routes/notifications')
const websocketRoutes = require('./routes/websocket')
const healthRoutes = require('./routes/health')
const adminRoutes = require('./routes/admin')

// Import services
const NotificationService = require('./services/notificationService')
const WebSocketService = require('./services/websocketService')
const PaymentEventService = require('./services/paymentEventService')

class Application {
  constructor() {
    this.app = express()
    this.server = http.createServer(this.app)
    this.io = socketIo(this.server, {
      cors: {
        origin: process.env.ALLOWED_ORIGINS?.split(',') || ['http://localhost:4200', 'http://localhost:4201', 'http://localhost:4202'],
        methods: ['GET', 'POST'],
        credentials: true
      },
      transports: ['websocket', 'polling']
    })
    
    this.port = process.env.PORT || 5005
    this.nodeEnv = process.env.NODE_ENV || 'development'
    
    this.initializeServices()
    this.initializeMiddleware()
    this.initializeRoutes()
    this.initializeWebSocket()
    this.initializeSwagger()
    this.initializeErrorHandling()
  }

  async initializeServices() {
    try {
      // Connect to Redis
      await connectRedis()
      logger.info('Connected to Redis')

      // Connect to MongoDB
      await connectMongoDB()
      logger.info('Connected to MongoDB')

      // Initialize queues
      await initializeQueues()
      logger.info('Initialized job queues')

      // Initialize services
      this.notificationService = new NotificationService()
      this.websocketService = new WebSocketService(this.io)
      this.paymentEventService = new PaymentEventService()

      logger.info('All services initialized successfully')
    } catch (error) {
      logger.error('Failed to initialize services:', error)
      process.exit(1)
    }
  }

  initializeMiddleware() {
    // Security middleware
    this.app.use(helmet({
      contentSecurityPolicy: {
        directives: {
          defaultSrc: ["'self'"],
          styleSrc: ["'self'", "'unsafe-inline'"],
          scriptSrc: ["'self'"],
          imgSrc: ["'self'", "data:", "https:"],
          connectSrc: ["'self'", "ws:", "wss:"]
        }
      }
    }))

    // CORS configuration
    this.app.use(cors({
      origin: process.env.ALLOWED_ORIGINS?.split(',') || ['http://localhost:4200', 'http://localhost:4201', 'http://localhost:4202'],
      credentials: true,
      methods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS'],
      allowedHeaders: ['Content-Type', 'Authorization', 'X-Requested-With', 'X-Request-ID']
    }))

    // Compression
    this.app.use(compression())

    // Request logging
    if (this.nodeEnv === 'development') {
      this.app.use(morgan('dev'))
    } else {
      this.app.use(morgan('combined', {
        stream: {
          write: (message) => logger.info(message.trim())
        }
      }))
    }

    // Rate limiting
    const limiter = rateLimit({
      windowMs: 15 * 60 * 1000, // 15 minutes
      max: 1000, // limit each IP to 1000 requests per windowMs
      message: {
        error: 'Too many requests from this IP, please try again later.',
        retryAfter: '15 minutes'
      },
      standardHeaders: true,
      legacyHeaders: false
    })
    this.app.use(limiter)

    // Body parsing
    this.app.use(express.json({ limit: '10mb' }))
    this.app.use(express.urlencoded({ extended: true, limit: '10mb' }))

    // Request ID middleware
    this.app.use((req, res, next) => {
      req.id = require('uuid').v4()
      res.setHeader('X-Request-ID', req.id)
      next()
    })

    // Custom rate limiting for specific endpoints
    this.app.use('/api/notifications', rateLimitMiddleware.notificationLimiter)
    this.app.use('/api/websocket', rateLimitMiddleware.websocketLimiter)
  }

  initializeRoutes() {
    // Health check (no auth required)
    this.app.use('/health', healthRoutes)

    // API routes
    this.app.use('/api/auth', authRoutes)
    this.app.use('/api/notifications', authMiddleware, notificationRoutes)
    this.app.use('/api/websocket', authMiddleware, websocketRoutes)
    this.app.use('/api/admin', authMiddleware, adminRoutes)

    // Root endpoint
    this.app.get('/', (req, res) => {
      res.json({
        service: 'YourCompany BNPL Real-time API',
        version: '1.0.0',
        status: 'running',
        timestamp: new Date().toISOString(),
        environment: this.nodeEnv,
        requestId: req.id
      })
    })

    // 404 handler
    this.app.use('*', (req, res) => {
      res.status(404).json({
        error: 'Endpoint not found',
        path: req.originalUrl,
        method: req.method,
        timestamp: new Date().toISOString(),
        requestId: req.id
      })
    })
  }

  initializeWebSocket() {
    this.io.use((socket, next) => {
      // WebSocket authentication middleware
      const token = socket.handshake.auth.token || socket.handshake.headers.authorization?.replace('Bearer ', '')
      
      if (!token) {
        return next(new Error('Authentication token required'))
      }

      try {
        const jwt = require('jsonwebtoken')
        const decoded = jwt.verify(token, process.env.JWT_SECRET)
        socket.userId = decoded.userId
        socket.userRoles = decoded.roles || []
        next()
      } catch (error) {
        next(new Error('Invalid authentication token'))
      }
    })

    this.io.on('connection', (socket) => {
      logger.info(`WebSocket client connected: ${socket.id}, User: ${socket.userId}`)

      // Join user-specific room
      socket.join(`user:${socket.userId}`)

      // Join role-based rooms
      socket.userRoles.forEach(role => {
        socket.join(`role:${role}`)
      })

      // Handle subscription to payment events
      socket.on('subscribe:payments', (data) => {
        if (data.merchantId) {
          socket.join(`merchant:${data.merchantId}`)
        }
        if (data.customerId) {
          socket.join(`customer:${data.customerId}`)
        }
        logger.info(`Socket ${socket.id} subscribed to payment events`)
      })

      // Handle subscription to notifications
      socket.on('subscribe:notifications', () => {
        socket.join('notifications')
        logger.info(`Socket ${socket.id} subscribed to notifications`)
      })

      // Handle unsubscription
      socket.on('unsubscribe', (data) => {
        if (data.merchantId) {
          socket.leave(`merchant:${data.merchantId}`)
        }
        if (data.customerId) {
          socket.leave(`customer:${data.customerId}`)
        }
        if (data.type === 'notifications') {
          socket.leave('notifications')
        }
        logger.info(`Socket ${socket.id} unsubscribed from ${data.type}`)
      })

      // Handle custom events
      socket.on('ping', () => {
        socket.emit('pong', { timestamp: new Date().toISOString() })
      })

      // Handle disconnection
      socket.on('disconnect', (reason) => {
        logger.info(`WebSocket client disconnected: ${socket.id}, Reason: ${reason}`)
      })

      // Handle errors
      socket.on('error', (error) => {
        logger.error(`WebSocket error for ${socket.id}:`, error)
      })
    })

    // Make io instance available to services
    this.websocketService.setIo(this.io)
  }

  initializeSwagger() {
    const swaggerOptions = {
      definition: {
        openapi: '3.0.0',
        info: {
          title: 'YourCompany BNPL Real-time API',
          version: '1.0.0',
          description: 'Real-time notifications and WebSocket API for BNPL platform',
          contact: {
            name: 'YourCompany Development Team',
            email: 'dev@yourcompany.com'
          }
        },
        servers: [
          {
            url: `http://localhost:${this.port}`,
            description: 'Development server'
          }
        ],
        components: {
          securitySchemes: {
            bearerAuth: {
              type: 'http',
              scheme: 'bearer',
              bearerFormat: 'JWT'
            }
          }
        },
        security: [
          {
            bearerAuth: []
          }
        ]
      },
      apis: ['./src/routes/*.js', './src/models/*.js']
    }

    const swaggerSpec = swaggerJsdoc(swaggerOptions)
    this.app.use('/api-docs', swaggerUi.serve, swaggerUi.setup(swaggerSpec))
  }

  initializeErrorHandling() {
    // Global error handler
    this.app.use(errorHandler)

    // Unhandled promise rejection handler
    process.on('unhandledRejection', (reason, promise) => {
      logger.error('Unhandled Rejection at:', promise, 'reason:', reason)
      // Application specific logging, throwing an error, or other logic here
    })

    // Uncaught exception handler
    process.on('uncaughtException', (error) => {
      logger.error('Uncaught Exception:', error)
      process.exit(1)
    })

    // Graceful shutdown
    process.on('SIGTERM', () => {
      logger.info('SIGTERM received, shutting down gracefully')
      this.shutdown()
    })

    process.on('SIGINT', () => {
      logger.info('SIGINT received, shutting down gracefully')
      this.shutdown()
    })
  }

  async shutdown() {
    try {
      logger.info('Starting graceful shutdown...')

      // Close HTTP server
      this.server.close(() => {
        logger.info('HTTP server closed')
      })

      // Close WebSocket connections
      this.io.close(() => {
        logger.info('WebSocket server closed')
      })

      // Close database connections
      const { disconnectRedis } = require('./config/redis')
      const { disconnectMongoDB } = require('./config/mongodb')
      
      await disconnectRedis()
      await disconnectMongoDB()
      
      logger.info('Database connections closed')
      logger.info('Graceful shutdown completed')
      
      process.exit(0)
    } catch (error) {
      logger.error('Error during shutdown:', error)
      process.exit(1)
    }
  }

  start() {
    this.server.listen(this.port, () => {
      logger.info(` Real-time API server running on port ${this.port}`)
      logger.info(`📚 API documentation available at http://localhost:${this.port}/api-docs`)
      logger.info(`🌍 Environment: ${this.nodeEnv}`)
      logger.info(` WebSocket endpoint: ws://localhost:${this.port}`)
    })
  }
}

// Create and start application
const app = new Application()
app.start()

module.exports = app
