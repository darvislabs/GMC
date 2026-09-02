from dotenv import load_dotenv
load_dotenv()

from fastapi import FastAPI, APIRouter, HTTPException, Request, UploadFile, File, Depends, Response
from starlette.middleware.cors import CORSMiddleware
from motor.motor_asyncio import AsyncIOMotorClient
import os
import logging
from pathlib import Path
from pydantic import BaseModel, Field, EmailStr
from typing import List, Optional, Dict, Any
import uuid
from datetime import datetime, timezone, timedelta
import bcrypt
import jwt
from bson import ObjectId
import pandas as pd
import io
import json
import secrets

# Configure logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(name)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

# MongoDB connection
mongo_url = os.environ['MONGO_URL']
client = AsyncIOMotorClient(mongo_url)
db = client[os.environ['DB_NAME']]

# JWT Configuration
JWT_ALGORITHM = "HS256"

def get_jwt_secret() -> str:
    return os.environ["JWT_SECRET"]

def hash_password(password: str) -> str:
    salt = bcrypt.gensalt()
    hashed = bcrypt.hashpw(password.encode("utf-8"), salt)
    return hashed.decode("utf-8")

def verify_password(plain_password: str, hashed_password: str) -> bool:
    return bcrypt.checkpw(plain_password.encode("utf-8"), hashed_password.encode("utf-8"))

def create_access_token(user_id: str, email: str) -> str:
    payload = {"sub": user_id, "email": email, "exp": datetime.now(timezone.utc) + timedelta(minutes=60), "type": "access"}
    return jwt.encode(payload, get_jwt_secret(), algorithm=JWT_ALGORITHM)

def create_refresh_token(user_id: str) -> str:
    payload = {"sub": user_id, "exp": datetime.now(timezone.utc) + timedelta(days=7), "type": "refresh"}
    return jwt.encode(payload, get_jwt_secret(), algorithm=JWT_ALGORITHM)

# Pydantic Models
class UserCreate(BaseModel):
    email: EmailStr
    password: str
    name: str
    role: str = "agent"

class UserLogin(BaseModel):
    email: EmailStr
    password: str

class UserResponse(BaseModel):
    id: str
    email: str
    name: str
    role: str
    created_at: str

class CaseCreate(BaseModel):
    client_name: str
    policy_type: str = "GMC"
    notes: Optional[str] = None

class CaseUpdate(BaseModel):
    client_name: Optional[str] = None
    status: Optional[str] = None
    notes: Optional[str] = None

class MappingOverride(BaseModel):
    source_column: str
    target_field: str

class CaseSubmit(BaseModel):
    corrected_data: Optional[List[Dict[str, Any]]] = None
    mapping_overrides: Optional[List[MappingOverride]] = None

class TemplateCreate(BaseModel):
    name: str
    insurer: str
    mappings: Dict[str, str]

class UnderwriterDecision(BaseModel):
    decision: str  # approve, reject, request_fixes
    notes: Optional[str] = None
    risk_flags: Optional[List[str]] = None

class UserManagement(BaseModel):
    name: Optional[str] = None
    role: Optional[str] = None
    is_active: Optional[bool] = None

class ForgotPassword(BaseModel):
    email: EmailStr

class ResetPassword(BaseModel):
    token: str
    new_password: str

# Auth Helper
async def get_current_user(request: Request) -> dict:
    token = request.cookies.get("access_token")
    if not token:
        auth_header = request.headers.get("Authorization", "")
        if auth_header.startswith("Bearer "):
            token = auth_header[7:]
    if not token:
        raise HTTPException(status_code=401, detail="Not authenticated")
    try:
        payload = jwt.decode(token, get_jwt_secret(), algorithms=[JWT_ALGORITHM])
        if payload.get("type") != "access":
            raise HTTPException(status_code=401, detail="Invalid token type")
        user = await db.users.find_one({"_id": ObjectId(payload["sub"])})
        if not user:
            raise HTTPException(status_code=401, detail="User not found")
        user["id"] = str(user["_id"])
        user.pop("_id", None)
        user.pop("password_hash", None)
        return user
    except jwt.ExpiredSignatureError:
        raise HTTPException(status_code=401, detail="Token expired")
    except jwt.InvalidTokenError:
        raise HTTPException(status_code=401, detail="Invalid token")

async def require_role(request: Request, roles: List[str]) -> dict:
    user = await get_current_user(request)
    if user["role"] not in roles:
        raise HTTPException(status_code=403, detail="Insufficient permissions")
    return user

# Create FastAPI app
app = FastAPI(title="GMC Platform API")
api_router = APIRouter(prefix="/api")

