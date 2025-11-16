# NGINX Monitoring Guide

## Quick Monitoring Commands

### 1. **View Real-time Logs**

```bash
# NGINX logs only
docker-compose logs -f nginx

# All services (NGINX + API + DB)
docker-compose logs -f

# Last 50 lines
docker-compose logs --tail=50 nginx

# Filter by keyword
docker-compose logs nginx | grep "ERROR"
```

### 2. **Check Container Status**

```bash
# Check if NGINX is running
docker-compose ps

# Expected output:
# NAME           STATUS          PORTS
# nginx_proxy    Up 2 minutes    0.0.0.0:80->80/tcp
```

### 3. **NGINX Built-in Status Page**

After rebuilding, access:
```
http://localhost/nginx-status
```

**Example Output:**
```
Active connections: 5
server accepts handled requests
 127 127 456
Reading: 0 Writing: 1 Waiting: 4
```

**What it means:**
- **Active connections**: Current connections to NGINX
- **Accepts**: Total accepted connections
- **Handled**: Total handled connections
- **Requests**: Total client requests
- **Reading**: Connections reading request
- **Writing**: Connections writing response
- **Waiting**: Idle keepalive connections

### 4. **Access NGINX Container**

```bash
# Enter the container
docker exec -it nginx_proxy sh

# Once inside, you can run:
nginx -t              # Test config syntax
nginx -s reload       # Reload config
ps aux               # View processes
cat /etc/nginx/conf.d/default.conf  # View config
exit                 # Leave container
```

### 5. **Monitor Resource Usage**

```bash
# Real-time stats (CPU, Memory, Network)
docker stats nginx_proxy

# One-time snapshot
docker stats --no-stream nginx_proxy
```

**Example Output:**
```
CONTAINER      CPU %     MEM USAGE / LIMIT     NET I/O
nginx_proxy    0.01%     5.5MiB / 7.775GiB    1.2kB / 856B
```

---

## Log Files Inside Container

```bash
# Access log (all requests)
docker exec nginx_proxy cat /var/log/nginx/access.log

# Error log
docker exec nginx_proxy cat /var/log/nginx/error.log

# Follow access log in real-time
docker exec nginx_proxy tail -f /var/log/nginx/access.log
```

---

## Advanced Monitoring

### Enable Detailed Logging

Edit [nginx.conf](ApiTemplateUi/nginx.conf) and add custom log format:

```nginx
log_format detailed '$remote_addr - $remote_user [$time_local] '
                    '"$request" $status $body_bytes_sent '
                    '"$http_referer" "$http_user_agent" '
                    'rt=$request_time uct="$upstream_connect_time" '
                    'uht="$upstream_header_time" urt="$upstream_response_time"';

access_log /var/log/nginx/access.log detailed;
```

This shows:
- Response time
- Upstream (API) connection time
- Detailed request info

### Monitor Specific Endpoints

```bash
# Count requests to API
docker exec nginx_proxy cat /var/log/nginx/access.log | grep "/api/" | wc -l

# Show 404 errors
docker exec nginx_proxy cat /var/log/nginx/access.log | grep " 404 "

# Show slow requests (if using detailed logging)
docker exec nginx_proxy cat /var/log/nginx/access.log | grep "rt=" | awk '$NF>1'
```

---

## Third-Party Monitoring Tools

### Option 1: NGINX Amplify (Free)

1. Sign up at https://amplify.nginx.com
2. Add monitoring agent to your Dockerfile
3. Get real-time metrics, alerts, and dashboards

### Option 2: Prometheus + Grafana

Install nginx-prometheus-exporter:

```yaml
# Add to docker-compose.yml
nginx-exporter:
  image: nginx/nginx-prometheus-exporter:latest
  container_name: nginx_exporter
  command:
    - '-nginx.scrape-uri=http://nginx/nginx-status'
  ports:
    - "9113:9113"
  networks:
    - app-network
  depends_on:
    - nginx
```

### Option 3: ELK Stack (Elasticsearch, Logstash, Kibana)

For production-level logging and analysis.

---

## Health Check Script

Create a simple health check:

```bash
# Check if NGINX is responding
curl -f http://localhost/nginx-status || echo "NGINX is down!"

# Check API through NGINX
curl -f http://localhost/api/v1/health || echo "API is down!"
```

---

## Common Monitoring Scenarios

### Scenario 1: Check if NGINX is working
```bash
docker-compose ps nginx
curl http://localhost/nginx-status
```

### Scenario 2: Debug 502 Bad Gateway
```bash
# Check API container
docker-compose ps api

# View NGINX error logs
docker-compose logs nginx | grep "error"

# Check if API is reachable from NGINX
docker exec nginx_proxy wget -O- http://api:8080/api/v1
```

### Scenario 3: Monitor traffic
```bash
# Watch requests in real-time
docker-compose logs -f nginx | grep "GET\|POST\|PUT\|DELETE"

# Count requests per minute
docker exec nginx_proxy tail -f /var/log/nginx/access.log | pv -l -i 60 > /dev/null
```

### Scenario 4: Check performance
```bash
# See resource usage
docker stats nginx_proxy

# Check active connections
curl http://localhost/nginx-status

# Load test with Apache Bench
ab -n 1000 -c 10 http://localhost/
```

---

## Alerts and Notifications

### Basic Health Check (Windows PowerShell)

```powershell
# Save as monitor-nginx.ps1
while ($true) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost/nginx-status" -UseBasicParsing
        if ($response.StatusCode -ne 200) {
            Write-Host "ALERT: NGINX not responding!" -ForegroundColor Red
        } else {
            Write-Host "OK: NGINX is healthy" -ForegroundColor Green
        }
    } catch {
        Write-Host "ERROR: $_" -ForegroundColor Red
    }
    Start-Sleep -Seconds 30
}
```

Run: `.\monitor-nginx.ps1`

---

## Troubleshooting Commands

```bash
# Restart NGINX container
docker-compose restart nginx

# Rebuild NGINX
docker-compose up -d --build nginx

# View full container configuration
docker inspect nginx_proxy

# Check network connectivity
docker exec nginx_proxy ping api
docker exec nginx_proxy nslookup api

# Validate NGINX config without restarting
docker exec nginx_proxy nginx -t
```

---

## Performance Metrics to Watch

| Metric | Command | Good Value |
|--------|---------|------------|
| CPU Usage | `docker stats nginx_proxy` | < 10% |
| Memory | `docker stats nginx_proxy` | < 50MB |
| Active Connections | `curl localhost/nginx-status` | Depends on traffic |
| Response Time | Check logs with detailed format | < 200ms |
| Error Rate | `grep " 5[0-9][0-9] " access.log` | < 1% |

---

## Production Monitoring Checklist

- [ ] Enable NGINX stub_status (✅ Already done!)
- [ ] Set up log rotation
- [ ] Configure alerts for errors
- [ ] Monitor disk space for logs
- [ ] Set up uptime monitoring (Pingdom, UptimeRobot)
- [ ] Configure Prometheus/Grafana for metrics
- [ ] Set up log aggregation (ELK, Splunk)
- [ ] Monitor SSL certificate expiration
- [ ] Track response times
- [ ] Set up automated health checks

---

## Quick Reference

```bash
# Status
docker-compose ps nginx
curl http://localhost/nginx-status

# Logs
docker-compose logs -f nginx

# Resources
docker stats nginx_proxy

# Shell access
docker exec -it nginx_proxy sh

# Restart
docker-compose restart nginx

# Rebuild
docker-compose up -d --build nginx
```

Happy monitoring! 📊
