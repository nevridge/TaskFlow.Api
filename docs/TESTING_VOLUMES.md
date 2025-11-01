# Testing Volume Mount Configuration

This document provides step-by-step instructions for testing the volume mount configuration changes to verify that logs and database files are correctly persisted to the expected locations.

## Prerequisites

- Docker Desktop installed and running
- Docker Compose v2 (or docker-compose v1.27+)
- Git (to check the changes)

## Quick Verification Test

Run this script to verify all configuration changes are in place:

```bash
#!/bin/bash
# Save as test-volume-config.sh and run: bash test-volume-config.sh

echo "=== Verifying Volume Configuration ==="

# Check Dockerfiles
grep -q "mkdir -p /app/logs /app/data" TaskFlow.Api/Dockerfile && echo "✓ Dockerfile correct" || echo "✗ Dockerfile issue"
grep -q "mkdir -p /app/logs /app/data" TaskFlow.Api/Dockerfile.dev && echo "✓ Dockerfile.dev correct" || echo "✗ Dockerfile.dev issue"

# Check appsettings
grep -q "Data Source=/app/data/tasks.db" TaskFlow.Api/appsettings.json && echo "✓ Production DB path correct" || echo "✗ Production DB path issue"
grep -q "Data Source=/app/data/tasks.dev.db" TaskFlow.Api/appsettings.Development.json && echo "✓ Dev DB path correct" || echo "✗ Dev DB path issue"

# Check Program.cs
grep -q 'LOG_PATH' TaskFlow.Api/Program.cs && echo "✓ LOG_PATH support added" || echo "✗ LOG_PATH support missing"
grep -q '/app/logs/log.txt' TaskFlow.Api/Program.cs && echo "✓ Default log path correct" || echo "✗ Default log path issue"

# Check docker-compose
grep -q "./data:/app/data" docker-compose.yml && echo "✓ Data volume mount correct" || echo "✗ Data volume mount issue"
grep -q "./logs:/app/logs" docker-compose.yml && echo "✓ Logs volume mount correct" || echo "✗ Logs volume mount issue"

echo "=== Verification Complete ==="
```

## Full Integration Test

### Test 1: Docker Compose Development Environment

1. **Clean up any existing containers and volumes:**
   ```bash
   docker compose down -v
   rm -rf ./data ./logs
   ```

2. **Build and start the containers:**
   ```bash
   docker compose up --build -d
   ```

3. **Wait for the application to start (about 10-15 seconds):**
   ```bash
   docker compose logs -f
   # Press Ctrl+C when you see "Starting web host on port 8080"
   ```

4. **Verify directories were created:**
   ```bash
   ls -la ./data ./logs
   ```
   
   Expected output:
   ```
   ./data:
   total XX
   drwxrwxrwx ... tasks.dev.db
   drwxrwxrwx ... tasks.dev.db-shm
   drwxrwxrwx ... tasks.dev.db-wal
   
   ./logs:
   total XX
   -rwxrwxrwx ... log<date>.txt
   ```

5. **Verify log file contents:**
   ```bash
   cat ./logs/log*.txt
   ```
   
   Expected: You should see Serilog startup messages including:
   - "Starting TaskFlow API (bootstrap logger)"
   - "Using connection string: Data Source=/app/data/tasks.dev.db"
   - "Created database directory: /app/data"
   - "Starting web host on port 8080"

6. **Verify database file exists and is writable:**
   ```bash
   # Check the file
   ls -lh ./data/tasks.dev.db
   
   # Verify it's a valid SQLite database
   file ./data/tasks.dev.db
   # Expected: SQLite 3.x database
   ```

7. **Test API and verify database writes:**
   ```bash
   # Create a task
   curl -X POST http://localhost:8080/api/TaskItems \
     -H "Content-Type: application/json" \
     -d '{"title":"Test Task","description":"Testing volume mounts","isCompleted":false}'
   
   # List tasks
   curl http://localhost:8080/api/TaskItems
   ```

8. **Verify the data persists across restarts:**
   ```bash
   # Stop the container
   docker compose down
   
   # Start again (without rebuilding)
   docker compose up -d
   
   # Wait a few seconds, then check tasks still exist
   curl http://localhost:8080/api/TaskItems
   # Should still show the test task created earlier
   ```

9. **Check logs were appended (not overwritten):**
   ```bash
   cat ./logs/log*.txt | grep "Starting TaskFlow API"
   # Should see multiple startup messages from different runs
   ```

10. **Clean up:**
    ```bash
    docker compose down
    # Optionally remove volumes: rm -rf ./data ./logs
    ```

### Test 2: Custom Environment Variables

1. **Create a custom docker-compose override:**
   ```bash
   cat > docker-compose.override.yml <<EOF
   services:
     taskflow-api:
       environment:
         - LOG_PATH=/app/logs/custom.log
         - ConnectionStrings__DefaultConnection=Data Source=/app/data/custom.db
   EOF
   ```

2. **Start with custom paths:**
   ```bash
   docker compose down -v
   rm -rf ./data ./logs
   docker compose up --build -d
   ```

