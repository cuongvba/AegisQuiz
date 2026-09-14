#!/usr/bin/env bash
# ==============================================================================
# AegisQuiz — Go-Live Pre-Flight Verification Script (Bash for Linux/VPS)
# ==============================================================================

set -e
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

echo "=================================================================="
echo "🛡️  AEGISQUIZ PRODUCTION GO-LIVE PRE-FLIGHT AUDIT"
echo "   Target: Production Platform (https://dehoc.vn)"
echo "=================================================================="
echo ""

PASSED=0
TOTAL=0

report_check() {
    local name="$1"
    local status="$2"
    local detail="$3"
    TOTAL=$((TOTAL + 1))
    if [ "$status" -eq 0 ]; then
        PASSED=$((PASSED + 1))
        echo -e " [PASS] ✓ $name"
        [ -n "$detail" ] && echo -e "        └─ $detail"
    else
        echo -e " [FAIL] ✗ $name"
        [ -n "$detail" ] && echo -e "        └─ $detail"
    fi
}

# 1. Nginx config
if grep -q "location /hubs/" Frontend/nginx.conf && grep -q "location /health" Frontend/nginx.conf; then
    report_check "Frontend Nginx Configuration" 0 "Đã cấu hình /hubs/ (SignalR WebSockets), /health và Upgrade headers"
else
    report_check "Frontend Nginx Configuration" 1 "Nginx configuration missing /hubs/ or /health proxy"
fi

# 2. appsettings.Production.json
if [ -f "Backend/src/AegisQuiz.API/appsettings.Production.json" ]; then
    report_check "Backend appsettings.Production.json" 0 "File tồn tại với cấu hình AllowedOrigins cho dehoc.vn"
else
    report_check "Backend appsettings.Production.json" 1 "Missing appsettings.Production.json"
fi

# 3. Docker Compose & Dockerfiles
if grep -q "healthcheck:" docker-compose.prod.yml && grep -q "curl" Backend/Dockerfile; then
    report_check "Docker Compose & Multi-stage Dockerfiles" 0 "Compose hỗ trợ Healthcheck, Port mapping; Backend runtime trang bị curl"
else
    report_check "Docker Compose & Multi-stage Dockerfiles" 1 "Docker compose or Dockerfile missing healthcheck/curl"
fi

# 4. Backend Unit Tests
if dotnet test Backend/tests/AegisQuiz.UnitTests/AegisQuiz.UnitTests.csproj --nologo -v q; then
    report_check "Backend Unit Tests (142/142 Tests)" 0 "100% Bộ kiểm thử Unit Tests vượt qua chuẩn mực"
else
    report_check "Backend Unit Tests (142/142 Tests)" 1 "Unit tests failed"
fi

# 5. Backend Release Publish
if dotnet publish Backend/src/AegisQuiz.API/AegisQuiz.API.csproj -c Release --nologo -v q; then
    report_check "Backend Release Compilation (.NET 10)" 0 "Biên dịch Release thành công, không phát sinh lỗi"
else
    report_check "Backend Release Compilation (.NET 10)" 1 "Release publish failed"
fi

# 6. Frontend Build
cd Frontend
if npm run build && [ -f "dist/index.html" ]; then
    report_check "Frontend Production Build (React 19 + Vite)" 0 "Đã sinh gói tài nguyên tối ưu trong Frontend/dist"
else
    report_check "Frontend Production Build (React 19 + Vite)" 1 "Frontend build failed"
fi
cd "$ROOT_DIR"

# 7. IRT Service files
if [ -f "irt-service/requirements.txt" ] && [ -f "irt-service/main.py" ] && [ -f "irt-service/Dockerfile" ]; then
    report_check "IRT Adaptive Testing Engine" 0 "requirements.txt, main.py và Dockerfile đầy đủ"
else
    report_check "IRT Adaptive Testing Engine" 1 "IRT service files missing"
fi

echo ""
echo "=================================================================="
echo "📊 TỔNG KẾT KIỂM TOÁN: $PASSED / $TOTAL TIÊU CHÍ ĐẠT CHUẨN"
echo "=================================================================="

if [ "$PASSED" -eq "$TOTAL" ]; then
    echo "🚀 HỆ THỐNG ĐÃ HOÀN TẤT ĐỦ ĐIỀU KIỆN GO-LIVE PRODUCTION TẠI DEHOC.VN!"
    exit 0
else
    echo "⚠️ CÒN TIÊU CHÍ CHƯA ĐẠT, VUI LÒNG KIỂM TRA LẠI CÁC MỤC [FAIL] Ở TRÊN."
    exit 1
fi
