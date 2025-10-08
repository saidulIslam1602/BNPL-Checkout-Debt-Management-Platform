const express = require('express')
const { body, validationResult } = require('express-validator')
const router = express.Router()
const logger = require('../utils/logger')
const NotificationService = require('../services/notificationService')
const { RedisService } = require('../config/redis')

const notificationService = new NotificationService()
const redis = new RedisService()

/**
 * @swagger
 * components:
 *   schemas:
 *     Notification:
 *       type: object
 *       required:
 *         - title
 *         - message
 *         - type
 *         - recipientId
 *       properties:
 *         id:
 *           type: string
 *           description: Unique notification ID
 *         title:
 *           type: string
 *           description: Notification title
 *         message:
 *           type: string
 *           description: Notification message
 *         type:
 *           type: string
 *           enum: [info, warning, error, success]
 *           description: Notification type
 *         recipientId:
 *           type: string
 *           description: Recipient user ID
 *         recipientType:
 *           type: string
 *           enum: [user, role, all]
 *           description: Recipient type
 *         data:
 *           type: object
 *           description: Additional notification data
 *         read:
 *           type: boolean
 *           description: Whether notification is read
 *         createdAt:
 *           type: string
 *           format: date-time
 *           description: Creation timestamp
 *         readAt:
 *           type: string
 *           format: date-time
 *           description: Read timestamp
 */

/**
 * @swagger
 * /api/notifications:
 *   get:
 *     summary: Get notifications for current user
 *     tags: [Notifications]
 *     security:
 *       - bearerAuth: []
 *     parameters:
 *       - in: query
 *         name: page
 *         schema:
 *           type: integer
 *           minimum: 1
 *           default: 1
 *         description: Page number
 *       - in: query
 *         name: limit
 *         schema:
 *           type: integer
 *           minimum: 1
 *           maximum: 100
 *           default: 20
 *         description: Number of notifications per page
 *       - in: query
 *         name: unread
 *         schema:
 *           type: boolean
 *         description: Filter unread notifications only
 *       - in: query
 *         name: type
 *         schema:
 *           type: string
 *           enum: [info, warning, error, success]
 *         description: Filter by notification type
 *     responses:
 *       200:
 *         description: Notifications retrieved successfully
 *         content:
 *           application/json:
 *             schema:
 *               type: object
 *               properties:
 *                 success:
 *                   type: boolean
 *                 data:
 *                   type: object
 *                   properties:
 *                     notifications:
 *                       type: array
 *                       items:
 *                         $ref: '#/components/schemas/Notification'
 *                     pagination:
 *                       type: object
 *                       properties:
 *                         page:
 *                           type: integer
 *                         limit:
 *                           type: integer
 *                         total:
 *                           type: integer
 *                         pages:
 *                           type: integer
 *       401:
 *         description: Unauthorized
 *       500:
 *         description: Internal server error
 */
router.get('/', async (req, res) => {
  try {
    const { page = 1, limit = 20, unread, type } = req.query
    const userId = req.user.id

    const filters = { recipientId: userId }
    if (unread !== undefined) {
      filters.read = unread === 'false'
    }
    if (type) {
      filters.type = type
    }

    const result = await notificationService.getNotifications(filters, {
      page: parseInt(page),
      limit: parseInt(limit)
    })

    res.json({
      success: true,
      data: result
    })
  } catch (error) {
    logger.error('Error getting notifications:', error)
    res.status(500).json({
      success: false,
      error: 'Failed to retrieve notifications'
    })
  }
})

/**
 * @swagger
 * /api/notifications/{id}:
 *   get:
 *     summary: Get specific notification
 *     tags: [Notifications]
 *     security:
 *       - bearerAuth: []
 *     parameters:
 *       - in: path
 *         name: id
 *         required: true
 *         schema:
 *           type: string
 *         description: Notification ID
 *     responses:
 *       200:
 *         description: Notification retrieved successfully
 *         content:
 *           application/json:
 *             schema:
 *               type: object
 *               properties:
 *                 success:
 *                   type: boolean
 *                 data:
 *                   $ref: '#/components/schemas/Notification'
 *       404:
 *         description: Notification not found
 *       500:
 *         description: Internal server error
 */
