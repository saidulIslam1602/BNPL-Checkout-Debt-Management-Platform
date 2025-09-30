# Build stage
FROM node:18-alpine AS build

WORKDIR /app

# Copy package files
COPY src/Web/MerchantPortal/package.json ./

# Install dependencies
RUN npm install --legacy-peer-deps

# Copy source files
COPY src/Web/MerchantPortal/ .

# Build the application
RUN npm run build:prod

# Production stage
FROM nginx:alpine AS final

# Copy custom nginx config
COPY src/Web/MerchantPortal/nginx.conf /etc/nginx/nginx.conf

# Copy built application from build stage
COPY --from=build /app/dist/merchant-portal /usr/share/nginx/html

# Add healthcheck
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD wget --quiet --tries=1 --spider http://localhost/health || exit 1

# Expose port
EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]