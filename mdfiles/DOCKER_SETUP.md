# Docker Setup with NGINX

This project uses Docker Compose to orchestrate the following services:
- **NGINX**: Reverse proxy and web server
- **API**: .NET API backend
- **Database**: SQL Server

## Architecture

```
Client Browser (port 80)
        ↓
    NGINX (port 80)
        ↓
   ┌────┴────┐
   ↓         ↓
Angular    API (port 8080)
  App         ↓
          Database (port 1433)
```

## What NGINX Does

1. **Serves Angular Application**: Static files (HTML, CSS, JS) on port 80
2. **Reverse Proxy**: Routes `/api/*` requests to the .NET backend
3. **Security Headers**: Adds X-Frame-Options, X-Content-Type-Options, etc.
4. **Compression**: Gzip compression for better performance
5. **Caching**: Caches static assets with proper cache headers
6. **SPA Support**: Routes all Angular routes to index.html

## Getting Started

### Prerequisites
- Docker Desktop installed
- Docker Compose installed

### Running the Application

1. **Build and start all services**:
   ```bash
   docker-compose up --build
   ```

2. **Start in detached mode**:
   ```bash
   docker-compose up -d
   ```

3. **View logs**:
   ```bash
   docker-compose logs -f
   ```

4. **Stop all services**:
   ```bash
   docker-compose down
   ```

5. **Stop and remove volumes** (clean database):
   ```bash
   docker-compose down -v
   ```

### Accessing the Application

- **Frontend (Angular + NGINX)**: http://localhost
- **API** (internal only, via NGINX): http://localhost/api
- **Database**: localhost:1433

## File Structure

```
.
├── docker-compose.yml           # Orchestrates all services
├── ApiTemplate/
│   └── Dockerfile              # .NET API Docker image
├── ApiTemplateUi/
│   ├── Dockerfile              # Angular + NGINX Docker image
│   ├── nginx.conf              # NGINX configuration
│   └── .dockerignore           # Files to exclude from Docker build
```

## NGINX Configuration Highlights

### API Proxy
All requests to `/api/*` are proxied to the backend:
```nginx
location /api/ {
    proxy_pass http://api:8080/api/;
    # ... headers and timeouts
}
```

### Static Asset Caching
JavaScript, CSS, and images cached for 1 year:
```nginx
location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ {
    expires 1y;
    add_header Cache-Control "public, immutable";
}
```

### Security Headers
- X-Frame-Options: Prevents clickjacking
- X-Content-Type-Options: Prevents MIME sniffing
- X-XSS-Protection: Enables XSS filtering

## Development Workflow

### Local Development (without Docker)
1. Start SQL Server via Docker:
   ```bash
   docker-compose up db
   ```

2. Run .NET API locally:
   ```bash
   cd ApiTemplate
   dotnet run
   ```

3. Run Angular locally:
   ```bash
   cd ApiTemplateUi
   npm start
   ```

   Update `environment.ts` to use:
   ```typescript
   apiUrl: 'http://localhost:8080/api/v1'
   ```

### Full Docker Setup
Use `docker-compose up` to run everything together.

## Troubleshooting

### Port Conflicts
If port 80 is in use:
```yaml
# In docker-compose.yml, change nginx ports:
ports:
  - "3000:80"  # Access via http://localhost:3000
```

### Database Connection Issues
Ensure the API waits for database to be ready. The API service includes `depends_on: db`.

### NGINX 502 Bad Gateway
- Check if API container is running: `docker-compose ps`
- View API logs: `docker-compose logs api`
- Ensure API is listening on port 8080

### Rebuild After Changes
```bash
docker-compose up --build --force-recreate
```

## Production Considerations

For production deployment, consider:

1. **HTTPS/SSL**: Add SSL certificates to NGINX
2. **Environment Variables**: Use production connection strings
3. **Rate Limiting**: Add NGINX rate limiting
4. **Health Checks**: Implement health check endpoints
5. **Logging**: Configure centralized logging
6. **Monitoring**: Add application monitoring
7. **Secrets Management**: Use Docker secrets or environment variables from secure sources

## Customization

### Change NGINX Port
Edit `docker-compose.yml`:
```yaml
nginx:
  ports:
    - "8000:80"
```

### Add Rate Limiting
Edit `nginx.conf`:
```nginx
limit_req_zone $binary_remote_addr zone=api_limit:10m rate=10r/s;

location /api/ {
    limit_req zone=api_limit burst=20 nodelay;
    # ... existing config
}
```

### Add CORS Headers (if needed)
Edit `nginx.conf`:
```nginx
add_header 'Access-Control-Allow-Origin' '*' always;
add_header 'Access-Control-Allow-Methods' 'GET, POST, PUT, DELETE, OPTIONS' always;
```
