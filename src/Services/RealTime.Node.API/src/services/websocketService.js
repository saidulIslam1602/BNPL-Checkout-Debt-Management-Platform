const logger = require('../utils/logger')
const { RedisService } = require('../config/redis')

/**
 * WebSocket Service for real-time communication
 */
class WebSocketService {
  constructor(io) {
    this.io = io
    this.redis = new RedisService()
    this.connectedClients = new Map()
    this.roomSubscriptions = new Map()
    
    this.initializeRedisSubscriptions()
  }

  /**
   * Set Socket.IO instance
   */
  setIo(io) {
    this.io = io
    this.setupEventHandlers()
  }

  /**
   * Setup Socket.IO event handlers
   */
  setupEventHandlers() {
    this.io.on('connection', (socket) => {
      this.handleConnection(socket)
    })
  }

  /**
   * Handle new WebSocket connection
   */
  handleConnection(socket) {
    const clientInfo = {
      id: socket.id,
      userId: socket.userId,
      userRoles: socket.userRoles || [],
      connectedAt: new Date(),
      lastActivity: new Date(),
      subscriptions: new Set()
    }

    this.connectedClients.set(socket.id, clientInfo)
    
    logger.info(`WebSocket client connected: ${socket.id}, User: ${socket.userId}`)

    // Join user-specific room
    socket.join(`user:${socket.userId}`)

    // Join role-based rooms
    socket.userRoles.forEach(role => {
      socket.join(`role:${role}`)
    })

    // Handle subscription to payment events
    socket.on('subscribe:payments', (data) => {
      this.handlePaymentSubscription(socket, data)
    })

    // Handle subscription to notifications
    socket.on('subscribe:notifications', () => {
      this.handleNotificationSubscription(socket)
    })

    // Handle subscription to merchant events
    socket.on('subscribe:merchant', (data) => {
      this.handleMerchantSubscription(socket, data)
    })

    // Handle subscription to customer events
    socket.on('subscribe:customer', (data) => {
      this.handleCustomerSubscription(socket, data)
    })

    // Handle unsubscription
    socket.on('unsubscribe', (data) => {
      this.handleUnsubscription(socket, data)
    })

    // Handle custom events
    socket.on('ping', () => {
      this.handlePing(socket)
    })

    // Handle typing indicators
    socket.on('typing:start', (data) => {
      this.handleTypingStart(socket, data)
    })

    socket.on('typing:stop', (data) => {
      this.handleTypingStop(socket, data)
    })

    // Handle disconnection
    socket.on('disconnect', (reason) => {
      this.handleDisconnection(socket, reason)
    })

    // Handle errors
    socket.on('error', (error) => {
      this.handleError(socket, error)
    })

    // Update last activity on any event
    socket.onAny(() => {
      this.updateLastActivity(socket.id)
    })
  }

  /**
   * Handle payment subscription
   */
  handlePaymentSubscription(socket, data) {
    const { merchantId, customerId, paymentId } = data

    if (merchantId) {
      socket.join(`merchant:${merchantId}`)
      this.addSubscription(socket.id, `merchant:${merchantId}`)
    }

    if (customerId) {
      socket.join(`customer:${customerId}`)
      this.addSubscription(socket.id, `customer:${customerId}`)
    }

    if (paymentId) {
      socket.join(`payment:${paymentId}`)
      this.addSubscription(socket.id, `payment:${paymentId}`)
    }

    logger.info(`Socket ${socket.id} subscribed to payment events`, data)
  }

  /**
   * Handle notification subscription
   */
  handleNotificationSubscription(socket) {
    socket.join('notifications')
    this.addSubscription(socket.id, 'notifications')
    logger.info(`Socket ${socket.id} subscribed to notifications`)
  }

  /**
   * Handle merchant subscription
   */
  handleMerchantSubscription(socket, data) {
    const { merchantId } = data

    if (merchantId) {
      socket.join(`merchant:${merchantId}`)
      this.addSubscription(socket.id, `merchant:${merchantId}`)
      logger.info(`Socket ${socket.id} subscribed to merchant ${merchantId}`)
    }
  }

  /**
   * Handle customer subscription
   */
  handleCustomerSubscription(socket, data) {
    const { customerId } = data

    if (customerId) {
      socket.join(`customer:${customerId}`)
      this.addSubscription(socket.id, `customer:${customerId}`)
      logger.info(`Socket ${socket.id} subscribed to customer ${customerId}`)
    }
  }