router.get('/:id', async (req, res) => {
  try {
    const { id } = req.params
    const userId = req.user.id

    const notification = await notificationService.getNotification(id, userId)

    if (!notification) {
      return res.status(404).json({
        success: false,
        error: 'Notification not found'
      })
    }

    res.json({
      success: true,
      data: notification
    })
  } catch (error) {
    logger.error('Error getting notification:', error)
    res.status(500).json({
      success: false,
      error: 'Failed to retrieve notification'
    })
  }
})

/**
 * @swagger
 * /api/notifications:
 *   post:
 *     summary: Create new notification
 *     tags: [Notifications]
 *     security:
 *       - bearerAuth: []
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             required:
 *               - title
 *               - message
 *               - type
 *               - recipientId
 *             properties:
 *               title:
 *                 type: string
 *                 description: Notification title
 *               message:
 *                 type: string
 *                 description: Notification message
 *               type:
 *                 type: string
 *                 enum: [info, warning, error, success]
 *                 description: Notification type
 *               recipientId:
 *                 type: string
 *                 description: Recipient user ID
 *               recipientType:
 *                 type: string
 *                 enum: [user, role, all]
 *                 default: user
 *                 description: Recipient type
 *               data:
 *                 type: object
 *                 description: Additional notification data
 *               channels:
 *                 type: array
 *                 items:
 *                   type: string
 *                   enum: [websocket, email, sms, push]
 *                 description: Notification channels
 *     responses:
 *       201:
 *         description: Notification created successfully
 *         content:
 *           application/json:
 *             schema:
 *               type: object
 *               properties:
 *                 success:
 *                   type: boolean
 *                 data:
 *                   $ref: '#/components/schemas/Notification'
 *       400:
 *         description: Validation error
 *       500:
 *         description: Internal server error
 */
router.post('/', [
  body('title').notEmpty().withMessage('Title is required'),
  body('message').notEmpty().withMessage('Message is required'),
  body('type').isIn(['info', 'warning', 'error', 'success']).withMessage('Invalid notification type'),
  body('recipientId').notEmpty().withMessage('Recipient ID is required'),
  body('recipientType').optional().isIn(['user', 'role', 'all']).withMessage('Invalid recipient type'),
  body('channels').optional().isArray().withMessage('Channels must be an array')
], async (req, res) => {
  try {
    const errors = validationResult(req)
    if (!errors.isEmpty()) {
      return res.status(400).json({
        success: false,
        error: 'Validation failed',
        details: errors.array()
      })
    }

    const notificationData = {
      ...req.body,
      senderId: req.user.id,
      createdAt: new Date()
    }

    const notification = await notificationService.createNotification(notificationData)

    // Publish to Redis for real-time delivery
    await redis.publish('notification:events', {
      type: 'notification:created',
      data: notification,
      target: {
        userId: notification.recipientId,
        role: notification.recipientType === 'role' ? notification.recipientId : null
      }
    })

    res.status(201).json({
      success: true,
      data: notification
    })
  } catch (error) {
    logger.error('Error creating notification:', error)
    res.status(500).json({
      success: false,
      error: 'Failed to create notification'
    })
  }
})

/**
 * @swagger
 * /api/notifications/{id}/read:
 *   patch:
 *     summary: Mark notification as read
 *     tags: [Notifications]
 *     security:
 *       - bearerAuth: []
 *     parameters:
 *       - in: path
 *         name: id
 *         required: true
 *         schema:
 *           type: string
 *         description: Notification ID
 *     responses:
 *       200:
 *         description: Notification marked as read
 *         content:
 *           application/json:
 *             schema:
 *               type: object
 *               properties:
 *                 success:
 *                   type: boolean
 *                 data:
 *                   $ref: '#/components/schemas/Notification'
 *       404:
 *         description: Notification not found
 *       500:
 *         description: Internal server error
 */
