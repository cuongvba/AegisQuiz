# AegisQuiz Frontend (Vite + React 19)
# Multi-stage build tối ưu

# ===== STAGE 1: Dependencies =====
FROM node:22-alpine AS deps
WORKDIR /app
COPY package*.json ./
RUN npm ci

# ===== STAGE 2: Build =====
FROM node:22-alpine AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .

ARG VITE_API_URL
ARG VITE_AUTH_HUB_URL
ENV VITE_API_URL=${VITE_API_URL}
ENV VITE_AUTH_HUB_URL=${VITE_AUTH_HUB_URL}

RUN npm run build

# ===== STAGE 3: Runtime (nginx static serving) =====
FROM nginx:alpine AS runner

COPY --from=builder /app/dist /usr/share/nginx/html

# Nginx config cho SPA (React Router)
RUN printf 'server {\n\
  listen 3000;\n\
  root /usr/share/nginx/html;\n\
  index index.html;\n\
  location / {\n\
    try_files $uri $uri/ /index.html;\n\
  }\n\
  location /api {\n\
    proxy_pass http://backend:8080;\n\
    proxy_set_header Host $host;\n\
  }\n\
}\n' > /etc/nginx/conf.d/default.conf

EXPOSE 3000
CMD ["nginx", "-g", "daemon off;"]
