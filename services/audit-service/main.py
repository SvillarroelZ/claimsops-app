# =============================================================================
# Audit Service - FastAPI Application
# =============================================================================
# Lightweight microservice for recording audit events from claims-service.
# Stores events in memory (no persistence) for MVP simplicity.
#
# Endpoints:
#   GET  /health      - Health check endpoint
#   POST /audit       - Record an audit event
#   GET  /audit       - Retrieve audit events (optional filter by claimId)
#
# Technology Stack:
#   - FastAPI: Modern Python web framework with automatic OpenAPI docs
#   - Uvicorn: ASGI server for async Python applications
#   - Pydantic: Data validation using Python type hints
#
# Running locally:
#   pip install -r requirements.txt
#   uvicorn main:app --reload --port 8000
# =============================================================================

from fastapi import FastAPI, Query
from pydantic import BaseModel, Field
from typing import List, Optional
from datetime import datetime
import uuid

# =============================================================================
# FastAPI Application Instance
# =============================================================================
app = FastAPI(
    title="Audit Service",
    description="Microservice for recording and retrieving audit events",
    version="1.0.0"
)

# =============================================================================
# Data Models (Pydantic)
# =============================================================================
# Pydantic models provide:
#   - Automatic request/response validation
#   - JSON serialization/deserialization
#   - Type checking at runtime
#   - Automatic OpenAPI schema generation
# =============================================================================

class AuditEventRequest(BaseModel):
    """
    Request model for creating a new audit event.
    
    Attributes:
        claim_id: UUID of the claim being audited
        event_type: Type of event (e.g., 'created', 'updated', 'approved')
        user_id: ID of the user who triggered the event
        details: Optional additional information about the event
    
    Example:
        {
            "claim_id": "7c5ee6b1-540c-4daf-a9f1-3a72c94be865",
            "event_type": "created",
            "user_id": "system",
            "details": "Claim created via API"
        }
    """
    claim_id: str = Field(..., description="UUID of the claim")
    event_type: str = Field(..., description="Type of audit event")
    user_id: str = Field(..., description="User who triggered the event")
    details: Optional[str] = Field(None, description="Additional event details")


class AuditEventResponse(BaseModel):
    """
    Response model for audit events.
    Includes all request fields plus generated ID and timestamp.
    """
    id: str = Field(..., description="Unique event identifier")
    claim_id: str
    event_type: str
    user_id: str
    details: Optional[str]
    timestamp: datetime = Field(..., description="UTC timestamp when event was recorded")


class HealthResponse(BaseModel):
    """Health check response model."""
    status: str
    service: str
    timestamp: datetime


# =============================================================================
# In-Memory Storage
# =============================================================================
# Simple list to store audit events during runtime.
# Data is lost when the service restarts.
# In production, this would be replaced with a database (PostgreSQL, MongoDB, etc.)
# =============================================================================
audit_events: List[AuditEventResponse] = []


# =============================================================================
# API Endpoints
# =============================================================================

@app.get("/health", response_model=HealthResponse, tags=["Health"])
async def health_check():
    """
    Health check endpoint for monitoring and orchestration.
    
    Returns:
        Health status with service name and current UTC timestamp.
    
    Example Response:
        {
            "status": "healthy",
            "service": "audit-service",
            "timestamp": "2026-02-27T00:00:00.000000"
        }
    """
    return HealthResponse(
        status="healthy",
        service="audit-service",
        timestamp=datetime.utcnow()
    )


@app.post("/audit", response_model=AuditEventResponse, status_code=201, tags=["Audit"])
async def create_audit_event(event: AuditEventRequest):
    """
    Record a new audit event.
    
    Args:
        event: Audit event data (claim_id, event_type, user_id, details)
    
    Returns:
        The created audit event with generated ID and timestamp.
    
    Example Request:
        POST /audit
        {
            "claim_id": "7c5ee6b1-540c-4daf-a9f1-3a72c94be865",
            "event_type": "created",
            "user_id": "system",
            "details": "Claim created via API"
        }
    
    Example Response:
        {
            "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
            "claim_id": "7c5ee6b1-540c-4daf-a9f1-3a72c94be865",
            "event_type": "created",
            "user_id": "system",
            "details": "Claim created via API",
            "timestamp": "2026-02-27T00:00:00.000000"
        }
    """
    # Generate unique ID and timestamp
    audit_event = AuditEventResponse(
        id=str(uuid.uuid4()),
        claim_id=event.claim_id,
        event_type=event.event_type,
        user_id=event.user_id,
        details=event.details,
        timestamp=datetime.utcnow()
    )
    
    # Store in memory
    audit_events.append(audit_event)
    
    return audit_event


@app.get("/audit", response_model=List[AuditEventResponse], tags=["Audit"])
async def get_audit_events(claim_id: Optional[str] = Query(None, description="Filter by claim ID")):
    """
    Retrieve audit events, optionally filtered by claim ID.
    
    Args:
        claim_id: Optional UUID to filter events for a specific claim
    
    Returns:
        List of audit events (all events or filtered by claim_id)
    
    Examples:
        GET /audit                                          # Get all events
        GET /audit?claim_id=7c5ee6b1-540c-4daf-a9f1-...    # Get events for specific claim
    """
    if claim_id:
        # Filter events by claim_id
        return [event for event in audit_events if event.claim_id == claim_id]
    
    # Return all events
    return audit_events


# =============================================================================
# Application Startup
# =============================================================================
# FastAPI applications can define startup and shutdown event handlers.
# Useful for initializing database connections, loading configuration, etc.
# =============================================================================

@app.on_event("startup")
async def startup_event():
    """Log when the service starts."""
    print("Audit Service started successfully")
    print(f"API documentation available at: http://localhost:8000/docs")


# =============================================================================
# Running the Application
# =============================================================================
# When running directly with Python (not via uvicorn command):
#   python main.py
#
# This block starts uvicorn programmatically.
# In production/Docker, we typically use: uvicorn main:app --host 0.0.0.0 --port 8000
# =============================================================================
if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