router.patch('/:id/read', async (req, res) => {
  try {
    const { id } = req.params
    const userId = req.user.id

    const notification = await notificationService.markAsRead(id, userId)

    if (!notification) {
      return res.status(404).json({
        success: false,
        error: 'Notification not found'
      })
    }

    // Publish to Redis for real-time delivery
    await redis.publish('notification:events', {
      type: 'notification:read',
      data: notification,
      target: { userId }
    })

    res.json({
      success: true,
      data: notification
    })
  } catch (error) {
    logger.error('Error marking notification as read:', error)
    res.status(500).json({
      success: false,
      error: 'Failed to mark notification as read'
    })
  }
})

/**
 * @swagger
 * /api/notifications/read-all:
 *   patch:
 *     summary: Mark all notifications as read
 *     tags: [Notifications]
 *     security:
 *       - bearerAuth: []
 *     responses:
 *       200:
 *         description: All notifications marked as read
 *         content:
 *           application/json:
 *             schema:
 *               type: object
 *               properties:
 *                 success:
 *                   type: boolean
 *                 data:
 *                   type: object
 *                   properties:
 *                     updatedCount:
 *                       type: integer
 *       500:
 *         description: Internal server error
 */
router.patch('/read-all', async (req, res) => {
  try {
    const userId = req.user.id

    const result = await notificationService.markAllAsRead(userId)

    res.json({
      success: true,
      data: {
        updatedCount: result.modifiedCount
      }
    })
  } catch (error) {
    logger.error('Error marking all notifications as read:', error)
    res.status(500).json({
      success: false,
      error: 'Failed to mark all notifications as read'
    })
  }
})

/**
 * @swagger
 * /api/notifications/{id}:
 *   delete:
 *     summary: Delete notification
 *     tags: [Notifications]
 *     security:
 *       - bearerAuth: []
 *     parameters:
 *       - in: path
 *         name: id
 *         required: true
 *         schema:
 *           type: string
 *         description: Notification ID
 *     responses:
 *       200:
 *         description: Notification deleted successfully
 *         content:
 *           application/json:
 *             schema:
 *               type: object
 *               properties:
 *                 success:
 *                   type: boolean
 *                 message:
 *                   type: string
 *       404:
 *         description: Notification not found
 *       500:
 *         description: Internal server error
 */
router.delete('/:id', async (req, res) => {
  try {
    const { id } = req.params
    const userId = req.user.id

    const result = await notificationService.deleteNotification(id, userId)

    if (!result) {
      return res.status(404).json({
        success: false,
        error: 'Notification not found'
      })
    }

    res.json({
      success: true,
      message: 'Notification deleted successfully'
    })
  } catch (error) {
    logger.error('Error deleting notification:', error)
    res.status(500).json({
      success: false,
      error: 'Failed to delete notification'
    })
  }
})

/**
 * @swagger
 * /api/notifications/stats:
 *   get:
 *     summary: Get notification statistics
 *     tags: [Notifications]
 *     security:
 *       - bearerAuth: []
 *     responses:
 *       200:
 *         description: Notification statistics retrieved successfully
 *         content:
 *           application/json:
 *             schema:
 *               type: object
 *               properties:
 *                 success:
 *                   type: boolean
 *                 data:
 *                   type: object
 *                   properties:
 *                     total:
 *                       type: integer
 *                     unread:
 *                       type: integer
 *                     byType:
 *                       type: object
 *                       properties:
 *                         info:
 *                           type: integer
 *                         warning:
 *                           type: integer
 *                         error:
 *                           type: integer
 *                         success:
 *                           type: integer
 *       500:
 *         description: Internal server error
 */
router.get('/stats', async (req, res) => {
  try {
    const userId = req.user.id

    const stats = await notificationService.getNotificationStats(userId)

    res.json({
      success: true,
      data: stats
    })
  } catch (error) {
    logger.error('Error getting notification stats:', error)
    res.status(500).json({
      success: false,
      error: 'Failed to retrieve notification statistics'
    })
  }
})

module.exports = router