  /**
   * Handle unsubscription
   */
  handleUnsubscription(socket, data) {
    const { type, merchantId, customerId, paymentId } = data

    switch (type) {
      case 'payments':
        if (merchantId) {
          socket.leave(`merchant:${merchantId}`)
          this.removeSubscription(socket.id, `merchant:${merchantId}`)
        }
        if (customerId) {
          socket.leave(`customer:${customerId}`)
          this.removeSubscription(socket.id, `customer:${customerId}`)
        }
        if (paymentId) {
          socket.leave(`payment:${paymentId}`)
          this.removeSubscription(socket.id, `payment:${paymentId}`)
        }
        break

      case 'notifications':
        socket.leave('notifications')
        this.removeSubscription(socket.id, 'notifications')
        break

      case 'merchant':
        if (merchantId) {
          socket.leave(`merchant:${merchantId}`)
          this.removeSubscription(socket.id, `merchant:${merchantId}`)
        }
        break

      case 'customer':
        if (customerId) {
          socket.leave(`customer:${customerId}`)
          this.removeSubscription(socket.id, `customer:${customerId}`)
        }
        break
    }

    logger.info(`Socket ${socket.id} unsubscribed from ${type}`, data)
  }

  /**
   * Handle ping
   */
  handlePing(socket) {
    socket.emit('pong', { 
      timestamp: new Date().toISOString(),
      serverTime: Date.now()
    })
  }

  /**
   * Handle typing start
   */
  handleTypingStart(socket, data) {
    const { room, user } = data
    socket.to(room).emit('typing:start', { user, timestamp: new Date().toISOString() })
  }

  /**
   * Handle typing stop
   */
  handleTypingStop(socket, data) {
    const { room, user } = data
    socket.to(room).emit('typing:stop', { user, timestamp: new Date().toISOString() })
  }

  /**
   * Handle disconnection
   */
  handleDisconnection(socket, reason) {
    this.connectedClients.delete(socket.id)
    logger.info(`WebSocket client disconnected: ${socket.id}, Reason: ${reason}`)
  }

  /**
   * Handle error
   */
  handleError(socket, error) {
    logger.error(`WebSocket error for ${socket.id}:`, error)
  }

  /**
   * Add subscription to client
   */
  addSubscription(socketId, subscription) {
    const client = this.connectedClients.get(socketId)
    if (client) {
      client.subscriptions.add(subscription)
    }
  }

  /**
   * Remove subscription from client
   */
  removeSubscription(socketId, subscription) {
    const client = this.connectedClients.get(socketId)
    if (client) {
      client.subscriptions.delete(subscription)
    }
  }

  /**
   * Update last activity
   */
  updateLastActivity(socketId) {
    const client = this.connectedClients.get(socketId)
    if (client) {
      client.lastActivity = new Date()
    }
  }

  /**
   * Initialize Redis subscriptions for cross-service communication
   */
  initializeRedisSubscriptions() {
    // Subscribe to payment events
    this.redis.subscribe('payment:events', (message) => {
      this.handlePaymentEvent(message)
    })

    // Subscribe to notification events
    this.redis.subscribe('notification:events', (message) => {
      this.handleNotificationEvent(message)
    })

    // Subscribe to system events
    this.redis.subscribe('system:events', (message) => {
      this.handleSystemEvent(message)
    })

    logger.info('Redis subscriptions initialized')
  }

  /**
   * Handle payment events from Redis
   */
  handlePaymentEvent(message) {
    const { type, data, target } = message

    switch (type) {
      case 'payment:created':
        this.broadcastToRoom(`merchant:${data.merchantId}`, 'payment:created', data)
        this.broadcastToRoom(`customer:${data.customerId}`, 'payment:created', data)
        break

      case 'payment:updated':
        this.broadcastToRoom(`payment:${data.paymentId}`, 'payment:updated', data)
        this.broadcastToRoom(`merchant:${data.merchantId}`, 'payment:updated', data)
        this.broadcastToRoom(`customer:${data.customerId}`, 'payment:updated', data)
        break

      case 'payment:completed':
        this.broadcastToRoom(`payment:${data.paymentId}`, 'payment:completed', data)
        this.broadcastToRoom(`merchant:${data.merchantId}`, 'payment:completed', data)
        this.broadcastToRoom(`customer:${data.customerId}`, 'payment:completed', data)
        break

      case 'payment:failed':
        this.broadcastToRoom(`payment:${data.paymentId}`, 'payment:failed', data)
        this.broadcastToRoom(`merchant:${data.merchantId}`, 'payment:failed', data)
        this.broadcastToRoom(`customer:${data.customerId}`, 'payment:failed', data)
        break

      case 'installment:due':
        this.broadcastToRoom(`customer:${data.customerId}`, 'installment:due', data)
        break

      case 'installment:overdue':
        this.broadcastToRoom(`customer:${data.customerId}`, 'installment:overdue', data)
        break
    }

    logger.info(`Payment event broadcasted: ${type}`, data)
  }

