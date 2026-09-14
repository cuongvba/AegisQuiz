# AegisQuiz Backend — .NET 10 Multi-stage build
# Fix: bỏ --no-restore ở publish


# ===== STAGE 1: Build =====
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution và tất cả csproj (cache layer cho dotnet restore)
COPY AegisQuiz.sln ./
COPY src/AegisQuiz.Domain/AegisQuiz.Domain.csproj             src/AegisQuiz.Domain/
COPY src/AegisQuiz.Application/AegisQuiz.Application.csproj   src/AegisQuiz.Application/
COPY src/AegisQuiz.Infrastructure/AegisQuiz.Infrastructure.csproj src/AegisQuiz.Infrastructure/
COPY src/AegisQuiz.API/AegisQuiz.API.csproj                   src/AegisQuiz.API/

# Restore packages (separate layer để cache Docker)
RUN dotnet restore src/AegisQuiz.API/AegisQuiz.API.csproj

# Copy toàn bộ source và publish
COPY . .
RUN dotnet publish src/AegisQuiz.API/AegisQuiz.API.csproj -c Release -o /app/publish

# ===== STAGE 2: Runtime =====
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends libgdiplus libfontconfig1 curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AegisQuiz.API.dll"]
