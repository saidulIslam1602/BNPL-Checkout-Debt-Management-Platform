const Redis = require('ioredis')
const logger = require('../utils/logger')

let redisClient = null
let redisSubscriber = null
let redisPublisher = null

const redisConfig = {
  host: process.env.REDIS_HOST || 'localhost',
  port: process.env.REDIS_PORT || 6379,
  password: process.env.REDIS_PASSWORD || null,
  db: process.env.REDIS_DB || 0,
  retryDelayOnFailover: 100,
  maxRetriesPerRequest: 3,
  lazyConnect: true,
  keepAlive: 30000,
  connectTimeout: 10000,
  commandTimeout: 5000,
  retryDelayOnClusterDown: 300,
  enableOfflineQueue: false,
  maxLoadingTimeout: 10000,
  enableReadyCheck: true,
  maxMemoryPolicy: 'allkeys-lru'
}

/**
 * Connect to Redis
 */
async function connectRedis() {
  try {
    // Main Redis client
    redisClient = new Redis(redisConfig)
    
    // Redis subscriber for pub/sub
    redisSubscriber = new Redis(redisConfig)
    
    // Redis publisher for pub/sub
    redisPublisher = new Redis(redisConfig)

    // Event handlers for main client
    redisClient.on('connect', () => {
      logger.info('Redis client connected')
    })

    redisClient.on('ready', () => {
      logger.info('Redis client ready')
    })

    redisClient.on('error', (error) => {
      logger.error('Redis client error:', error)
    })

    redisClient.on('close', () => {
      logger.warn('Redis client connection closed')
    })

    redisClient.on('reconnecting', () => {
      logger.info('Redis client reconnecting...')
    })

    // Event handlers for subscriber
    redisSubscriber.on('connect', () => {
      logger.info('Redis subscriber connected')
    })

    redisSubscriber.on('ready', () => {
      logger.info('Redis subscriber ready')
    })

    redisSubscriber.on('error', (error) => {
      logger.error('Redis subscriber error:', error)
    })

    // Event handlers for publisher
    redisPublisher.on('connect', () => {
      logger.info('Redis publisher connected')
    })

    redisPublisher.on('ready', () => {
      logger.info('Redis publisher ready')
    })

    redisPublisher.on('error', (error) => {
      logger.error('Redis publisher error:', error)
    })

    // Test connection
    await redisClient.ping()
    logger.info('Redis connection test successful')

    return redisClient
  } catch (error) {
    logger.error('Failed to connect to Redis:', error)
    throw error
  }
}

/**
 * Disconnect from Redis
 */
async function disconnectRedis() {
  try {
    if (redisClient) {
      await redisClient.quit()
      redisClient = null
    }
    
    if (redisSubscriber) {
      await redisSubscriber.quit()
      redisSubscriber = null
    }
    
    if (redisPublisher) {
      await redisPublisher.quit()
      redisPublisher = null
    }
    
    logger.info('Redis connections closed')
  } catch (error) {
    logger.error('Error closing Redis connections:', error)
    throw error
  }
}

/**
 * Get Redis client
 */
function getRedisClient() {
  if (!redisClient) {
    throw new Error('Redis client not initialized')
  }
  return redisClient
}

/**
 * Get Redis subscriber
 */
function getRedisSubscriber() {
  if (!redisSubscriber) {
    throw new Error('Redis subscriber not initialized')
  }
  return redisSubscriber
}

/**
 * Get Redis publisher
 */
function getRedisPublisher() {
  if (!redisPublisher) {
    throw new Error('Redis publisher not initialized')
  }
  return redisPublisher
}

/**
 * Redis utility functions
 */
class RedisService {
  constructor() {
    this.client = getRedisClient()
    this.publisher = getRedisPublisher()
    this.subscriber = getRedisSubscriber()
  }

  /**
   * Set key-value pair with expiration
   */
  async set(key, value, ttl = null) {
    try {
      const serializedValue = JSON.stringify(value)
      if (ttl) {
        return await this.client.setex(key, ttl, serializedValue)
      } else {
        return await this.client.set(key, serializedValue)
      }
    } catch (error) {
      logger.error(`Redis SET error for key ${key}:`, error)
      throw error
    }
  }

  /**
   * Get value by key
   */
  async get(key) {
    try {
      const value = await this.client.get(key)
      return value ? JSON.parse(value) : null
    } catch (error) {
      logger.error(`Redis GET error for key ${key}:`, error)
      throw error
    }
  }

  /**
   * Delete key
   */
  async del(key) {
    try {
      return await this.client.del(key)
    } catch (error) {
      logger.error(`Redis DEL error for key ${key}:`, error)
      throw error
    }
  }

  /**
   * Check if key exists
   */
  async exists(key) {
    try {
      return await this.client.exists(key)
    } catch (error) {
      logger.error(`Redis EXISTS error for key ${key}:`, error)
      throw error
    }
  }

