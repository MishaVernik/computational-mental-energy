# Troubleshooting Guide

Common issues and solutions for the CME Simulation System.

## Issue: "dependency failed to start: container cme-sqlserver is unhealthy"

### Cause
SQL Server 2022 uses a newer version of `sqlcmd` (`mssql-tools18`) and requires the `-C` flag to trust the server certificate.

### Solution
This has been fixed in the `docker-compose.yml`. The health check now uses:
```yaml
test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd -Q 'SELECT 1' -C || exit 1"]
```

If you still see this error after pulling the latest code:
```bash
docker-compose down -v  # Remove volumes to start fresh
docker-compose up -d
```

## Issue: Database tables not created

### Cause
If you stopped/restarted containers multiple times during initial setup, the database may have been created but without the schema.

### Solution
```bash
# Stop all services
docker-compose down

# Remove the SQL Server volume
docker volume rm lab45_sqlserver_data

# Start fresh
docker-compose up -d
```

Or manually drop and recreate:
```bash
docker-compose stop api
docker exec cme-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -C -Q "ALTER DATABASE CmeSimDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE CmeSimDb;"
docker-compose start api
```

## Issue: API shows "Invalid object name 'InferenceRequestLogs'"

### Cause
Database exists but tables weren't created.

### Solution
Same as "Database tables not created" above.

## Issue: Quantum backend not responding

### Symptoms
- API returns 503 errors
- Logs show "Quantum backend is unavailable"

### Solution
```bash
# Check quantum backend logs
docker logs cme-qbackend

# Restart quantum backend
docker-compose restart qbackend

# Verify it's healthy
curl http://localhost:8001/health
```

## Issue: Port already in use

### Symptoms
```
Error: bind: address already in use
```

### Solution
Change ports in `docker-compose.yml`:
```yaml
ports:
  - "5001:5000"  # Change 5001 to any available port
```

Or stop the conflicting service:
```powershell
# Find process using port 5000
netstat -ano | findstr :5000

# Kill the process (replace PID with actual process ID)
taskkill /PID <PID> /F
```

## Issue: Simulation client can't connect to API

### Symptoms
```
Error: API error: ECONNREFUSED
```

### Solution
1. Verify API is running:
```bash
docker-compose ps
curl http://localhost:5000/api/dashboard/summary
```

2. Check if you're using the correct URL:
```bash
npm run simulate -- --url http://localhost:5000
```

3. If running API on a different port, update the URL accordingly.

## Issue: High latency (>5 seconds)

### Cause
This is expected! The quantum backend simulates realistic QPU delays (300-2000ms).

### Solution
To reduce latency for testing, edit `docker-compose.yml`:
```yaml
qbackend:
  environment:
    - QPU_LATENCY_MIN_MS=100
    - QPU_LATENCY_MAX_MS=500
```

Then restart:
```bash
docker-compose restart qbackend
```

## Issue: Training jobs stuck in "Queued" status

### Cause
Training worker service may not be running or encountering errors.

### Solution
```bash
# Check API logs for training worker errors
docker logs cme-api 2>&1 | findstr /C:"Training" /C:"training"

# Restart API
docker-compose restart api
```

## Issue: Docker Compose warnings about 'version' attribute

### Symptoms
```
level=warning msg="the attribute `version` is obsolete"
```

### Solution
This is just a warning and can be safely ignored. Docker Compose v2 no longer requires the `version` field, but it's kept for backward compatibility.

To remove the warning, delete the first line from `docker-compose.yml`:
```yaml
version: '3.8'  # Delete this line
```

## Issue: Cannot run dotnet ef commands

### Cause
EF Core tools not installed globally.

### Solution
```bash
# Install EF Core tools
dotnet tool install --global dotnet-ef

# Or use as local tool
cd CmeSim.Api
dotnet tool install dotnet-ef
```

## Checking Service Health

### SQL Server
```bash
docker exec cme-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -C -Q "SELECT @@VERSION"
```

### Quantum Backend
```bash
curl http://localhost:8001/health
```

### API
```bash
curl http://localhost:5000/api/dashboard/summary
```

## Complete Reset

To start completely fresh:

```bash
# Stop and remove everything
docker-compose down -v

# Remove all images (optional)
docker rmi lab45-api lab45-qbackend

# Start fresh
docker-compose up -d --build

# Wait for initialization
timeout /t 30 /nobreak

# Verify
docker-compose ps
curl http://localhost:5000/api/dashboard/summary
```

## Getting Help

If you encounter issues not covered here:

1. Check container logs:
```bash
docker logs cme-api
docker logs cme-qbackend
docker logs cme-sqlserver
```

2. Check service status:
```bash
docker-compose ps
```

3. Verify network connectivity:
```bash
docker network inspect lab45_cme-network
```

4. Check database tables:
```bash
docker exec cme-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -C -d CmeSimDb -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'"
```

## Common Commands

```bash
# View logs in real-time
docker-compose logs -f

# Restart a single service
docker-compose restart api

# Rebuild and restart
docker-compose up -d --build

# Stop without removing volumes
docker-compose stop

# Stop and remove volumes
docker-compose down -v

# Check container resource usage
docker stats
```


