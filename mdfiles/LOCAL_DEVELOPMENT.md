# Local Development Guide

## Two Ways to Run Locally

### Option 1: Without Docker (Hot Reload - Recommended for Development)

This is best when you're actively coding because you get instant hot reload.

**Step 1: Start the database**
```bash
docker-compose up db
```

**Step 2: Run the .NET API**
```bash
cd ApiTemplate
dotnet run
```
API runs at: http://localhost:8080

**Step 3: Run Angular dev server**
```bash
cd ApiTemplateUi
npm start
```
Angular runs at: http://localhost:4200

**Configuration:**
- [environment.ts](ApiTemplateUi/src/environments/environment.ts) is already configured for this mode
- API URL: `http://localhost:8080/api/v1`

**Workflow:**
- Make code changes → Save → Auto-reload ✅
- No rebuilding needed
- Fast development cycle

---

### Option 2: With Docker + NGINX (Production-like Setup)

This is best when you want to test the full setup including NGINX.

**Single Command:**
```bash
docker-compose up --build
```

Access everything at: http://localhost (port 80)

**Configuration:**
- Uses [environment.docker.ts](ApiTemplateUi/src/environments/environment.docker.ts)
- API URL: `http://localhost/api/v1` (proxied through NGINX)

**Workflow:**
- Make code changes → Stop containers → Rebuild → Start
- Slower iteration but tests real deployment setup

**Useful Commands:**
```bash
# Run in background
docker-compose up -d

# View logs
docker-compose logs -f

# Rebuild after changes
docker-compose up --build

# Stop everything
docker-compose down

# Clean restart (removes volumes)
docker-compose down -v
```

---

## Quick Comparison

| Feature | Without Docker | With Docker |
|---------|---------------|-------------|
| **Speed** | ⚡ Fast (hot reload) | 🐌 Slower (rebuild needed) |
| **Setup** | Need Node, .NET, SQL Server | Just Docker |
| **Use Case** | Active development | Testing deployment |
| **Ports** | 4200 (Angular), 8080 (API) | 80 (NGINX) |
| **NGINX** | ❌ Not used | ✅ Used |
| **Environment** | environment.ts | environment.docker.ts |

---

## Switching Between Modes

The environment files are automatically selected based on build configuration:

- **`npm start`** → Uses `environment.ts` (localhost:8080)
- **`npm run build -- --configuration docker`** → Uses `environment.docker.ts` (localhost/api)
- **`npm run build -- --configuration production`** → Uses `environment.prod.ts` (/api)

No manual file editing needed! 🎉

---

## Testing the NGINX Setup Locally

If you want to test NGINX features (caching, compression, security headers) without full Docker:

1. Build Angular for Docker:
   ```bash
   cd ApiTemplateUi
   npm run build -- --configuration docker
   ```

2. Run NGINX locally with Docker:
   ```bash
   docker run -d -p 80:80 \
     -v $(pwd)/dist/api-template-ui/browser:/usr/share/nginx/html \
     -v $(pwd)/nginx.conf:/etc/nginx/conf.d/default.conf \
     nginx:alpine
   ```

3. Run API and DB as usual

---

## Troubleshooting

### "Cannot connect to API" in browser
- Check if API is running: http://localhost:8080/api/v1 (or whatever your endpoint is)
- Check environment file matches your setup:
  - Without Docker: `environment.ts` should use `localhost:8080`
  - With Docker: Uses `environment.docker.ts` with `localhost/api`

### Port already in use
- **Port 80**: Change NGINX port in `docker-compose.yml`
- **Port 8080**: Kill other .NET processes or change API port
- **Port 4200**: Change Angular dev server port with `ng serve --port 4201`

### Database connection failed
- Ensure SQL Server container is running: `docker-compose ps`
- Connection string should use `Server=localhost,1433` (not `db`)

---

## Recommended Workflow

**For day-to-day development:**
```bash
# Terminal 1
docker-compose up db

# Terminal 2
cd ApiTemplate && dotnet watch run

# Terminal 3
cd ApiTemplateUi && npm start
```

**Before committing/deploying:**
```bash
# Test the full Docker setup
docker-compose up --build

# Verify everything works at http://localhost
```

This gives you the best of both worlds! 🚀