  /**
   * Set expiration for key
   */
  async expire(key, ttl) {
    try {
      return await this.client.expire(key, ttl)
    } catch (error) {
      logger.error(`Redis EXPIRE error for key ${key}:`, error)
      throw error
    }
  }

  /**
   * Get TTL for key
   */
  async ttl(key) {
    try {
      return await this.client.ttl(key)
    } catch (error) {
      logger.error(`Redis TTL error for key ${key}:`, error)
      throw error
    }
  }

  /**
   * Increment counter
   */
  async incr(key) {
    try {
      return await this.client.incr(key)
    } catch (error) {
      logger.error(`Redis INCR error for key ${key}:`, error)
      throw error
    }
  }

  /**
   * Decrement counter
   */
  async decr(key) {
    try {
      return await this.client.decr(key)
    } catch (error) {
      logger.error(`Redis DECR error for key ${key}:`, error)
      throw error
    }
  }

  /**
   * Add to set
   */
  async sadd(key, ...members) {
    try {
      return await this.client.sadd(key, ...members)
    } catch (error) {
      logger.error(`Redis SADD error for key ${key}:`, error)
      throw error
    }
  }

  /**
   * Get set members
   */
  async smembers(key) {
    try {
      return await this.client.smembers(key)
    } catch (error) {
      logger.error(`Redis SMEMBERS error for key ${key}:`, error)
      throw error
    }
  }

  /**
   * Remove from set
   */
  async srem(key, ...members) {
    try {
      return await this.client.srem(key, ...members)
    } catch (error) {
      logger.error(`Redis SREM error for key ${key}:`, error)
      throw error
    }
  }

  /**
   * Add to list
   */
  async lpush(key, ...values) {
    try {
      return await this.client.lpush(key, ...values)
    } catch (error) {
      logger.error(`Redis LPUSH error for key ${key}:`, error)
      throw error
    }
  }

  /**
   * Get list elements
   */
  async lrange(key, start, stop) {
    try {
      return await this.client.lrange(key, start, stop)
    } catch (error) {
      logger.error(`Redis LRANGE error for key ${key}:`, error)
      throw error
    }
  }

  /**
   * Publish message to channel
   */
  async publish(channel, message) {
    try {
      const serializedMessage = JSON.stringify(message)
      return await this.publisher.publish(channel, serializedMessage)
    } catch (error) {
      logger.error(`Redis PUBLISH error for channel ${channel}:`, error)
      throw error
    }
  }

  /**
   * Subscribe to channel
   */
  async subscribe(channel, callback) {
    try {
      this.subscriber.subscribe(channel)
      this.subscriber.on('message', (receivedChannel, message) => {
        if (receivedChannel === channel) {
          try {
            const parsedMessage = JSON.parse(message)
            callback(parsedMessage)
          } catch (error) {
            logger.error(`Error parsing message from channel ${channel}:`, error)
          }
        }
      })
    } catch (error) {
      logger.error(`Redis SUBSCRIBE error for channel ${channel}:`, error)
      throw error
    }
  }

  /**
   * Unsubscribe from channel
   */
  async unsubscribe(channel) {
    try {
      return await this.subscriber.unsubscribe(channel)
    } catch (error) {
      logger.error(`Redis UNSUBSCRIBE error for channel ${channel}:`, error)
      throw error
    }
  }

  /**
   * Get multiple keys
   */
  async mget(...keys) {
    try {
      const values = await this.client.mget(...keys)
      return values.map(value => value ? JSON.parse(value) : null)
    } catch (error) {
      logger.error(`Redis MGET error for keys ${keys.join(', ')}:`, error)
      throw error
    }
  }

  /**
   * Set multiple key-value pairs
   */
  async mset(keyValuePairs) {
    try {
      const serializedPairs = {}
      for (const [key, value] of Object.entries(keyValuePairs)) {
        serializedPairs[key] = JSON.stringify(value)
      }
      return await this.client.mset(serializedPairs)
    } catch (error) {
      logger.error(`Redis MSET error:`, error)
      throw error
    }
  }

  /**
   * Get keys by pattern
   */
  async keys(pattern) {
    try {
      return await this.client.keys(pattern)
    } catch (error) {
      logger.error(`Redis KEYS error for pattern ${pattern}:`, error)
      throw error
    }
  }

  /**
   * Flush database
   */
  async flushdb() {
    try {
      return await this.client.flushdb()
    } catch (error) {
      logger.error('Redis FLUSHDB error:', error)
      throw error
    }
  }

  /**
   * Get Redis info
   */
  async info() {
    try {
      return await this.client.info()
    } catch (error) {
      logger.error('Redis INFO error:', error)
      throw error
    }
  }

  /**
   * Ping Redis
   */
  async ping() {
    try {
      return await this.client.ping()
    } catch (error) {
      logger.error('Redis PING error:', error)
      throw error
    }
  }
}

module.exports = {
  connectRedis,
  disconnectRedis,
  getRedisClient,
  getRedisSubscriber,
  getRedisPublisher,
  RedisService
}
