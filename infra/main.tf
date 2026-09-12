# [Cảnh giới 4: Cloud Provisioning] Hạ tầng GCP cho AegisQuiz
# Lệnh triển khai: terraform init && terraform apply

terraform {
  required_providers {
    google = {
      source  = "hashicorp/google"
      version = "~> 6.0"
    }
  }
}

provider "google" {
  project = var.project_id
  region  = var.region
}

variable "project_id" {
  description = "GCP Project ID"
  type        = string
}

variable "region" {
  default = "asia-southeast1" # Singapore (gần Việt Nam nhất)
}

variable "gemini_api_key" {
  description = "Google Gemini API Key"
  type        = string
  sensitive   = true
}

# ===== CLOUD SQL (PostgreSQL) =====
resource "google_sql_database_instance" "aegisquiz_db" {
  name             = "aegisquiz-db"
  database_version = "POSTGRES_17"
  region           = var.region

  settings {
    tier              = "db-f1-micro" # Tier nhỏ nhất (Dev/Startup)
    availability_type = "ZONAL"

    backup_configuration {
      enabled = true
    }
  }

  deletion_protection = false
}

resource "google_sql_database" "aegisquiz" {
  name     = "aegisquiz"
  instance = google_sql_database_instance.aegisquiz_db.name
}

# ===== REDIS (Memorystore) =====
resource "google_redis_instance" "aegisquiz_cache" {
  name           = "aegisquiz-cache"
  tier           = "BASIC"
  memory_size_gb = 1
  region         = var.region
}

# ===== CLOUD RUN: BACKEND =====
resource "google_cloud_run_v2_service" "backend" {
  name     = "aegisquiz-backend"
  location = var.region

  template {
    containers {
      image = "gcr.io/${var.project_id}/aegisquiz-backend:latest"

      ports {
        container_port = 8080
      }

      env {
        name  = "ConnectionStrings__DefaultConnection"
        value = "Host=${google_sql_database_instance.aegisquiz_db.private_ip_address};Database=aegisquiz;Username=postgres;Password=SET_IN_SECRET_MANAGER"
      }

      env {
        name  = "ConnectionStrings__Redis"
        value = "${google_redis_instance.aegisquiz_cache.host}:${google_redis_instance.aegisquiz_cache.port}"
      }

      env {
        name  = "Gemini__ApiKey"
        value = var.gemini_api_key
      }

      resources {
        limits = {
          cpu    = "1"
          memory = "512Mi"
        }
      }
    }

    scaling {
      min_instance_count = 0  # Scale-to-zero (Tiết kiệm tiền)
      max_instance_count = 10 # Tối đa 10 instance khi traffic cao
    }
  }
}

# ===== CLOUD RUN: FRONTEND =====
resource "google_cloud_run_v2_service" "frontend" {
  name     = "aegisquiz-frontend"
  location = var.region

  template {
    containers {
      image = "gcr.io/${var.project_id}/aegisquiz-frontend:latest"

      ports {
        container_port = 3000
      }

      env {
        name  = "NEXT_PUBLIC_API_URL"
        value = google_cloud_run_v2_service.backend.uri
      }

      resources {
        limits = {
          cpu    = "1"
          memory = "256Mi"
        }
      }
    }

    scaling {
      min_instance_count = 0
      max_instance_count = 5
    }
  }
}

# ===== MỞ PUBLIC (Cho phép học sinh truy cập không cần login GCP) =====
resource "google_cloud_run_v2_service_iam_member" "frontend_public" {
  name     = google_cloud_run_v2_service.frontend.name
  location = var.region
  role     = "roles/run.invoker"
  member   = "allUsers"
}

# ===== OUTPUTS =====
output "frontend_url" {
  value       = google_cloud_run_v2_service.frontend.uri
  description = "URL trang web AegisQuiz (Cho học sinh)"
}

output "backend_url" {
  value       = google_cloud_run_v2_service.backend.uri
  description = "URL API Backend (Cho Admin/Webhook)"
}