3. **Verify custom paths are used:**
   ```bash
   # Check for custom log file
   ls ./logs/custom*.txt
   
   # Check for custom database file
   ls ./data/custom.db
   
   # Verify in logs
   docker compose logs | grep "custom"
   ```

4. **Clean up:**
   ```bash
   docker compose down
   rm docker-compose.override.yml
   ```

### Test 3: Production Dockerfile

1. **Build the production image from repository root:**
   ```bash
   cd /path/to/TaskFlow.Api
   docker build -f TaskFlow.Api/Dockerfile -t taskflow-api:test .
   ```

2. **Run with volume mounts:**
   ```bash
   # Create host directories
   mkdir -p /tmp/taskflow-prod/{data,logs}
   
   # Run the container
   docker run -d \
     -p 8080:8080 \
     -v /tmp/taskflow-prod/data:/app/data \
     -v /tmp/taskflow-prod/logs:/app/logs \
     --name taskflow-test \
     taskflow-api:test
   ```

3. **Wait for startup and verify:**
   ```bash
   # Wait for startup
   sleep 10
   
   # Check health
   curl http://localhost:8080/health
   
   # Verify files exist
   ls -la /tmp/taskflow-prod/data/
   ls -la /tmp/taskflow-prod/logs/
   
   # Check production uses correct database name
   ls /tmp/taskflow-prod/data/tasks.db
   ```

4. **Test with custom environment variables:**
   ```bash
   # Stop the test container
   docker stop taskflow-test && docker rm taskflow-test
   
   # Run with custom paths
   docker run -d \
     -p 8080:8080 \
     -e LOG_PATH=/app/logs/production.log \
     -e ConnectionStrings__DefaultConnection="Data Source=/app/data/prod.db" \
     -v /tmp/taskflow-prod/data:/app/data \
     -v /tmp/taskflow-prod/logs:/app/logs \
     --name taskflow-test \
     taskflow-api:test
   
   # Wait and verify custom files
   sleep 10
   ls /tmp/taskflow-prod/logs/production*.txt
   ls /tmp/taskflow-prod/data/prod.db
   ```

5. **Clean up:**
   ```bash
   docker stop taskflow-test && docker rm taskflow-test
   rm -rf /tmp/taskflow-prod
   docker rmi taskflow-api:test
   ```

## Troubleshooting Test Failures

### Logs directory is empty

**Possible causes:**
- Application failed to start (check `docker logs`)
- Wrong volume mount path
- Permission issues

**Debug steps:**
```bash
# Check if log file exists in container
docker exec taskflow-api ls -la /app/logs

# Check LOG_PATH environment variable
docker exec taskflow-api env | grep LOG_PATH

# Check container logs for Serilog errors
docker logs taskflow-api 2>&1 | grep -i error
```

### Database file not created

**Possible causes:**
- Migrations failed
- Wrong connection string
- Permission issues

**Debug steps:**
```bash
# Check if directory exists in container
docker exec taskflow-api ls -la /app/data

# Check connection string
docker exec taskflow-api env | grep ConnectionStrings

# Check container logs for migration errors
docker logs taskflow-api 2>&1 | grep -i "migration\|database"

# Check directory permissions
docker exec taskflow-api ls -ld /app/data
# Should show drwxrwxrwx (777)
```

### Data doesn't persist across restarts

**Possible causes:**
- Volume mount missing or incorrect
- Using wrong host path

**Debug steps:**
```bash
# Inspect volume mounts
docker inspect taskflow-api | jq '.[0].Mounts'

# Verify host directories exist
ls -la ./data ./logs

# Check if files are in the container but not on host
docker exec taskflow-api ls -la /app/data
```

## Success Criteria

All tests should pass with the following results:

✅ **Directories Created:**
- `./data/` exists on host with database file
- `./logs/` exists on host with log files

✅ **Correct Paths Used:**
- Logs written to `/app/logs/log.txt` in container
- Database at `/app/data/tasks.db` (prod) or `/app/data/tasks.dev.db` (dev)

✅ **Environment Variables Work:**
- `LOG_PATH` can override log file location
- `ConnectionStrings__DefaultConnection` can override database location

✅ **Data Persists:**
- Database content survives container restart
- Log files are appended, not overwritten

✅ **Permissions Correct:**
- Application can write to both directories
- No permission denied errors in logs

## Automated Test Results

If you ran the automated test script at the beginning, you should see:

```
✓ Dockerfile creates /app/logs and /app/data
✓ Dockerfile.dev creates /app/logs and /app/data
✓ appsettings.json uses /app/data/tasks.db
✓ appsettings.Development.json uses /app/data/tasks.dev.db
✓ Program.cs supports LOG_PATH environment variable
✓ Program.cs uses /app/logs/log.txt as default
✓ docker-compose.yml mounts ./data to /app/data
✓ docker-compose.yml mounts ./logs to /app/logs
✓ docs/volumes.md exists
✓ .gitignore includes data/ directory
✓ No references to old paths
```

All checks should pass (✓).

## Next Steps

After successful testing:

1. Document any issues found and their resolutions
2. Update this document if additional edge cases are discovered
3. Consider adding automated integration tests to CI/CD pipeline
4. Review the [volume configuration documentation](volumes.md) for deployment best practices
