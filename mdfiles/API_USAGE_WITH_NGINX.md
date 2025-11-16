# How to Call Your API with NGINX

## The Problem
When using NGINX as a reverse proxy, you need to ensure:
1. All services are running (NGINX, API, Database)
2. You're using the correct URL format
3. The API container is accessible from NGINX

## Quick Fix Steps

### 1. Check if all containers are running
```bash
docker-compose ps
```

**Expected output:**
```
NAME                 STATUS          PORTS
nginx_proxy          Up              0.0.0.0:80->80/tcp
apitemplate_api_v2   Up              8080/tcp, 8081/tcp
sqlserver_db_v2      Up              0.0.0.0:1433->1433/tcp
```

If API is not running, check logs:
```bash
docker-compose logs api
```

### 2. Restart all services
```bash
docker-compose down
docker-compose up --build
```

### 3. Test API connectivity

**From NGINX container to API:**
```bash
# Test if NGINX can reach API
docker exec nginx_proxy wget -O- http://api:8080/api/v1
```

**From your browser/Postman:**
```
http://localhost/api/v1/Auth/login
```

---

## API URL Formats

### When using Docker + NGINX (Port 80)

**✅ CORRECT:**
- `http://localhost/api/v1/Auth/login`
- `http://localhost/api/v1/Users`
- `http://localhost/api/v1/...`

**❌ WRONG:**
- `http://localhost:8080/api/v1/...` (API port not exposed externally)
- `http://localhost:80/api/v1/...` (don't need to specify port 80)

### When using Local Development (No Docker)

**✅ CORRECT:**
- `http://localhost:8080/api/v1/Auth/login`
- `http://localhost:8080/api/v1/Users`

---

## How NGINX Proxying Works

```
Your Request:
http://localhost/api/v1/Auth/login
        ↓
NGINX receives on port 80
        ↓
NGINX matches location /api/
        ↓
NGINX forwards to: http://api:8080/api/v1/Auth/login
        ↓
.NET API processes the request
        ↓
Response flows back through NGINX
        ↓
You receive the response
```

---

## Common Issues & Solutions

### Issue 1: "400 Bad Request" from NGINX

**Cause:** API container not running or not accessible

**Solution:**
```bash
# Check API container
docker-compose ps api

# View API logs
docker-compose logs api

# Restart API
docker-compose restart api
```

### Issue 2: "502 Bad Gateway"

**Cause:** NGINX can't connect to API backend

**Solution:**
```bash
# Test connection from NGINX to API
docker exec nginx_proxy ping api

# Check if API is listening
docker exec apitemplate_api_v2 netstat -tlnp

# Verify network
docker network inspect dotnet-api-template_app-network
```

### Issue 3: "Connection Refused"

**Cause:** API not started or crashed

**Solution:**
```bash
# Check API logs for errors
docker-compose logs api

# Check database connection
docker-compose logs db

# Restart everything
docker-compose restart
```

### Issue 4: "CORS Error"

**Cause:** API CORS settings don't allow requests

**Check your API CORS configuration** in [Program.cs](ApiTemplate/Program.cs) or startup configuration.

---

## Testing the Setup

### 1. Health Check
```bash
# Check NGINX
curl http://localhost/nginx-status

# Check API (should work if API has a health endpoint)
curl http://localhost/api/v1/health
```

### 2. Login Test (Postman/cURL)
```bash
curl -X POST http://localhost/api/v1/Auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "jafarabbas",
    "password": "12345"
  }'
```

### 3. From Angular App
Your Angular app should use the environment configuration:

**For Docker (environment.docker.ts):**
```typescript
apiUrl: 'http://localhost/api/v1'
```

**For Local Dev (environment.ts):**
```typescript
apiUrl: 'http://localhost:8080/api/v1'
```

---

## Debugging Commands

```bash
# 1. Check all containers
docker-compose ps

# 2. View all logs
docker-compose logs -f

# 3. View API logs only
docker-compose logs -f api

# 4. View NGINX logs
docker-compose logs -f nginx

# 5. Test API from NGINX container
docker exec nginx_proxy wget -O- http://api:8080/api/v1/Auth/login

# 6. Enter API container
docker exec -it apitemplate_api_v2 sh

# 7. Check NGINX config syntax
docker exec nginx_proxy nginx -t

# 8. Check network
docker network inspect dotnet-api-template_app-network
```

---

## Example API Calls

### Login
```http
POST http://localhost/api/v1/Auth/login
Content-Type: application/json

{
  "username": "jafarabbas",
  "password": "12345"
}
```

### With Bearer Token
```http
GET http://localhost/api/v1/Users
Authorization: Bearer YOUR_JWT_TOKEN
```

### Refresh Token
```http
POST http://localhost/api/v1/Auth/refresh
Content-Type: application/json

{
  "refreshToken": "your-refresh-token-here"
}
```

---

## Postman Configuration

**Base URL Variable:**
- Create environment variable: `baseUrl`
- Docker value: `http://localhost`
- Local value: `http://localhost:8080`

**Sample Request:**
```
POST {{baseUrl}}/api/v1/Auth/login
```

This way you can easily switch between Docker and local development!

---

## Production Considerations

When deploying to production:

1. **Use HTTPS:** Configure SSL certificates in NGINX
2. **Update API URL:** Use your domain name
3. **Environment Variables:** Set production connection strings
4. **Remove Exposed Ports:** Don't expose database port 1433
5. **Security:** Enable NGINX rate limiting

---

## Quick Reference

| Environment | API Base URL | What's Running |
|-------------|--------------|----------------|
| **Docker** | `http://localhost/api/v1` | NGINX + API + DB in containers |
| **Local Dev** | `http://localhost:8080/api/v1` | API locally, DB in container |
| **Production** | `https://yourdomain.com/api/v1` | All services with HTTPS |

---

## Need Help?

1. Check logs: `docker-compose logs -f`
2. Verify containers: `docker-compose ps`
3. Test connectivity: `docker exec nginx_proxy ping api`
4. Restart services: `docker-compose restart`
5. Clean restart: `docker-compose down && docker-compose up --build`
