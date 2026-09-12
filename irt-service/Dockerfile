# AegisQuiz IRT Service — Multi-stage Dockerfile
# Stage 1: Builder
FROM python:3.12-slim AS builder

WORKDIR /app
COPY requirements.txt .

# Cài đặt dependencies vào venv độc lập
RUN python -m venv /opt/venv
ENV PATH="/opt/venv/bin:$PATH"
RUN pip install --no-cache-dir --upgrade pip \
 && pip install --no-cache-dir -r requirements.txt

# Stage 2: Runtime (distroless-like minimal image)
FROM python:3.12-slim AS runtime

# Tạo non-root user (Security best practice)
RUN useradd --uid 1001 --create-home aegis
USER aegis

WORKDIR /app
COPY --from=builder /opt/venv /opt/venv
COPY --chown=aegis:aegis main.py .

ENV PATH="/opt/venv/bin:$PATH"
ENV PYTHONDONTWRITEBYTECODE=1
ENV PYTHONUNBUFFERED=1

EXPOSE 8001

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD python -c "import urllib.request; urllib.request.urlopen('http://localhost:8001/health')"

CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8001", "--workers", "2"]
