# 🚀 Hướng Dẫn Triển Khai & Vận Hành Hệ Thống (Deployment & DevOps Guide)

Tài liệu này hướng dẫn kỹ sư hệ thống và DevOps triển khai toàn bộ nền tảng **AegisQuiz** từ môi trường cục bộ (Local Development) đến môi trường sản xuất (Production / Kubernetes).

---

## 1. Yêu Cầu Hạ Tầng & Phần Cứng

- **Máy chủ đề xuất (Production)**:
  - CPU: 4 vCPU trở lên.
  - RAM: 8 GB RAM trở lên (khuyến nghị 16 GB để chịu tải hàng ngàn kết nối SignalR đồng thời).
  - Ổ cứng: 50 GB SSD NVMe.
  - Hệ điều hành: Ubuntu 22.04 LTS hoặc Linux Server tương đương có cài đặt Docker & Docker Compose.

---

## 2. Cấu Hình Biến Môi Trường (`.env`)

Sao chép file `.env.example` thành `.env` tại thư mục gốc:

```bash
cp .env.example .env
```

Các biến môi trường trọng yếu:
```ini
# Database (PostgreSQL)
POSTGRES_USER=aegis_admin
POSTGRES_PASSWORD=YourStrongSecretPassword123!
POSTGRES_DB=aegisquiz

# Cache (Redis)
REDIS_HOST=redis
REDIS_PORT=6379

# JWT & Bảo Mật
JWT_SECRET=YourSuperLongAndSecureJwtSigningKey256BitsMinimum!
WEBHOOK_SECRET=YourWebhookSecretKeyForPayments

# AI Service (Google Gemini)
GEMINI_API_KEY=AIzaSyYourGeminiApiKeyHere

# IRT Engine URL
IRT_SERVICE_URL=http://irt-service:8001
```

---

## 3. Khởi Chạy Bằng Docker Compose

Toàn bộ hệ sinh thái (Backend, Frontend, Python IRT, PostgreSQL, Redis, Keycloak) được định nghĩa hoàn chỉnh trong file `docker-compose.yml`:

```bash
# Khởi động toàn bộ các dịch vụ dưới nền
docker compose up -d

# Xem log các dịch vụ
docker compose logs -f backend
docker compose logs -f irt-service
docker compose logs -f frontend

# Kiểm tra trạng thái hoạt động (Healthcheck)
docker compose ps
```

---

## 4. Cấu Hình Nginx Reverse Proxy & SSL (HTTPS / WSS)

Cấu hình mẫu cho máy chủ Production hỗ trợ cả HTTPS và nâng cấp giao thức WebSockets cho SignalR:

```nginx
server {
    listen 80;
    server_name quiz.yourdomain.com;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name quiz.yourdomain.com;

    ssl_certificate /etc/letsencrypt/live/quiz.yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/quiz.yourdomain.com/privkey.pem;

    # Frontend SPA
    location / {
        proxy_pass http://localhost:3000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }

    # Backend REST API
    location /api/ {
        proxy_pass http://localhost:8080/api/;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # SignalR WebSockets Hub
    location /hubs/ {
        proxy_pass http://localhost:8080/hubs/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_read_timeout 3600s;
        proxy_send_timeout 3600s;
    }
}
```

---

## 5. Sao Lưu Dữ Liệu & Khắc Phục Sự Cố (Backups & Maintenance)

### 5.1. Sao lưu PostgreSQL định kỳ:
```bash
docker compose exec postgres pg_dump -U aegis_admin aegisquiz > backup_$(date +%Y%m%d_%H%M%S).sql
```

### 5.2. Khởi động lại khi nâng cấp phiên bản mới:
```bash
git pull origin main
docker compose build
docker compose up -d
```