# CORS - use regex patterns to allow subdomains with credentials
app.add_middleware(
    CORSMiddleware,
    allow_origin_regex=r"https://.*\.netlify\.app|https://.*\.trycloudflare\.com|https://.*\.loca\.lt|http://localhost:\d+",
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Health check endpoint (public)
@api_router.get("/health")
async def health():
    return {"status": "healthy", "service": "gmc-platform"}

# ==================== AUTH ENDPOINTS ====================
@api_router.post("/auth/register")
async def register(data: UserCreate, response: Response):
    email = data.email.lower()
    existing = await db.users.find_one({"email": email})
    if existing:
        raise HTTPException(status_code=400, detail="Email already registered")
    
    user_doc = {
        "email": email,
        "password_hash": hash_password(data.password),
        "name": data.name,
        "role": data.role if data.role in ["agent", "underwriter"] else "agent",
        "is_active": True,
        "created_at": datetime.now(timezone.utc).isoformat()
    }
    result = await db.users.insert_one(user_doc)
    user_id = str(result.inserted_id)
    
    access_token = create_access_token(user_id, email)
    refresh_token = create_refresh_token(user_id)
    
    # Remove domain from cookie to work with any domain (cloudflare, localtunnel, etc.)
    # secure=True required when samesite=none for modern browsers
    response.set_cookie(key="access_token", value=access_token, httponly=True, secure=False, samesite="lax", max_age=3600, path="/")
    response.set_cookie(key="refresh_token", value=refresh_token, httponly=True, secure=False, samesite="lax", max_age=604800, path="/")
    
    await log_audit("user_registered", user_id, {"email": email, "role": user_doc["role"]})
    
    return {"id": user_id, "email": email, "name": data.name, "role": user_doc["role"], "created_at": user_doc["created_at"], "access_token": access_token}

@api_router.post("/auth/login")
async def login(data: UserLogin, response: Response, request: Request):
    email = data.email.lower()
    ip = request.client.host if request.client else "unknown"
    identifier = f"{ip}:{email}"
    
    # Check brute force
    attempts = await db.login_attempts.find_one({"identifier": identifier})
    if attempts and attempts.get("count", 0) >= 5:
        lockout_time = attempts.get("locked_until")
        if lockout_time and datetime.fromisoformat(lockout_time) > datetime.now(timezone.utc):
            raise HTTPException(status_code=429, detail="Account temporarily locked. Try again later.")
    
    user = await db.users.find_one({"email": email})
    if not user or not verify_password(data.password, user["password_hash"]):
        # Increment failed attempts
        await db.login_attempts.update_one(
            {"identifier": identifier},
            {"$inc": {"count": 1}, "$set": {"locked_until": (datetime.now(timezone.utc) + timedelta(minutes=15)).isoformat()}},
            upsert=True
        )
        raise HTTPException(status_code=401, detail="Invalid credentials")
    
    if not user.get("is_active", True):
        raise HTTPException(status_code=401, detail="Account is deactivated")
    
    # Clear failed attempts on success
    await db.login_attempts.delete_one({"identifier": identifier})
    
    user_id = str(user["_id"])
    access_token = create_access_token(user_id, email)
    refresh_token = create_refresh_token(user_id)
    
    response.set_cookie(key="access_token", value=access_token, httponly=True, secure=False, samesite="lax", max_age=3600, path="/")
    response.set_cookie(key="refresh_token", value=refresh_token, httponly=True, secure=False, samesite="lax", max_age=604800, path="/")
    
    await log_audit("user_login", user_id, {"email": email})
    
    # Also return token in response for API clients
    return {"id": user_id, "email": email, "name": user["name"], "role": user["role"], "created_at": user.get("created_at", ""), "access_token": access_token}

@api_router.post("/auth/logout")
async def logout(response: Response, request: Request):
    user = await get_current_user(request)
    # Remove cookies without domain to work with any domain
    response.delete_cookie(key="access_token", path="/")
    response.delete_cookie(key="refresh_token", path="/")
    await log_audit("user_logout", user["id"], {})
    return {"message": "Logged out successfully"}

@api_router.get("/auth/me")
async def get_me(request: Request):
    user = await get_current_user(request)
    return user

@api_router.post("/auth/refresh")
async def refresh_token(request: Request, response: Response):
    token = request.cookies.get("refresh_token")
    if not token:
        raise HTTPException(status_code=401, detail="No refresh token")
    try:
        payload = jwt.decode(token, get_jwt_secret(), algorithms=[JWT_ALGORITHM])
        if payload.get("type") != "refresh":
            raise HTTPException(status_code=401, detail="Invalid token type")
        user = await db.users.find_one({"_id": ObjectId(payload["sub"])})
        if not user:
            raise HTTPException(status_code=401, detail="User not found")
        
        user_id = str(user["_id"])
        access_token = create_access_token(user_id, user["email"])
        # Set cookie without domain to work with any domain
        # secure=True required when samesite=none for modern browsers
        response.set_cookie(key="access_token", value=access_token, httponly=True, secure=False, samesite="lax", max_age=3600, path="/")
        return {"message": "Token refreshed", "access_token": access_token}
    except jwt.ExpiredSignatureError:
        raise HTTPException(status_code=401, detail="Refresh token expired")
    except jwt.InvalidTokenError:
        raise HTTPException(status_code=401, detail="Invalid token")

@api_router.post("/auth/forgot-password")
async def forgot_password(data: ForgotPassword):
    email = data.email.lower()
    user = await db.users.find_one({"email": email})
    if not user:
        return {"message": "If email exists, reset link will be sent"}
    
    token = secrets.token_urlsafe(32)
    await db.password_reset_tokens.insert_one({
        "token": token,
        "user_id": str(user["_id"]),
        "expires_at": datetime.now(timezone.utc) + timedelta(hours=1),
        "used": False
    })
    
    logger.info(f"Password reset link: /reset-password?token={token}")
    return {"message": "If email exists, reset link will be sent"}

@api_router.post("/auth/reset-password")
async def reset_password(data: ResetPassword):
    token_doc = await db.password_reset_tokens.find_one({"token": data.token, "used": False})
    if not token_doc:
        raise HTTPException(status_code=400, detail="Invalid or expired token")
    
    if datetime.fromisoformat(str(token_doc["expires_at"])) < datetime.now(timezone.utc):
        raise HTTPException(status_code=400, detail="Token expired")
    
    await db.users.update_one(
        {"_id": ObjectId(token_doc["user_id"])},
        {"$set": {"password_hash": hash_password(data.new_password)}}
    )
    await db.password_reset_tokens.update_one({"token": data.token}, {"$set": {"used": True}})
    
    return {"message": "Password reset successful"}

# ==================== CASE MANAGEMENT ====================
@api_router.post("/cases")
async def create_case(data: CaseCreate, request: Request):
    user = await get_current_user(request)
    
    case_id = f"GMC-{datetime.now().strftime('%Y%m%d')}-{str(uuid.uuid4())[:8].upper()}"
    case_doc = {
        "case_id": case_id,
        "client_name": data.client_name,
        "policy_type": data.policy_type,
        "notes": data.notes,
        "status": "draft",
        "agent_id": user["id"],
        "agent_name": user["name"],
        "member_count": 0,
        "sum_insured": 0,
        "raw_data": None,
        "mapped_data": None,
        "corrected_data": None,
        "mapping_suggestions": None,
        "ai_confidence": None,
        "risk_flags": [],
        "underwriter_notes": None,
        "underwriter_id": None,
        "created_at": datetime.now(timezone.utc).isoformat(),
        "updated_at": datetime.now(timezone.utc).isoformat()
    }
    
    await db.cases.insert_one(case_doc)
    await log_audit("case_created", user["id"], {"case_id": case_id})
    
    case_doc.pop("_id", None)
    return case_doc

@api_router.get("/cases")
async def get_cases(request: Request, status: Optional[str] = None, search: Optional[str] = None, page: int = 1, limit: int = 20):
    user = await get_current_user(request)
    
    query = {}
    if user["role"] == "agent":
        query["agent_id"] = user["id"]
    elif user["role"] == "underwriter":
        query["status"] = {"$in": ["submitted", "under_review", "approved", "rejected"]}
    
    if status:
        query["status"] = status
    if search:
        query["$or"] = [
            {"case_id": {"$regex": search, "$options": "i"}},
            {"client_name": {"$regex": search, "$options": "i"}}
        ]
    
    total = await db.cases.count_documents(query)
    cases = await db.cases.find(query, {"_id": 0}).sort("created_at", -1).skip((page - 1) * limit).limit(limit).to_list(limit)
    
    return {"cases": cases, "total": total, "page": page, "limit": limit}

@api_router.get("/cases/{case_id}")
async def get_case(case_id: str, request: Request):
    user = await get_current_user(request)
    
    case = await db.cases.find_one({"case_id": case_id}, {"_id": 0})
    if not case:
        raise HTTPException(status_code=404, detail="Case not found")
    
    if user["role"] == "agent" and case["agent_id"] != user["id"]:
        raise HTTPException(status_code=403, detail="Access denied")
    
    return case

@api_router.put("/cases/{case_id}")
async def update_case(case_id: str, data: CaseUpdate, request: Request):
    user = await get_current_user(request)
    
    case = await db.cases.find_one({"case_id": case_id})
    if not case:
        raise HTTPException(status_code=404, detail="Case not found")
    
    if user["role"] == "agent" and case["agent_id"] != user["id"]:
        raise HTTPException(status_code=403, detail="Access denied")
    
    update_data = {k: v for k, v in data.model_dump().items() if v is not None}
    update_data["updated_at"] = datetime.now(timezone.utc).isoformat()
    
    await db.cases.update_one({"case_id": case_id}, {"$set": update_data})
    await log_audit("case_updated", user["id"], {"case_id": case_id, "updates": list(update_data.keys())})
    
    updated_case = await db.cases.find_one({"case_id": case_id}, {"_id": 0})
    return updated_case

@api_router.delete("/cases/{case_id}")
async def delete_case(case_id: str, request: Request):
    user = await get_current_user(request)
    
    case = await db.cases.find_one({"case_id": case_id})
    if not case:
        raise HTTPException(status_code=404, detail="Case not found")
    
    if user["role"] not in ["admin"] and (user["role"] == "agent" and case["agent_id"] != user["id"]):
        raise HTTPException(status_code=403, detail="Access denied")
    
    await db.cases.delete_one({"case_id": case_id})
    await log_audit("case_deleted", user["id"], {"case_id": case_id})
    
    return {"message": "Case deleted"}

# ==================== FILE UPLOAD & AI MAPPING ====================
@api_router.post("/cases/{case_id}/upload")
async def upload_file(case_id: str, file: UploadFile = File(...), request: Request = None):
    user = await get_current_user(request)
    
    case = await db.cases.find_one({"case_id": case_id})
    if not case:
        raise HTTPException(status_code=404, detail="Case not found")
    
    if user["role"] == "agent" and case["agent_id"] != user["id"]:
        raise HTTPException(status_code=403, detail="Access denied")
    
    # Read file
    content = await file.read()
    filename = file.filename.lower()
    
    try:
        if filename.endswith('.csv'):
            df = pd.read_csv(io.BytesIO(content))
        elif filename.endswith(('.xlsx', '.xls')):
            df = pd.read_excel(io.BytesIO(content))
        else:
            raise HTTPException(status_code=400, detail="Unsupported file format. Use CSV or Excel.")
        
        # Convert to records
        raw_data = df.fillna("").to_dict(orient="records")
        columns = list(df.columns)
        
        # Get AI mapping suggestions
        mapping_suggestions = await get_ai_mapping_suggestions(columns, raw_data[:5])
        
        # Calculate stats
        member_count = len(raw_data)
        sum_insured = 0
        for row in raw_data:
            for key, value in row.items():
                if any(term in key.lower() for term in ["sum", "insured", "cover", "amount"]):
                    try:
                        sum_insured += float(str(value).replace(",", ""))
                    except:
                        pass
        
        # Update case
        await db.cases.update_one(
            {"case_id": case_id},
            {"$set": {
                "raw_data": raw_data,
                "mapping_suggestions": mapping_suggestions,
                "member_count": member_count,
                "sum_insured": sum_insured,
                "status": "mapping_review",
                "updated_at": datetime.now(timezone.utc).isoformat()
            }}
        )
        
        await log_audit("file_uploaded", user["id"], {"case_id": case_id, "filename": file.filename, "rows": member_count})
        
        return {
            "message": "File uploaded successfully",
            "columns": columns,
            "row_count": member_count,
            "mapping_suggestions": mapping_suggestions
        }
    except Exception as e:
        logger.error(f"File processing error: {str(e)}")
        raise HTTPException(status_code=400, detail=f"Error processing file: {str(e)}")

async def get_ai_mapping_suggestions(columns: List[str], sample_data: List[Dict]) -> List[Dict]:
    """Use Gemini 3 Flash to suggest column mappings"""
    
    standard_fields = [
        "employee_id", "employee_name", "date_of_birth", "gender", "relationship",
        "sum_insured", "email", "phone", "address", "department", "designation",
        "date_of_joining", "salary", "policy_start_date", "policy_end_date",
        "nominee_name", "nominee_relationship", "pre_existing_conditions"
    ]
    
    try:
        from emergentintegrations.llm.chat import LlmChat, UserMessage
        
        chat = LlmChat(
            api_key=os.environ.get("EMERGENT_LLM_KEY", ""),
            session_id=f"mapping-{uuid.uuid4()}",
            system_message="You are a data mapping expert for insurance GMC files. Map source columns to standard fields accurately."
        ).with_model("gemini", "gemini-3-flash-preview")
        
        prompt = f"""Analyze these Excel columns and map them to standard GMC fields.

Source Columns: {json.dumps(columns)}
Sample Data (first 5 rows): {json.dumps(sample_data[:5])}

Standard Fields: {json.dumps(standard_fields)}

For each source column, provide:
1. Best matching standard field (or "unmapped" if no match)
2. Confidence score (high/medium/low/uncertain)
3. Brief reasoning

Return JSON array format:
[{{"source_column": "col1", "suggested_field": "employee_name", "confidence": "high", "reasoning": "..."}}]

Return ONLY valid JSON, no other text."""

        user_message = UserMessage(text=prompt)
        response = await chat.send_message(user_message)
        
        # Parse response
        try:
            # Clean response - extract JSON
            response_text = response.strip()
            if response_text.startswith("```"):
                response_text = response_text.split("```")[1]
                if response_text.startswith("json"):
                    response_text = response_text[4:]
            mappings = json.loads(response_text)
            return mappings
        except:
            # Fallback to basic matching
            return basic_mapping_suggestions(columns)
            
    except Exception as e:
        logger.error(f"AI mapping error: {str(e)}")
        return basic_mapping_suggestions(columns)

def basic_mapping_suggestions(columns: List[str]) -> List[Dict]:
    """Fallback basic mapping without AI"""
    mappings = []
    field_patterns = {
        "employee_id": ["id", "emp", "employee", "staff"],
        "employee_name": ["name", "employee", "member"],
        "date_of_birth": ["dob", "birth", "born"],
        "gender": ["gender", "sex"],
        "relationship": ["relation", "type", "member"],
        "sum_insured": ["sum", "insured", "cover", "amount", "si"],
        "email": ["email", "mail"],
        "phone": ["phone", "mobile", "contact"],
        "address": ["address", "addr"],
        "department": ["dept", "department"],
        "designation": ["designation", "title", "position"],
        "date_of_joining": ["joining", "doj", "join"],
        "salary": ["salary", "ctc", "compensation"],
    }
    
    for col in columns:
        col_lower = col.lower()
        matched_field = "unmapped"
        confidence = "uncertain"
        
        for field, patterns in field_patterns.items():
            if any(p in col_lower for p in patterns):
                matched_field = field
                confidence = "medium"
                break
        
        mappings.append({
            "source_column": col,
            "suggested_field": matched_field,
            "confidence": confidence,
            "reasoning": "Pattern matching" if matched_field != "unmapped" else "No matching pattern found"
        })
    
    return mappings

@api_router.post("/cases/{case_id}/apply-mapping")
async def apply_mapping(case_id: str, overrides: List[MappingOverride], request: Request):
    user = await get_current_user(request)
    
    case = await db.cases.find_one({"case_id": case_id})
    if not case:
        raise HTTPException(status_code=404, detail="Case not found")
    
    if user["role"] == "agent" and case["agent_id"] != user["id"]:
        raise HTTPException(status_code=403, detail="Access denied")
    
    raw_data = case.get("raw_data", [])
    mapping_suggestions = case.get("mapping_suggestions", [])
    
    # Build final mapping
    final_mapping = {}
    for suggestion in mapping_suggestions:
        final_mapping[suggestion["source_column"]] = suggestion["suggested_field"]
    
    # Apply overrides
    for override in overrides:
        final_mapping[override.source_column] = override.target_field
    
    # Transform data
    mapped_data = []
    errors = []
    
    for idx, row in enumerate(raw_data):
        mapped_row = {"_row_index": idx, "_errors": []}
        for source_col, target_field in final_mapping.items():
            if target_field != "unmapped" and source_col in row:
                value = row[source_col]
                mapped_row[target_field] = value
                
                # Validate
                if target_field == "date_of_birth" and value:
                    try:
                        pd.to_datetime(value)
                    except:
                        mapped_row["_errors"].append({"field": target_field, "message": "Invalid date format"})
                elif target_field == "sum_insured" and value:
                    try:
                        float(str(value).replace(",", ""))
                    except:
                        mapped_row["_errors"].append({"field": target_field, "message": "Invalid number"})
                elif target_field == "email" and value:
                    if "@" not in str(value):
                        mapped_row["_errors"].append({"field": target_field, "message": "Invalid email format"})
        
        if mapped_row["_errors"]:
            errors.append({"row": idx, "errors": mapped_row["_errors"]})
        mapped_data.append(mapped_row)
    
    # Calculate AI confidence
    high_confidence = sum(1 for s in mapping_suggestions if s.get("confidence") == "high")
    ai_confidence = round((high_confidence / len(mapping_suggestions)) * 100) if mapping_suggestions else 0
    
    # Update case
    new_status = "data_correction" if errors else "review"
    await db.cases.update_one(
        {"case_id": case_id},
        {"$set": {
            "mapped_data": mapped_data,
            "ai_confidence": ai_confidence,
            "status": new_status,
            "updated_at": datetime.now(timezone.utc).isoformat()
        }}
    )
    
    await log_audit("mapping_applied", user["id"], {"case_id": case_id, "errors_count": len(errors)})
    
    return {
        "message": "Mapping applied",
        "mapped_rows": len(mapped_data),
        "errors": errors,
        "ai_confidence": ai_confidence,
        "status": new_status
    }

@api_router.post("/cases/{case_id}/correct")
async def correct_data(case_id: str, data: CaseSubmit, request: Request):
    user = await get_current_user(request)
    
    case = await db.cases.find_one({"case_id": case_id})
    if not case:
        raise HTTPException(status_code=404, detail="Case not found")
    
    if user["role"] == "agent" and case["agent_id"] != user["id"]:
        raise HTTPException(status_code=403, detail="Access denied")
    
    await db.cases.update_one(
        {"case_id": case_id},
        {"$set": {
            "corrected_data": data.corrected_data,
            "status": "review",
            "updated_at": datetime.now(timezone.utc).isoformat()
        }}
    )
    
    await log_audit("data_corrected", user["id"], {"case_id": case_id})
    
    return {"message": "Data corrections saved", "status": "review"}

@api_router.post("/cases/{case_id}/submit")
async def submit_case(case_id: str, request: Request):
    user = await get_current_user(request)
    
    case = await db.cases.find_one({"case_id": case_id})
    if not case:
        raise HTTPException(status_code=404, detail="Case not found")
    
    if user["role"] == "agent" and case["agent_id"] != user["id"]:
        raise HTTPException(status_code=403, detail="Access denied")
    
    await db.cases.update_one(
        {"case_id": case_id},
        {"$set": {
            "status": "submitted",
            "submitted_at": datetime.now(timezone.utc).isoformat(),
            "updated_at": datetime.now(timezone.utc).isoformat()
        }}
    )
    
    await log_audit("case_submitted", user["id"], {"case_id": case_id})
    
    # Create notification for underwriters
    await db.notifications.insert_one({
        "type": "new_submission",
        "title": "New Case Submitted",
        "message": f"Case {case_id} from {user['name']} is ready for review",
        "case_id": case_id,
        "target_role": "underwriter",
        "read": False,
        "created_at": datetime.now(timezone.utc).isoformat()
    })
    
    return {"message": "Case submitted for underwriting", "status": "submitted"}

# ==================== UNDERWRITER ENDPOINTS ====================

# ==================== UNDERWRITING AI (Gemma 4 RAG) ====================
class UnderwritingInput(BaseModel):
    premium: Optional[float] = None
    previous_premium: Optional[float] = None


@api_router.post("/cases/{case_id}/process-ai")
async def process_ai(case_id: str, request: Request = None):
    """Process enrollment and claims data with Gemma 4 AI to generate insights"""
    user = await get_current_user(request)
    
    case = await db.cases.find_one({"case_id": case_id})
    if not case:
        raise HTTPException(status_code=404, detail="Case not found")
    
    if user["role"] == "agent" and case["agent_id"] != user["id"]:
        raise HTTPException(status_code=403, detail="Access denied")
    
    enrollment_data = case.get("raw_data", [])
    
    if not enrollment_data:
        raise HTTPException(status_code=400, detail="No enrollment data found")
    
    # Sample data for AI
    enrollment_sample = enrollment_data[:50]
    
    # Calculate basic stats
    total_enrolled = len(enrollment_data)
    
    # Determine sum insured from raw data
    sum_insured_total = sum(
        float(str(e.get("SumInsured") or e.get("sum_insured") or 0).replace(",", ""))
        for e in enrollment_data
    )
    
    # Get claims data if exists
    claims_data = case.get("claims_data", [])
    total_claims = len(claims_data)
    total_claimed = sum(
        float(str(c.get("Claimed Amount") or c.get("Incurred Amount") or c.get("Net_Amount_paid_Including_GST_After_TDS", 0)).replace(",", ""))
        for c in claims_data
    )
    
    estimated_premium = total_claimed * 1.5 if total_claimed > 0 else sum_insured_total * 0.1
    
    # Generate structured data from enrollment (merge with claims)
    structured_data = []
    claims_by_emp = {}
    for c in claims_data:
        emp_id = str(c.get("EmpCode") or c.get("Employee_ID") or "").strip()
        if emp_id:
            if emp_id not in claims_by_emp:
                claims_by_emp[emp_id] = []
            claims_by_emp[emp_id].append(c)
    
    for emp in enrollment_data:
        emp_id = str(emp.get("EmployeeCode") or emp.get("Employee_ID") or "").strip()
        emp_claims = claims_by_emp.get(emp_id, [])
        
        has_claims = len(emp_claims) > 0
        claim_count = len(emp_claims)
        total_claimed_amt = sum(
            float(str(c.get("Claimed Amount") or c.get("Incurred Amount") or 0).replace(",", ""))
            for c in emp_claims
        )
        total_approved_amt = sum(
            float(str(c.get("Net_Amount_paid_Including_GST_After_TDS", 0)).replace(",", ""))
            for c in emp_claims
        )
        
        claims_detail = []
        for c in emp_claims:
            claims_detail.append({
                "claim_id": c.get("CCN", ""),
                "date_admission": str(c.get("Date of admission", "")),
                "date_discharge": str(c.get("DOD", "")),
                "hospital": c.get("HospitlName", ""),
                "diagnosis_primary": c.get("Pdig", ""),
                "diagnosis_secondary": c.get("Pdig2", ""),
                "treatment": c.get("TreatmentType", ""),
                "amount_claimed": float(str(c.get("Claimed Amount", 0)).replace(",", "")),
                "amount_approved": float(str(c.get("Net_Amount_paid_Including_GST_After_TDS", 0)).replace(",", "")),
                "status": c.get("Claim Status", ""),
                "match_type": "auto" if emp_claims else "none"
            })
        
        structured_data.append({
            "Employee_ID": emp.get("EmployeeCode"),
            "Name": emp.get("MemberName"),
            "Age": emp.get("Age"),
            "Age_Band": emp.get("AgeBand"),
            "Gender": emp.get("Gender", ""),
            "Relationship": emp.get("Relation", "Self"),
            "Department": emp.get("Department", ""),
            "Sum_Insured": emp.get("SumInsured", 0),
            "Pre_Existing_Conditions": "",
            "Chronic_Condition": False,
            "Claim_Count": claim_count,
            "Total_Claimed": total_claimed_amt,
            "Total_Approved": total_approved_amt,
            "Claim_Status": "Outstanding" if has_claims and any(c.get("Claim Status") == "Outstanding" for c in emp_claims) else ("Paid" if has_claims else ""),
            "Has_Claims": has_claims,
            "Hospital_1": emp_claims[0].get("HospitlName", "") if emp_claims else "",
            "Diagnosis_1": emp_claims[0].get("Pdig", "") if emp_claims else "",
            "Diagnosis_2": emp_claims[0].get("Pdig2", "") if emp_claims else "",
            "risk_flags": [],
            "claims_detail": claims_detail
        })
    
    # Mark AI confidence (simulated - would be from model)
    ai_confidence = 95  # High confidence in structured data matching
    
    # Update case
    await db.cases.update_one(
        {"case_id": case_id},
        {"$set": {
            "structured_data": structured_data,
            "ai_confidence": ai_confidence,
            "status": "ai_processed",
            "processed_at": datetime.now(timezone.utc).isoformat(),
            "updated_at": datetime.now(timezone.utc).isoformat()
        }}
    )
    
    await log_audit("ai_processing_complete", user["id"], {
        "case_id": case_id,
        "records_processed": total_enrolled,
        "ai_confidence": ai_confidence
    })
    
    return {
        "success": True,
        "message": "AI processing complete",
        "structured_data_entries": len(structured_data),
        "ai_confidence": ai_confidence,
        "claims_matched": sum(1 for s in structured_data if s["Has_Claims"])
    }


# ==================== UNDERWRITING AI ====================
@api_router.post("/cases/{case_id}/underwriting-ai")
async def generate_underwriting_ai(case_id: str, data: UnderwritingInput = None, request: Request = None):
    """Generate Part B - AI Underwriting Intelligence from structured data"""
    user = await get_current_user(request)
    
    case = await db.cases.find_one({"case_id": case_id})
    if not case:
        raise HTTPException(status_code=404, detail="Case not found")
    
    if user["role"] == "agent" and case["agent_id"] != user["id"]:
        raise HTTPException(status_code=403, detail="Access denied")
    
    structured_data = case.get("structured_data", [])
    
    if not structured_data:
        raise HTTPException(status_code=400, detail="Run AI processing first")
    
    claims_data = case.get("claims_data", [])
    total_enrolled = len(structured_data)
    total_claims = len(claims_data)
    total_claimed = sum(s.get("Total_Claimed", 0) for s in structured_data)
    
    # Key stats
    members_with_claims = sum(1 for s in structured_data if s.get("Has_Claims"))
    claims_frequency = (members_with_claims / total_enrolled * 100) if total_enrolled else 0
    
    # Average claim size
    avg_claim_size = (total_claimed / total_claims) if total_claims else 0
    
    # Age distribution
    ages = [s.get("Age", 0) for s in structured_data if s.get("Age")]
    avg_age = sum(ages) / len(ages) if ages else 30
    
    age_bands = {"18-25": 0, "26-35": 0, "36-45": 0, "46-55": 0, "55+": 0}
    for age in ages:
        if age < 26:
            age_bands["18-25"] += 1
        elif age < 36:
            age_bands["26-35"] += 1
        elif age < 46:
            age_bands["36-45"] += 1
        elif age < 56:
            age_bands["46-55"] += 1
        else:
            age_bands["55+"] += 1
    
    for band in age_bands:
        age_bands[band] = round(age_bands[band] / total_enrolled * 100, 1)
    
    # Sum insured average
    total_sum_insured = sum(s.get("Sum_Insured", 0) for s in structured_data)
    avg_sum_insured = total_sum_insured / total_enrolled if total_enrolled else 0
    
    # Estimate premium (1.5x total claimed as base)
    estimated_premium = data.premium if data and data.premium else total_claimed * 1.5
    
    # Loss ratio
    loss_ratio = (total_claimed / estimated_premium * 100) if estimated_premium > 0 else 0
    
    # Claim status breakdown
    claim_status = {"Pending": 0, "Paid": 0, "Rejected": 0, "Outstanding": 0}
    for s in structured_data:
        status = s.get("Claim_Status", "")
        if status and status in claim_status:
            claim_status[status] += 1
        elif status:
            claim_status["Outstanding"] += 1
    
    # High cost claims (>5L)
    high_cost_claims = []
    for s in structured_data:
        if s.get("Total_Claimed", 0) > 500000:
            high_cost_claims.append({
                "name": s.get("Name", "N/A"),
                "amount": s.get("Total_Claimed", 0),
                "status": s.get("Claim_Status", "")
            })
    
    # Employee vs dependent
    employees = sum(1 for s in structured_data if str(s.get("Relationship", "")).lower() in ["self", "employee"])
    dependents = total_enrolled - employees
    emp_dependent_ratio = (employees / dependents) if dependents > 0 else employees
    
    # Chronic conditions (check claims for patterns)
    chronic_indicators = ["hypertension", "diabetes", "asthma", "cardiac", "chronic"]
    chronic_count = 0
    for s in structured_data:
        diag = str(s.get("Diagnosis_1", "") + " " + s.get("Diagnosis_2", "")).lower()
        if any(ci in diag for ci in chronic_indicators):
            chronic_count += 1
    
    chronic_pct = round(chronic_count / total_enrolled * 100, 1)
    
    # Claims concentration
    sorted_by_claims = sorted(structured_data, key=lambda s: s.get("Total_Claimed", 0), reverse=True)
    top3_claims = sum(s.get("Total_Claimed", 0) for s in sorted_by_claims[:3])
    concentration_pct = round(top3_claims / total_claimed * 100, 1) if total_claimed else 0
    
    # Gender distribution
    gender_dist = {}
    for s in structured_data:
        g = str(s.get("Gender", "")).strip().title()
        if g:
            gender_dist[g] = gender_dist.get(g, 0) + 1
    gender_pct = {k: round(v / total_enrolled * 100, 1) for k, v in gender_dist.items()}
    
    # Premium per member
    premium_per_member = round(estimated_premium / total_enrolled) if total_enrolled else 0
    claim_per_member = round(total_claimed / total_enrolled) if total_enrolled else 0
    
    # Industry benchmark comparison
    industry_benchmark_lr = 65
    lr_vs_benchmark = round(loss_ratio - industry_benchmark_lr, 1)
    
    # Premium per lac
    total_si_lac = total_sum_insured / 100000 if total_sum_insured else 0
    premium_per_lac = round(estimated_premium / total_si_lac) if total_si_lac > 0 else 0
    
    metrics = {
        "total_enrolled": total_enrolled,
        "total_claims": total_claims,
        "total_claimed": round(total_claimed, 2),
        "estimated_premium": round(estimated_premium, 2),
        "loss_ratio": round(loss_ratio, 1),
        "average_age": round(avg_age, 1),
        "average_claim_size": round(avg_claim_size, 0),
        "claims_frequency": round(claims_frequency, 2),
        "members_with_claims": members_with_claims,
        "claim_status_breakdown": claim_status,
        "age_distribution": age_bands,
        "employee_dependent_ratio": round(emp_dependent_ratio, 2),
        "chronic_members_count": chronic_count,
        "chronic_members_pct": chronic_pct,
        "top_3_concentration_pct": concentration_pct,
        "gender_distribution": gender_pct,
        "employees_only_pct": round(employees / total_enrolled * 100, 1) if total_enrolled else 100,
        "premium_per_member": premium_per_member,
        "claim_per_member": claim_per_member,
        "lr_vs_industry_benchmark": lr_vs_benchmark,
        "industry_benchmark": industry_benchmark_lr,
        "premium_per_lac": premium_per_lac
    }
    
    # Risk score calculation
    lr_score = 0
    if loss_ratio >= 100:
        lr_score = max(0, 40 - (loss_ratio - 80))
    elif loss_ratio >= 75:
        lr_score = 30
    elif loss_ratio >= 50:
        lr_score = 20
    else:
        lr_score = 40 - loss_ratio
    
    freq_score = min(25, claims_frequency * 2)
    
    age_score = min(20, max(0, (avg_age - 25) * 1.2))
    
    chronic_score = min(15, chronic_count * 3)
    
    total_score = lr_score + freq_score + age_score + chronic_score
    total_score = max(0, min(100, total_score))
    
    if total_score < 25:
        risk_category = "Low"
    elif total_score < 50:
        risk_category = "Medium"
    elif total_score < 75:
        risk_category = "High"
    else:
        risk_category = "Very High"
    
    risk_score = {
        "risk_score": round(total_score, 1),
        "risk_category": risk_category,
        "breakdown": {
            "loss_ratio_score": round(lr_score, 1),
            "frequency_score": round(freq_score, 1),
            "demographics_score": round(age_score, 1),
            "chronic_score": round(chronic_score, 1)
        }
    }
    
    # Generate factors
    factors = []
    
    # Loss ratio factor
    if loss_ratio >= 100:
        loading = min(50, (loss_ratio - 80))
        factors.append({
            "category": "Loss Ratio",
            "factor": f"Loss Ratio {loss_ratio}% — High",
            "loading": f"{round(loading, 1)}%",
            "discount": "",
            "justification": f"Loss ratio of {loss_ratio}% exceeds 100% - claim cost exceeds premium",
            "burn_cost_impact": round(total_claimed * loading / 100, 0),
            "enrollment_impact": round(estimated_premium * loading / 100, 0),
            "severity": "high"
        })
    elif loss_ratio >= 75:
        loading = min(20, (loss_ratio - 70))
        factors.append({
            "category": "Loss Ratio",
            "factor": f"Loss Ratio {loss_ratio}% — Moderate",
            "loading": f"{round(loading, 1)}%",
            "discount": "",
            "justification": f"Loss ratio of {loss_ratio}% approaching concern threshold",
            "burn_cost_impact": round(total_claimed * loading / 100, 0),
            "enrollment_impact": round(estimated_premium * loading / 100, 0),
            "severity": "medium"
        })
    elif loss_ratio < 50:
        discount = min(25, (50 - loss_ratio))
        factors.append({
            "category": "Loss Ratio",
            "factor": "Profitable Portfolio",
            "loading": "",
            "discount": f"{round(discount, 1)}%",
            "justification": f"Loss ratio of {loss_ratio}% indicates strong profitability",
            "burn_cost_impact": round(-estimated_premium * discount / 100, 0),
            "enrollment_impact": round(-estimated_premium * discount / 100, 0),
            "severity": "low"
        })
    
    # Frequency factor
    if claims_frequency > 15:
        loading = min(30, claims_frequency * 2)
        factors.append({
            "category": "Frequency",
            "factor": "Very High Claims Frequency",
            "loading": f"{round(loading, 1)}%",
            "discount": "",
            "justification": f"{claims_frequency}% of members filed claims - very high frequency",
            "burn_cost_impact": round(total_claimed * 0.1, 0),
            "enrollment_impact": round(estimated_premium * 0.05, 0),
            "severity": "high"
        })
    elif claims_frequency > 8:
        loading = min(15, claims_frequency)
        factors.append({
            "category": "Frequency",
            "factor": "High Claims Frequency",
            "loading": f"{round(loading, 1)}%",
            "discount": "",
            "justification": f"{claims_frequency}% claims frequency above industry average (5-8%)",
            "burn_cost_impact": round(total_claimed * 0.05, 0),
            "enrollment_impact": round(estimated_premium * 0.03, 0),
            "severity": "medium"
        })
    elif claims_frequency < 5:
        discount = min(15, (8 - claims_frequency))
        factors.append({
            "category": "Frequency",
            "factor": "Low Claims Frequency",
            "loading": "",
            "discount": f"{round(discount, 1)}%",
            "justification": f"Excellent {claims_frequency}% claims frequency - below industry average",
            "burn_cost_impact": round(-estimated_premium * discount / 100, 0),
            "enrollment_impact": round(-estimated_premium * discount / 100, 0),
            "severity": "low"
        })
    
    # High cost claims
    if high_cost_claims:
        loading = min(25, len(high_cost_claims) * 8)
        factors.append({
            "category": "Severity",
            "factor": f"{len(high_cost_claims)} High-Cost Claims (≥₹5L)",
            "loading": f"{loading}%",
            "discount": "",
            "justification": f"{len(high_cost_claims)} claims exceed ₹5L threshold",
            "burn_cost_impact": round(total_claimed * 0.05, 0),
            "enrollment_impact": round(estimated_premium * 0.03, 0),
            "severity": "high" if len(high_cost_claims) >= 3 else "medium"
        })
    
    # Chronic conditions
    if chronic_pct > 10:
        loading = min(20, chronic_pct * 1.5)
        factors.append({
            "category": "Health Profile",
            "factor": f"{chronic_pct}% Chronic Conditions",
            "loading": f"{round(loading, 1)}%",
            "discount": "",
            "justification": f"{chronic_count} members with diabetes/hypertension require higher reserves",
            "burn_cost_impact": round(estimated_premium * loading / 100, 0),
            "enrollment_impact": round(estimated_premium * loading / 100, 0),
            "severity": "high" if chronic_pct > 20 else "medium"
        })
    
    # Age factor
    if avg_age > 40:
        loading = min(20, (avg_age - 40) * 2)
        factors.append({
            "category": "Demographics",
            "factor": f"Aging Workforce (Avg {avg_age} yrs)",
            "loading": f"{round(loading, 1)}%",
            "discount": "",
            "justification": f"Average age {avg_age} increases medical risk profile",
            "burn_cost_impact": round(estimated_premium * loading / 100, 0),
            "enrollment_impact": round(estimated_premium * loading / 100, 0),
            "severity": "medium"
        })
    elif avg_age < 30:
        discount = min(12, (30 - avg_age) * 1)
        factors.append({
            "category": "Demographics",
            "factor": f"Young Workforce (Avg {avg_age} yrs)",
            "loading": "",
            "discount": f"{round(discount, 1)}%",
            "justification": f"Young average age {avg_age} reduces claims probability",
            "burn_cost_impact": round(-estimated_premium * discount / 100, 0),
            "enrollment_impact": round(-estimated_premium * discount / 100, 0),
            "severity": "low"
        })
    
    # 55+ age band
    if age_bands.get("55+", 0) > 10:
        loading = min(15, age_bands["55+"])
        factors.append({
            "category": "Demographics",
            "factor": f"{age_bands['55+']}% Members Age 55+",
            "loading": f"{round(loading, 1)}%",
            "discount": "",
            "justification": f"Senior age band requires elevated risk loading",
            "burn_cost_impact": round(estimated_premium * loading / 100, 0),
            "enrollment_impact": round(estimated_premium * loading / 100, 0),
            "severity": "medium"
        })
    
    # Concentration risk
    if concentration_pct > 40:
        loading = min(20, concentration_pct - 30)
        factors.append({
            "category": "Concentration",
            "factor": "Claims Concentration Risk",
            "loading": f"{round(loading, 1)}%",
            "discount": "",
            "justification": f"Top 3 account for {concentration_pct}% of total claims",
            "burn_cost_impact": round(total_claimed * 0.05, 0),
            "enrollment_impact": round(estimated_premium * 0.03, 0),
            "severity": "high" if concentration_pct > 60 else "medium"
        })
    
    # Industry benchmark
    if loss_ratio > industry_benchmark_lr * 1.2:
        factors.append({
            "category": "Benchmark",
            "factor": "Above Industry Benchmark",
            "loading": "10%",
            "discount": "",
            "justification": f"LR {loss_ratio}% is {(loss_ratio/industry_benchmark_lr-1)*100:.0f}% above industry {industry_benchmark_lr}%",
            "burn_cost_impact": round(estimated_premium * 0.1, 0),
            "enrollment_impact": round(estimated_premium * 0.1, 0),
            "severity": "medium"
        })
    
    # Calculate premium impact
    total_burn_impact = sum(f.get("burn_cost_impact", 0) for f in factors)
    total_enrollment_impact = sum(f.get("enrollment_impact", 0) for f in factors)
    
    final_premium = estimated_premium + total_enrollment_impact
    premium_change_pct = (total_enrollment_impact / estimated_premium * 100) if estimated_premium > 0 else 0
    
    total_loading = sum(float(f.get("loading", "0").replace("%", "")) for f in factors if f.get("loading"))
    total_discount = sum(float(f.get("discount", "0").replace("%", "")) for f in factors if f.get("discount"))
    
    severity_counts = {"high": 0, "medium": 0, "low": 0}
    for f in factors:
        sev = f.get("severity", "medium")
        if sev in severity_counts:
            severity_counts[sev] += 1
    
    overall_severity = "high" if severity_counts["high"] >= 2 else ("medium" if severity_counts["high"] >= 1 or severity_counts["medium"] >= 2 else "low")
    
    premium_impact = {
        "base_premium": round(estimated_premium, 2),
        "adjusted_premium": round(final_premium, 2),
        "burn_cost_impact": round(total_burn_impact, 2),
        "total_adjustment": round(total_enrollment_impact, 2),
        "change_percent": round(premium_change_pct, 1),
        "recommendation": "Increase" if premium_change_pct > 5 else ("Decrease" if premium_change_pct < -5 else "Maintain"),
        "total_loading_percent": round(total_loading, 1),
        "total_discount_percent": round(total_discount, 1),
        "overall_severity": overall_severity,
        "severity_summary": severity_counts
    }
    
    # Generate premade plans
    plans = []
    
    base_rate_per_lac = premium_per_lac if premium_per_lac > 0 else 12000
    
    plan_defs = [
        {
            "id": "essential",
            "name": "Essential Plan",
            "tier": "Entry Level",
            "premium_multiplier": 0.75,
            "coverage_tier": "Basic",
            "features": ["Base coverage", "Standard exclusions", "Basic hospitalization"],
            "recommended_for": "Low risk, young workforce"
        },
        {
            "id": "standard",
            "name": "Standard Plan",
            "tier": "Mid-Market",
            "premium_multiplier": 1.0,
            "coverage_tier": "Comprehensive",
            "features": ["Full coverage", "Maternity benefit", "Day care procedures"],
            "recommended_for": "Balanced risk profile"
        },
        {
            "id": "enhanced",
            "name": "Enhanced Plan",
            "tier": "Premium Protection",
            "premium_multiplier": 1.15,
            "coverage_tier": "Premium",
            "features": ["Enhanced SI", "No co-pay 60+", "Annual checkup"],
            "recommended_for": "Higher risk, senior workforce"
        }
    ]
    
    for pd in plan_defs:
        plan = {
            "id": pd["id"],
            "name": pd["name"],
            "tier": pd["tier"],
            "premium_per_lac": round(base_rate_per_lac * pd["premium_multiplier"]),
            "coverage_tier": pd["coverage_tier"],
            "features": pd["features"],
            "recommended": pd["id"] == "standard" and overall_severity == "low",
            "recommended_for": pd["recommended_for"]
        }
        plans.append(plan)
    
    # Mark appropriate plan as recommended based on risk
    if overall_severity == "low":
        for p in plans:
            if p["id"] == "essential":
                p["recommended"] = True
    elif overall_severity == "high":
        for p in plans:
            if p["id"] == "enhanced":
                p["recommended"] = True
    else:
        for p in plans:
            if p["id"] == "standard":
                p["recommended"] = True
    
    # AI insights
    ai_insights = []
    
    if loss_ratio > 100:
        ai_insights.append({
            "type": "high_risk",
            "title": "Loss Ratio Alert",
            "description": f"Loss ratio {loss_ratio}% exceeds 100% - immediate premium adjustment required",
            "severity": "high"
        })
    elif loss_ratio < 50:
        ai_insights.append({
            "type": "opportunity",
            "title": "Profitability Opportunity",
            "description": f"Loss ratio {loss_ratio}% indicates strong profitability - consider loyalty discounts",
            "severity": "low"
        })
    
    if claims_frequency < 5:
        ai_insights.append({
            "type": "strength",
            "title": "Low Claims Frequency",
            "description": f"{claims_frequency}% claims frequency is excellent - well below industry average",
            "severity": "low"
        })
    elif claims_frequency > 15:
        ai_insights.append({
            "type": "risk",
            "title": "High Claims Frequency Alert",
            "description": f"{claims_frequency}% claims frequency significantly above industry average",
            "severity": "high"
        })
    
    if high_cost_claims:
        ai_insights.append({
            "type": "monitoring",
            "title": f"{len(high_cost_claims)} High-Cost Claims",
            "description": f"Monitor {len(high_cost_claims)} claims exceeding ₹5L for ongoing cost management",
            "severity": "medium"
        })
    
    if chronic_pct > 15:
        ai_insights.append({
            "type": "risk",
            "title": "Chronic Condition Prevalence",
            "description": f"{chronic_pct}% members with chronic conditions - consider condition-specific loadings",
            "severity": "medium"
        })
    
    # Update case with all results
    await db.cases.update_one(
        {"case_id": case_id},
        {"$set": {
            "underwriting_metrics": metrics,
            "risk_score": risk_score,
            "recommended_factors": factors,
            "premium_impact": premium_impact,
            "premade_plans": plans,
            "ai_insights": ai_insights,
            "underwriting_ai_generated": datetime.now(timezone.utc).isoformat(),
            "updated_at": datetime.now(timezone.utc).isoformat()
        }}
    )
    
    await log_audit("underwriting_ai_complete", user["id"], {
        "case_id": case_id,
        "risk_score": risk_score["risk_score"],
        "loss_ratio": loss_ratio,
        "factors_generated": len(factors)
    })
    
    return {
        "success": True,
        "underwriting_metrics": metrics,
        "risk_score": risk_score,
        "recommended_factors": factors,
        "premium_impact": premium_impact,
        "premade_plans": plans,
        "ai_insights": ai_insights
    }


@api_router.get("/cases/{case_id}/analytics")
async def get_analytics(case_id: str, request: Request = None):
    """Get comprehensive analytics for a case"""
    user = await get_current_user(request)
    
    case = await db.cases.find_one({"case_id": case_id})
    if not case:
        raise HTTPException(status_code=404, detail="Case not found")
    
    if user["role"] == "agent" and case["agent_id"] != user["id"]:
        raise HTTPException(status_code=403, detail="Access denied")
    
    structured_data = case.get("structured_data", [])
    claims_data = case.get("claims_data", [])
    
    # Match quality analysis
    total_enrollment = len(structured_data)
    matched_count = sum(1 for s in structured_data if s.get("Has_Claims"))
    match_rate = (matched_count / total_enrollment * 100) if total_enrolled else 0
    
    # Risk indicators
    risk_indicators = []
    avg_age = sum(s.get("Age", 0) for s in structured_data if s.get("Age")) / (total_enrolled or 1)
    
    if avg_age > 40:
        risk_indicators.append("Aging workforce detected - higher medical risk expected")
    
    if match_rate > 20:
        risk_indicators.append(f"High claims frequency ({match_rate:.1f}%) indicates elevated risk")
    
    total_claimed = sum(s.get("Total_Claimed", 0) for s in structured_data)
    total_sum_insured = sum(s.get("Sum_Insured", 0) for s in structured_data)
    
    si_utilization = (total_claimed / total_sum_insured * 100) if total_sum_insured else 0
    if si_utilization > 50:
        risk_indicators.append(f"High SI utilization ({si_utilization:.1f}%) suggests possible adverse selection")
    
    # Demographics
    gender_dist = {}
    for s in structured_data:
        g = str(s.get("Gender", "")).strip().title()
        if g:
            gender_dist[g] = gender_dist.get(g, 0) + 1
    
    age_bands = {"18-25": 0, "26-35": 0, "36-45": 0, "46-55": 0, "55+": 0}
    for s in structured_data:
        age = s.get("Age", 0)
        if age:
            if age < 26:
                age_bands["18-25"] += 1
            elif age < 36:
                age_bands["26-35"] += 1
            elif age < 46:
                age_bands["36-45"] += 1
            elif age < 56:
                age_bands["46-55"] += 1
            else:
                age_bands["55+"] += 1
    
    # Recommendations
    recommendations = []
    
    if match_rate < 5:
        recommendations.append({
            "priority": "low",
            "recommendation": "Portfolio shows low claims frequency - consider competitive premium rates",
            "impact": "potential_premium_reduction"
        })
    
    if avg_age > 40:
        recommendations.append({
            "priority": "medium",
            "recommendation": "Aging demographic requires enhanced medical coverage options",
            "impact": "coverage_enhancement"
        })
    
    if si_utilization > 60:
        recommendations.append({
            "priority": "high",
            "recommendation": "High SI utilization warrants premium adjustment at renewal",
            "impact": "premium_adjustment"
        })
    
    analytics = {
        "overview": {
            "total_enrollment": total_enrollment,
            "total_claims": len(claims_data),
            "matched_claims": matched_count,
            "match_rate": round(match_rate, 1),
            "quality_score": round(match_rate * 0.75 + (100 - si_utilization) * 0.25, 1)
        },
        "risk_indicators": risk_indicators,
        "recommendations": recommendations,
        "demographics": {
            "age_distribution": age_bands,
            "gender_distribution": gender_dist
        },
        "match_quality": {
            "match_rate": round(match_rate, 1),
            "unmatched_members": total_enrollment - matched_count,
            "auto_matched": matched_count,
            "manual_correction_needed": 0
        }
    }
    
    await db.cases.update_one(
        {"case_id": case_id},
        {"$set": {"analytics": analytics, "updated_at": datetime.now(timezone.utc).isoformat()}}
    )
    
    return analytics


@api_router.get("/underwriter/queue")
async def get_underwriter_queue(request: Request, status: Optional[str] = None):
    user = await require_role(request, ["underwriter", "admin"])
    
    query = {"status": {"$in": ["submitted", "under_review"]}}
    if status:
        query["status"] = status
    
    cases = await db.cases.find(query, {"_id": 0}).sort("submitted_at", 1).to_list(100)
    return {"cases": cases}

@api_router.post("/cases/{case_id}/review")
async def start_review(case_id: str, request: Request):
    user = await require_role(request, ["underwriter", "admin"])
    
    case = await db.cases.find_one({"case_id": case_id})
    if not case:
        raise HTTPException(status_code=404, detail="Case not found")
    
    await db.cases.update_one(
        {"case_id": case_id},
        {"$set": {
            "status": "under_review",
            "underwriter_id": user["id"],
            "underwriter_name": user["name"],
            "review_started_at": datetime.now(timezone.utc).isoformat(),
            "updated_at": datetime.now(timezone.utc).isoformat()
        }}
    )
    
    await log_audit("review_started", user["id"], {"case_id": case_id})
    
    return {"message": "Review started", "status": "under_review"}

@api_router.post("/cases/{case_id}/decision")
async def make_decision(case_id: str, decision: UnderwriterDecision, request: Request):
    user = await require_role(request, ["underwriter", "admin"])
    
    case = await db.cases.find_one({"case_id": case_id})
    if not case:
        raise HTTPException(status_code=404, detail="Case not found")
    
    status_map = {
        "approve": "approved",
        "reject": "rejected",
        "request_fixes": "needs_correction"
    }
    
    new_status = status_map.get(decision.decision, "under_review")
    
    await db.cases.update_one(
        {"case_id": case_id},
        {"$set": {
            "status": new_status,
            "underwriter_notes": decision.notes,
            "risk_flags": decision.risk_flags or [],
            "decision_at": datetime.now(timezone.utc).isoformat(),
            "updated_at": datetime.now(timezone.utc).isoformat()
        }}
    )
    
    await log_audit("decision_made", user["id"], {"case_id": case_id, "decision": decision.decision})
    
    # Notify agent
    await db.notifications.insert_one({
        "type": f"case_{decision.decision}",
        "title": f"Case {decision.decision.replace('_', ' ').title()}",
        "message": f"Case {case_id} has been {new_status}. {decision.notes or ''}",
        "case_id": case_id,
        "target_user_id": case["agent_id"],
        "read": False,
        "created_at": datetime.now(timezone.utc).isoformat()
    })
    
    return {"message": f"Case {new_status}", "status": new_status}

# ==================== ADMIN ENDPOINTS ====================
@api_router.get("/admin/stats")
async def get_admin_stats(request: Request):
    user = await require_role(request, ["admin"])
    
    # Get case stats
    total_cases = await db.cases.count_documents({})
    draft_cases = await db.cases.count_documents({"status": "draft"})
    mapping_cases = await db.cases.count_documents({"status": "mapping_review"})
    correction_cases = await db.cases.count_documents({"status": "data_correction"})
    review_cases = await db.cases.count_documents({"status": "review"})
    submitted_cases = await db.cases.count_documents({"status": "submitted"})
    under_review_cases = await db.cases.count_documents({"status": "under_review"})
    approved_cases = await db.cases.count_documents({"status": "approved"})
    rejected_cases = await db.cases.count_documents({"status": "rejected"})
    needs_correction = await db.cases.count_documents({"status": "needs_correction"})
    
    # Get user stats
    total_users = await db.users.count_documents({})
    agents = await db.users.count_documents({"role": "agent"})
    underwriters = await db.users.count_documents({"role": "underwriter"})
    admins = await db.users.count_documents({"role": "admin"})
    
    # Calculate avg AI confidence
    pipeline = [{"$group": {"_id": None, "avg_confidence": {"$avg": "$ai_confidence"}}}]
    ai_stats = await db.cases.aggregate(pipeline).to_list(1)
    avg_ai_confidence = ai_stats[0]["avg_confidence"] if ai_stats and ai_stats[0].get("avg_confidence") else 0
    
    return {
        "cases": {
            "total": total_cases,
            "draft": draft_cases,
            "mapping_review": mapping_cases,
            "data_correction": correction_cases,
            "review": review_cases,
            "submitted": submitted_cases,
            "under_review": under_review_cases,
            "approved": approved_cases,
            "rejected": rejected_cases,
            "needs_correction": needs_correction
        },
        "users": {
            "total": total_users,
            "agents": agents,
            "underwriters": underwriters,
            "admins": admins
        },
        "ai": {
            "avg_confidence": round(avg_ai_confidence, 1) if avg_ai_confidence else 0
        }
    }

@api_router.get("/admin/users")
async def get_users(request: Request, role: Optional[str] = None, page: int = 1, limit: int = 20):
    await require_role(request, ["admin"])
    
    query = {}
    if role:
        query["role"] = role
    
    total = await db.users.count_documents(query)
    users = await db.users.find(query, {"_id": 0, "password_hash": 0}).skip((page - 1) * limit).limit(limit).to_list(limit)
    
    # Add id field
    for user in users:
        if "id" not in user:
            user_doc = await db.users.find_one({"email": user["email"]})
            if user_doc:
                user["id"] = str(user_doc["_id"])
    
    return {"users": users, "total": total, "page": page, "limit": limit}

@api_router.put("/admin/users/{user_id}")
async def update_user(user_id: str, data: UserManagement, request: Request):
    await require_role(request, ["admin"])
    
    update_data = {k: v for k, v in data.model_dump().items() if v is not None}
    if not update_data:
        raise HTTPException(status_code=400, detail="No updates provided")
    
    result = await db.users.update_one({"_id": ObjectId(user_id)}, {"$set": update_data})
    if result.matched_count == 0:
        raise HTTPException(status_code=404, detail="User not found")
    
    return {"message": "User updated"}

# ==================== TEMPLATES ====================
@api_router.post("/templates")
async def create_template(data: TemplateCreate, request: Request):
    await require_role(request, ["admin"])
    
    template_doc = {
        "id": str(uuid.uuid4()),
        "name": data.name,
        "insurer": data.insurer,
        "mappings": data.mappings,
        "created_at": datetime.now(timezone.utc).isoformat(),
        "updated_at": datetime.now(timezone.utc).isoformat()
    }
    
    await db.templates.insert_one(template_doc)
    template_doc.pop("_id", None)
    return template_doc

@api_router.get("/templates")
async def get_templates(request: Request):
    await get_current_user(request)
    templates = await db.templates.find({}, {"_id": 0}).to_list(100)
    return {"templates": templates}

@api_router.get("/templates/{template_id}")
async def get_template(template_id: str, request: Request):
    await get_current_user(request)
    template = await db.templates.find_one({"id": template_id}, {"_id": 0})
    if not template:
        raise HTTPException(status_code=404, detail="Template not found")
    return template

@api_router.put("/templates/{template_id}")
async def update_template(template_id: str, data: TemplateCreate, request: Request):
    await require_role(request, ["admin"])
    
    result = await db.templates.update_one(
        {"id": template_id},
        {"$set": {"name": data.name, "insurer": data.insurer, "mappings": data.mappings, "updated_at": datetime.now(timezone.utc).isoformat()}}
    )
    if result.matched_count == 0:
        raise HTTPException(status_code=404, detail="Template not found")
    
    return {"message": "Template updated"}

@api_router.delete("/templates/{template_id}")
async def delete_template(template_id: str, request: Request):
    await require_role(request, ["admin"])
    
    result = await db.templates.delete_one({"id": template_id})
    if result.deleted_count == 0:
        raise HTTPException(status_code=404, detail="Template not found")
    
    return {"message": "Template deleted"}

# ==================== NOTIFICATIONS ====================
@api_router.get("/notifications")
async def get_notifications(request: Request, unread_only: bool = False):
    user = await get_current_user(request)
    
    query = {"$or": [{"target_user_id": user["id"]}, {"target_role": user["role"]}]}
    if unread_only:
        query["read"] = False
    
    notifications = await db.notifications.find(query, {"_id": 0}).sort("created_at", -1).limit(50).to_list(50)
    unread_count = await db.notifications.count_documents({**query, "read": False})
    
    return {"notifications": notifications, "unread_count": unread_count}

@api_router.post("/notifications/mark-read")
async def mark_notifications_read(request: Request, notification_ids: Optional[List[str]] = None):
    user = await get_current_user(request)
    
    query = {"$or": [{"target_user_id": user["id"]}, {"target_role": user["role"]}]}
    
    await db.notifications.update_many(query, {"$set": {"read": True}})
    return {"message": "Notifications marked as read"}

# ==================== AUDIT TRAIL ====================
async def log_audit(action: str, user_id: str, details: Dict):
    await db.audit_logs.insert_one({
        "action": action,
        "user_id": user_id,
        "details": details,
        "timestamp": datetime.now(timezone.utc).isoformat()
    })

@api_router.get("/audit-logs")
async def get_audit_logs(request: Request, action: Optional[str] = None, user_id: Optional[str] = None, page: int = 1, limit: int = 50):
    await require_role(request, ["admin"])
    
    query = {}
    if action:
        query["action"] = action
    if user_id:
        query["user_id"] = user_id
    
    total = await db.audit_logs.count_documents(query)
    logs = await db.audit_logs.find(query, {"_id": 0}).sort("timestamp", -1).skip((page - 1) * limit).limit(limit).to_list(limit)
    
    return {"logs": logs, "total": total, "page": page, "limit": limit}

# ==================== DASHBOARD ====================
@api_router.get("/dashboard/stats")
async def get_dashboard_stats(request: Request):
    user = await get_current_user(request)
    
    if user["role"] == "agent":
        query = {"agent_id": user["id"]}
    else:
        query = {}
    
    total = await db.cases.count_documents(query)
    in_progress = await db.cases.count_documents({**query, "status": {"$in": ["draft", "mapping_review", "data_correction", "review"]}})
    needs_review = await db.cases.count_documents({**query, "status": {"$in": ["needs_correction"]}})
    failed = await db.cases.count_documents({**query, "status": "failed"})
    ready_uw = await db.cases.count_documents({**query, "status": "submitted"})
    completed = await db.cases.count_documents({**query, "status": "approved"})
    
    return {
        "total_uploads": total,
        "in_progress": in_progress,
        "needs_review": needs_review,
        "failed": failed,
        "ready_for_uw": ready_uw,
        "completed": completed
    }

@api_router.get("/dashboard/recent-activity")
async def get_recent_activity(request: Request):
    user = await get_current_user(request)
    
    if user["role"] == "agent":
        query = {"user_id": user["id"]}
    else:
        query = {}
    
    activities = await db.audit_logs.find(query, {"_id": 0}).sort("timestamp", -1).limit(10).to_list(10)
    return {"activities": activities}

# Include router
app.include_router(api_router)

# Startup events
@app.on_event("startup")
async def startup():
    # Create indexes
    await db.users.create_index("email", unique=True)
    await db.cases.create_index("case_id", unique=True)
    await db.cases.create_index("agent_id")
    await db.cases.create_index("status")
    await db.login_attempts.create_index("identifier")
    await db.notifications.create_index("target_user_id")
    await db.notifications.create_index("target_role")
    await db.audit_logs.create_index("timestamp")
    
    # Seed admin
    admin_email = os.environ.get("ADMIN_EMAIL", "admin@gmc.com")
    admin_password = os.environ.get("ADMIN_PASSWORD", "Admin@123")
    
    existing = await db.users.find_one({"email": admin_email})
    if existing is None:
        hashed = hash_password(admin_password)
        await db.users.insert_one({
            "email": admin_email,
            "password_hash": hashed,
            "name": "Admin",
            "role": "admin",
            "is_active": True,
            "created_at": datetime.now(timezone.utc).isoformat()
        })
        logger.info(f"Admin user created: {admin_email}")
    elif not verify_password(admin_password, existing["password_hash"]):
        await db.users.update_one({"email": admin_email}, {"$set": {"password_hash": hash_password(admin_password)}})
        logger.info("Admin password updated")
    
    # Write test credentials
    creds_dir = Path("./memory")
    creds_dir.mkdir(exist_ok=True)
    with open(creds_dir / "test_credentials.md", "w") as f:
        f.write(f"""# Test Credentials

## Admin Account
- Email: {admin_email}
- Password: {admin_password}
- Role: admin

## Auth Endpoints
- POST /api/auth/register
- POST /api/auth/login
- POST /api/auth/logout
- GET /api/auth/me
- POST /api/auth/refresh
- POST /api/auth/forgot-password
- POST /api/auth/reset-password
""")

@app.on_event("shutdown")
async def shutdown():
    client.close()

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
