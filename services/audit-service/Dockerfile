# =============================================================================
# Audit Service Dockerfile
# =============================================================================
# Multi-stage build for Python FastAPI application.
# Uses official Python 3.11 slim image for smaller footprint.
#
# Build stages:
#   1. Base: Install dependencies
#   2. Runtime: Copy application code and run
# =============================================================================

FROM python:3.11-slim

# Set working directory inside container
WORKDIR /app

# Copy requirements file first (layer caching optimization)
# If requirements.txt doesn't change, Docker reuses this layer
COPY services/audit-service/requirements.txt .

# Install Python dependencies
# --no-cache-dir: Don't cache pip packages (reduces image size)
# --upgrade pip: Ensure latest pip version
RUN pip install --no-cache-dir --upgrade pip && \
    pip install --no-cache-dir -r requirements.txt

# Copy application code
COPY services/audit-service/main.py .

# Expose port 8000 for FastAPI/Uvicorn
EXPOSE 8000

# Run the application
# --host 0.0.0.0: Listen on all network interfaces (required for Docker)
# --port 8000: Port number
# main:app: module_name:fastapi_instance
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
