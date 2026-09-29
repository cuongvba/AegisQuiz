<#
============================================================================
 AegisQuiz — Dev Launcher (menu chọn Backend / Frontend / All)
============================================================================
 Thay vì phải gõ tay lệnh docker compose dài với nhiều cờ -f, script này hiện
 menu lựa chọn và tự chạy đúng lệnh tương ứng ở chế độ DEV (Hot-Reload qua
 Backend/Dockerfile.dev + Frontend/Dockerfile.dev, override bởi
 docker-compose.dev.yml — Postgres/Redis/Keycloak/IRT-Service dùng chung từ
 docker-compose.yml gốc).

 [Tối ưu tốc độ] Hệ thống chủ yếu thay đổi ở Backend/Frontend — script CHỈ
 build lại (--build) đúng 2 container này. Hạ tầng dùng chung (Postgres/
 Redis/Keycloak/IRT-Service) được khởi động bằng `up -d` KHÔNG kèm --build:
 nếu image đã có sẵn từ lần chạy trước thì tái sử dụng ngay (không mất thời
 gian build lại mỗi lần), chỉ build nếu đó là lần đầu tiên chưa có image.

 Cách chạy (từ thư mục gốc dự án, PowerShell):
   .\dev.ps1

 Có thể chạy thẳng không cần menu bằng tham số:
   .\dev.ps1 -Target backend
   .\dev.ps1 -Target frontend
   .\dev.ps1 -Target all
   .\dev.ps1 -Down          # Dừng & dọn toàn bộ container dev đang chạy
============================================================================
#>

param(
    [ValidateSet('backend', 'frontend', 'all')]
    [string]$Target,

    [switch]$Down
)

$ErrorActionPreference = 'Stop'

# Luôn thao tác từ thư mục chứa chính script này — chạy đúng dù người dùng
# đứng ở thư mục nào khi gọi .\dev.ps1
Set-Location -Path $PSScriptRoot

$ComposeArgs   = @('-f', 'docker-compose.yml', '-f', 'docker-compose.dev.yml')
# Các service hạ tầng dùng chung, hiếm khi thay đổi — KHÔNG cần rebuild mỗi lần.
$InfraServices = @('postgres', 'redis', 'keycloak', 'irt-service')

function Ensure-EnvFile {
    if (-not (Test-Path '.env')) {
        Write-Host "[dev.ps1] Chưa có file .env — tự tạo từ .env.example ..." -ForegroundColor Yellow
        Copy-Item '.env.example' '.env'
        Write-Host "[dev.ps1] Đã tạo .env. Hãy mở file và điền các giá trị CHANGE_ME_* trước khi chạy thật (mật khẩu DB, JWT secret, v.v.)." -ForegroundColor Yellow
    }
}

function Start-Infra {
    # Không truyền --build: Docker Compose chỉ build khi CHƯA có image (lần đầu),
    # các lần sau tái sử dụng image cũ ngay lập tức — tiết kiệm thời gian đáng kể.
    Write-Host "[dev.ps1] Đảm bảo hạ tầng dùng chung đang chạy: $($InfraServices -join ', ') (không rebuild nếu đã có image)..." -ForegroundColor DarkGray
    docker compose @ComposeArgs up -d @InfraServices
}

function Start-DevStack {
    param([Parameter(Mandatory)][string]$Choice)

    Start-Infra

    switch ($Choice) {
        'backend' {
            Write-Host "`n[dev.ps1] Build lại & khởi động RIÊNG container DEV: BACKEND...`n" -ForegroundColor Cyan
            docker compose @ComposeArgs up --build backend
        }
        'frontend' {
            Write-Host "`n[dev.ps1] Build lại & khởi động RIÊNG container DEV: FRONTEND...`n" -ForegroundColor Cyan
            docker compose @ComposeArgs up --build frontend
        }
        'all' {
            Write-Host "`n[dev.ps1] Build lại & khởi động 2 container DEV: BACKEND + FRONTEND...`n" -ForegroundColor Cyan
            # Chỉ định rõ 2 service — tránh Compose vô tình build lại irt-service
            # (không có code thay đổi) như khi gọi `up --build` không kèm tên service.
            docker compose @ComposeArgs up --build backend frontend
        }
    }
}

function Stop-DevStack {
    Write-Host "`n[dev.ps1] Dừng & dọn toàn bộ container DEV...`n" -ForegroundColor Cyan
    docker compose @ComposeArgs down
}

function Show-Menu {
    Write-Host ''
    Write-Host '=========================================================' -ForegroundColor DarkCyan
    Write-Host '   AegisQuiz — Dev Container Launcher (Docker Desktop)   ' -ForegroundColor DarkCyan
    Write-Host '=========================================================' -ForegroundColor DarkCyan
    Write-Host '  [1] Backend   — chỉ build/chạy lại container Backend (.NET, dotnet watch)'
    Write-Host '  [2] Frontend  — chỉ build/chạy lại container Frontend (Vite, HMR)'
    Write-Host '  [3] All       — build/chạy lại cả Backend + Frontend'
    Write-Host '        (Hạ tầng Postgres/Redis/Keycloak/IRT-Service: chỉ khởi động,'
    Write-Host '         KHÔNG rebuild trừ khi chưa từng có image)'
    Write-Host '  [0] Down      — dừng & dọn toàn bộ container dev đang chạy'
    Write-Host '  [Q] Thoát'
    Write-Host '=========================================================' -ForegroundColor DarkCyan

    $selection = Read-Host 'Chọn một mục (1/2/3/0/Q)'
    switch ($selection.Trim().ToUpperInvariant()) {
        '1' { return 'backend' }
        '2' { return 'frontend' }
        '3' { return 'all' }
        '0' { return 'down' }
        'Q' { return 'quit' }
        default {
            Write-Host "[dev.ps1] Lựa chọn không hợp lệ: '$selection'" -ForegroundColor Red
            return Show-Menu
        }
    }
}

# ── Entry point ─────────────────────────────────────────────────────────────
Ensure-EnvFile

if ($Down) {
    Stop-DevStack
    return
}

if ($Target) {
    Start-DevStack -Choice $Target
    return
}

$choice = Show-Menu
if ($choice -eq 'quit') {
    Write-Host '[dev.ps1] Đã thoát, không chạy gì cả.' -ForegroundColor Yellow
    return
}
if ($choice -eq 'down') {
    Stop-DevStack
    return
}

Start-DevStack -Choice $choice