  /**
   * Handle notification events from Redis
   */
  handleNotificationEvent(message) {
    const { type, data, target } = message

    switch (type) {
      case 'notification:created':
        if (target.userId) {
          this.broadcastToRoom(`user:${target.userId}`, 'notification:created', data)
        }
        if (target.role) {
          this.broadcastToRoom(`role:${target.role}`, 'notification:created', data)
        }
        break

      case 'notification:read':
        if (target.userId) {
          this.broadcastToRoom(`user:${target.userId}`, 'notification:read', data)
        }
        break
    }

    logger.info(`Notification event broadcasted: ${type}`, data)
  }

  /**
   * Handle system events from Redis
   */
  handleSystemEvent(message) {
    const { type, data } = message

    switch (type) {
      case 'system:maintenance':
        this.broadcastToAll('system:maintenance', data)
        break

      case 'system:alert':
        this.broadcastToAll('system:alert', data)
        break

      case 'system:status':
        this.broadcastToAll('system:status', data)
        break
    }

    logger.info(`System event broadcasted: ${type}`, data)
  }

  /**
   * Broadcast message to specific room
   */
  broadcastToRoom(room, event, data) {
    this.io.to(room).emit(event, {
      ...data,
      timestamp: new Date().toISOString(),
      event
    })
  }

  /**
   * Broadcast message to all connected clients
   */
  broadcastToAll(event, data) {
    this.io.emit(event, {
      ...data,
      timestamp: new Date().toISOString(),
      event
    })
  }

  /**
   * Send message to specific user
   */
  sendToUser(userId, event, data) {
    this.broadcastToRoom(`user:${userId}`, event, data)
  }

  /**
   * Send message to users with specific role
   */
  sendToRole(role, event, data) {
    this.broadcastToRoom(`role:${role}`, event, data)
  }

  /**
   * Send message to specific socket
   */
  sendToSocket(socketId, event, data) {
    this.io.to(socketId).emit(event, {
      ...data,
      timestamp: new Date().toISOString(),
      event
    })
  }

  /**
   * Get connected clients count
   */
  getConnectedClientsCount() {
    return this.connectedClients.size
  }

  /**
   * Get connected clients info
   */
  getConnectedClients() {
    return Array.from(this.connectedClients.values())
  }

  /**
   * Get clients in room
   */
  getClientsInRoom(room) {
    const roomSockets = this.io.sockets.adapter.rooms.get(room)
    return roomSockets ? Array.from(roomSockets) : []
  }

  /**
   * Check if user is online
   */
  isUserOnline(userId) {
    const roomSockets = this.io.sockets.adapter.rooms.get(`user:${userId}`)
    return roomSockets && roomSockets.size > 0
  }

  /**
   * Get online users count
   */
  getOnlineUsersCount() {
    const userRooms = Array.from(this.io.sockets.adapter.rooms.keys())
      .filter(room => room.startsWith('user:'))
    return userRooms.length
  }

  /**
   * Disconnect user
   */
  disconnectUser(userId) {
    const roomSockets = this.io.sockets.adapter.rooms.get(`user:${userId}`)
    if (roomSockets) {
      roomSockets.forEach(socketId => {
        const socket = this.io.sockets.sockets.get(socketId)
        if (socket) {
          socket.disconnect(true)
        }
      })
    }
  }

  /**
   * Get server statistics
   */
  getServerStats() {
    return {
      connectedClients: this.getConnectedClientsCount(),
      onlineUsers: this.getOnlineUsersCount(),
      rooms: this.io.sockets.adapter.rooms.size,
      uptime: process.uptime(),
      memoryUsage: process.memoryUsage(),
      timestamp: new Date().toISOString()
    }
  }
}

module.exports = WebSocketService
