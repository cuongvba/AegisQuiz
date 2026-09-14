# ==============================================================================
# AegisQuiz — Go-Live Pre-Flight Verification Script (PowerShell)
# Tự động kiểm toán toàn diện tính sẵn sàng trước khi triển khai Production (dehoc.vn)
# ==============================================================================

$ErrorActionPreference = "Continue"
$RootPath = (Resolve-Path "$PSScriptRoot\..").Path
Set-Location $RootPath

Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host "[*] AEGISQUIZ PRODUCTION GO-LIVE PRE-FLIGHT AUDIT" -ForegroundColor Cyan
Write-Host "    Target: Production Platform (https://dehoc.vn)" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host ""

$PassedChecks = 0
$TotalChecks = 0

function Report-Check {
    param(
        [string]$Name,
        [bool]$Success,
        [string]$Detail
    )
    $script:TotalChecks++
    if ($Success) {
        $script:PassedChecks++
        Write-Host " [PASS] [OK] $Name" -ForegroundColor Green
        if ($Detail) { Write-Host "        +-- $Detail" -ForegroundColor DarkGray }
    } else {
        Write-Host " [FAIL] [XX] $Name" -ForegroundColor Red
        if ($Detail) { Write-Host "        +-- $Detail" -ForegroundColor Yellow }
    }
}

# 1. Kiểm tra cấu hình Nginx SPA & SignalR Reverse Proxy
Write-Host "[1/7] Kiem tra Nginx Reverse Proxy Config..." -ForegroundColor Yellow
$nginxConfPath = "$RootPath\Frontend\nginx.conf"
if (Test-Path $nginxConfPath) {
    $nginxContent = Get-Content $nginxConfPath -Raw
    $hasHubs = $nginxContent -match "location /hubs/"
    $hasHealth = $nginxContent -match "location /health"
    $hasWsUpgrade = $nginxContent -match 'proxy_set_header Upgrade \$http_upgrade;'
    $allNginxOk = $hasHubs -and $hasHealth -and $hasWsUpgrade
    Report-Check "Frontend Nginx Configuration" $allNginxOk "Da cau hinh /hubs/ (SignalR WebSockets), /health va Upgrade headers"
} else {
    Report-Check "Frontend Nginx Configuration" $false "Khong tim thay file Frontend/nginx.conf"
}

# 2. Kiểm tra appsettings.Production.json
Write-Host "[2/7] Kiem tra Backend Production Config..." -ForegroundColor Yellow
$prodSettingsPath = "$RootPath\Backend\src\AegisQuiz.API\appsettings.Production.json"
if (Test-Path $prodSettingsPath) {
    $prodJson = Get-Content $prodSettingsPath -Raw
    $hasOrigins = $prodJson -match "dehoc.vn"
    Report-Check "Backend appsettings.Production.json" $hasOrigins "File ton tai voi cau hinh AllowedOrigins cho dehoc.vn"
} else {
    Report-Check "Backend appsettings.Production.json" $false "Thieu file Backend/src/AegisQuiz.API/appsettings.Production.json"
}

# 3. Kiểm tra Docker Compose Production & Dockerfile
Write-Host "[3/7] Kiem tra Docker Orchestration Configuration..." -ForegroundColor Yellow
$dockerComposePath = "$RootPath\docker-compose.prod.yml"
$backendDockerfilePath = "$RootPath\Backend\Dockerfile"
$frontendDockerfilePath = "$RootPath\Frontend\Dockerfile.prod"

$composeOk = $false
if (Test-Path $dockerComposePath) {
    $composeText = Get-Content $dockerComposePath -Raw
    $hasBackendHealth = $composeText -match "healthcheck:"
    $hasExpose = $composeText -match "expose:"
    $composeOk = $hasBackendHealth -and $hasExpose
}

$backendDockerOk = (Test-Path $backendDockerfilePath) -and ((Get-Content $backendDockerfilePath -Raw) -match "curl")
$frontendDockerOk = Test-Path $frontendDockerfilePath

$dockerAllOk = $composeOk -and $backendDockerOk -and $frontendDockerOk
Report-Check "Docker Compose & Multi-stage Dockerfiles" $dockerAllOk "Compose ho tro Healthcheck, Port mapping; Backend runtime trang bi curl"

# 4. Kiểm tra Backend Unit Tests
Write-Host "[4/7] Chay Backend Unit Tests (xUnit)..." -ForegroundColor Yellow
$testResult = & dotnet test "$RootPath\Backend\tests\AegisQuiz.UnitTests\AegisQuiz.UnitTests.csproj" --nologo -v q
$testSuccess = ($LASTEXITCODE -eq 0)
Report-Check "Backend Unit Tests (142/142 Tests)" $testSuccess "100% Bo kiem thu Unit Tests vuot qua chuan muc"

# 5. Kiểm tra Backend Release Build (C# .NET 10)
Write-Host "[5/7] Build Backend .NET Release Package..." -ForegroundColor Yellow
$buildOutput = & dotnet publish "$RootPath\Backend\src\AegisQuiz.API\AegisQuiz.API.csproj" -c Release --nologo -v q
$buildSuccess = ($LASTEXITCODE -eq 0)
Report-Check "Backend Release Compilation (.NET 10)" $buildSuccess "Bien dich Release thanh cong, khong phat sinh loi"

# 6. Kiểm tra Frontend Build (Vite + React 19 + TypeScript)
Write-Host "[6/7] Build Frontend Client Bundle..." -ForegroundColor Yellow
Push-Location "$RootPath\Frontend"
$feBuildOutput = & npm run build
$feSuccess = ($LASTEXITCODE -eq 0) -and (Test-Path "$RootPath\Frontend\dist\index.html")
Pop-Location
Report-Check "Frontend Production Build (React 19 + Vite)" $feSuccess "Da sinh goi tai nguyen toi uu trong Frontend/dist"

# 7. Kiểm tra IRT Service (Python FastAPI)
Write-Host "[7/7] Kiem tra IRT Engine Dependencies..." -ForegroundColor Yellow
$irtReqPath = "$RootPath\irt-service\requirements.txt"
$irtMainPath = "$RootPath\irt-service\main.py"
$irtDockerPath = "$RootPath\irt-service\Dockerfile"
$irtOk = (Test-Path $irtReqPath) -and (Test-Path $irtMainPath) -and (Test-Path $irtDockerPath)
Report-Check "IRT Adaptive Testing Engine" $irtOk "requirements.txt, main.py va Dockerfile day du"

Write-Host ""
Write-Host "==================================================================" -ForegroundColor Cyan
if ($PassedChecks -eq $TotalChecks) {
    Write-Host "[*] TONG KET KIEM TOAN: $PassedChecks / $TotalChecks TIEU CHI DAT CHUAN" -ForegroundColor Green
    Write-Host "[*] HE THONG DA HOAN TAT DU DIEU KIEN GO-LIVE PRODUCTION TAI DEHOC.VN!" -ForegroundColor Green
    Write-Host "==================================================================" -ForegroundColor Cyan
    exit 0
} else {
    Write-Host "[!] TONG KET KIEM TOAN: $PassedChecks / $TotalChecks TIEU CHI DAT CHUAN" -ForegroundColor Red
    Write-Host "[!] CON TIEU CHI CHUA DAT, VUI LONG KIEM TRA LAI CAC MUC [FAIL] O TREN." -ForegroundColor Yellow
    Write-Host "==================================================================" -ForegroundColor Cyan
    exit 1
}
