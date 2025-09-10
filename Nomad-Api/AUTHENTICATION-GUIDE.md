# 🔐 Authentication Guide - Nomad Surveys Multi-Tenant RBAC API

## Quick Start - Getting Your Bearer Token

### Step 1: Open Swagger UI
Navigate to: **http://localhost:5231/swagger**

### Step 2: Login to Get JWT Token

#### Option A: SuperAdmin Login
```bash
curl -X POST "http://localhost:5231/api/auth/superadmin/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "superadmin@nomadsurveys.com",
    "password": "SuperAdmin123!"
  }'
```

#### Option B: Tenant Admin Login
```bash
curl -X POST "http://localhost:5231/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@acmecorp.com",
    "password": "TenantAdmin123!",
    "tenantSlug": "acme-corp"
  }'
```

#### Option C: Participant Login
```bash
curl -X POST "http://localhost:5231/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "participant@acmecorp.com",
    "password": "Participant123!",
    "tenantSlug": "acme-corp"
  }'
```

### Step 3: Copy the JWT Token
From the response, copy the `accessToken` value:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "bbb5d6ea-1b47-44b1-9117e-00cb4cbbe521",
  "expiresAt": "2025-09-10T21:29:57.3612201Z",
  "user": { ... },
  "tenant": null
}
```

### Step 4: Authorize in Swagger
1. Click the **🔒 Authorize** button at the top of Swagger UI
2. In the "Value" field, enter: `Bearer YOUR_TOKEN_HERE`
   - Example: `Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...`
3. Click **Authorize**
4. Click **Close**

### Step 5: Test Protected Endpoints
Now you can test any protected endpoint in Swagger!

## 🧪 Testing Different User Roles

### SuperAdmin Access
- **Login**: `superadmin@nomadsurveys.com` / `SuperAdmin123!`
- **Access**: All endpoints, all tenants
- **Test Endpoints**:
  - `GET /api/tenant` - List all tenants
  - `POST /api/tenant` - Create new tenant
  - `GET /api/test/superadmin-test` - SuperAdmin test

### Tenant Admin Access  
- **Login**: `admin@acmecorp.com` / `TenantAdmin123!` (tenantSlug: `acme-corp`)
- **Access**: Full access within `acme-corp` tenant
- **Test Endpoints**:
  - `GET /acme-corp/api/users` - List tenant users
  - `POST /acme-corp/api/users` - Create tenant user
  - `GET /acme-corp/api/company` - View company info
  - `GET /api/test/tenantadmin-test` - TenantAdmin test

### Participant Access
- **Login**: `participant@acmecorp.com` / `Participant123!` (tenantSlug: `acme-corp`)
- **Access**: Limited access to assigned surveys only
- **Test Endpoints**:
  - `GET /api/test/auth-test` - Basic auth test
  - Limited survey access (when surveys are implemented)

## 🔗 API Endpoint Patterns

### Global Endpoints (No Tenant Required)
```
POST /api/auth/login               - Login (tenant users)
POST /api/auth/superadmin/login    - SuperAdmin login
GET  /api/auth/me                  - Current user info
GET  /api/test/health              - Health check
GET  /api/tenant                   - List tenants (SuperAdmin only)
POST /api/tenant                   - Create tenant (SuperAdmin only)
```

### Tenant-Scoped Endpoints
```
GET  /{tenantSlug}/api/users           - List tenant users
POST /{tenantSlug}/api/users           - Create tenant user
GET  /{tenantSlug}/api/users/{id}      - Get specific user
POST /{tenantSlug}/api/users/{id}/roles - Assign user roles
GET  /{tenantSlug}/api/company         - Get company info
PUT  /{tenantSlug}/api/company         - Update company info
```

## 🛠️ Manual Testing with cURL

### 1. Login and Get Token
```bash
# Login as SuperAdmin
curl -X POST "http://localhost:5231/api/auth/superadmin/login" \
  -H "Content-Type: application/json" \
  -d '{"email": "superadmin@nomadsurveys.com", "password": "SuperAdmin123!"}'

# Save the accessToken from response
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### 2. Test Protected Endpoints
```bash
# Test SuperAdmin endpoint
curl -X GET "http://localhost:5231/api/test/superadmin-test" \
  -H "Authorization: Bearer $TOKEN"

# Test tenant-scoped endpoint
curl -X GET "http://localhost:5231/acme-corp/api/users" \
  -H "Authorization: Bearer $TOKEN"

# Test health endpoint (no auth required)
curl -X GET "http://localhost:5231/api/test/health"
```

## 🔍 Understanding JWT Claims

When you decode your JWT token, you'll see claims like:
```json
{
  "sub": "user-id-guid",
  "email": "admin@acmecorp.com",
  "TenantId": "tenant-id-guid",
  "role": ["TenantAdmin"],
  "Permission": ["manage_users", "manage_surveys", "view_reports", "assign_surveys", "manage_company"],
  "exp": 1694123456
}
```

## 🚨 Common Issues & Solutions

### Issue: "Unauthorized" Response
- **Solution**: Ensure you're using `Bearer ` prefix in Authorization header
- **Check**: Token hasn't expired (24-hour default)
- **Verify**: Correct user credentials and tenant slug

### Issue: "Forbidden" Response  
- **Solution**: User doesn't have required permissions for this endpoint
- **Check**: User role and permissions in JWT claims
- **Verify**: Correct tenant slug in URL path

### Issue: "Tenant not found"
- **Solution**: Check tenant slug in URL path
- **Verify**: Tenant exists and is active
- **Check**: Case sensitivity of tenant slug

## 📊 Database Tables Created

Your PostgreSQL database now contains these tables:
- `Users` - User accounts with tenant association
- `Tenants` - Company/organization containers  
- `Companies` - Company information and contact details
- `Roles` - Tenant-scoped roles (SuperAdmin, TenantAdmin, Participant)
- `Permissions` - Fine-grained access control
- `UserTenantRoles` - User-role assignments within tenants
- `RolePermissions` - Role-permission mappings
- `UserRoles`, `UserClaims`, `RoleClaims` - ASP.NET Identity tables

## 🎯 Next Steps

1. **Test all endpoints** using the provided credentials
2. **Create new tenants** using SuperAdmin account
3. **Add new users** to existing tenants
4. **Explore role-based access** with different user types
5. **Build survey functionality** on top of this RBAC foundation

## 🔧 Development Tips

- Use **Postman** or **Insomnia** for advanced API testing
- Check **browser developer tools** for network requests
- Monitor **application logs** for debugging
- Use **pgAdmin** to inspect database tables and data
- **JWT.io** to decode and inspect JWT tokens
