# Volume Mount Configuration

This document describes the volume mount configuration for TaskFlow.Api, including paths, environment variables, and best practices for local development and deployment.

## Overview

TaskFlow.Api uses Docker volumes to persist data and logs across container restarts and deployments. The application follows industry-standard practices by using `/app` as the base directory for application files, with subdirectories for persistent data.

## Standard Volume Paths

### In-Container Paths

| Purpose | Path | Description |
|---------|------|-------------|
| Logs | `/app/logs/` | Application logs written by Serilog |
| Database | `/app/data/` | SQLite database files |

### Host Mounts (docker-compose)

| Host Path | Container Path | Purpose |
|-----------|----------------|---------|
| `./logs` | `/app/logs` | Persists application logs on the host |
| `./data` | `/app/data` | Persists SQLite database on the host |

## Environment Variables

### LOG_PATH

Controls the path where application logs are written.

- **Default**: `/app/logs/log.txt`
- **Format**: Full file path including filename
- **Example**: `LOG_PATH=/app/logs/application.log`

**Usage in docker-compose.yml:**
```yaml
environment:
  - LOG_PATH=/app/logs/log.txt
```

**Usage in docker run:**
```bash
docker run -e LOG_PATH=/custom/logs/app.log taskflow-api:latest
```

### ConnectionStrings__DefaultConnection

Controls the SQLite database file location.

- **Default (Production)**: `Data Source=/app/data/tasks.db`
- **Default (Development)**: `Data Source=/app/data/tasks.dev.db`
- **Format**: SQLite connection string
- **Example**: `ConnectionStrings__DefaultConnection="Data Source=/app/data/mydb.db"`

**Usage in docker-compose.yml:**
```yaml
environment:
  - ConnectionStrings__DefaultConnection=Data Source=/app/data/tasks.db
```

**Usage in docker run:**
```bash
docker run -e ConnectionStrings__DefaultConnection="Data Source=/app/data/tasks.db" taskflow-api:latest
```

## Docker Configuration

### Dockerfile

Both `Dockerfile` and `Dockerfile.dev` create the necessary directories during the build process:

```dockerfile
# Create directories for logs and data with proper permissions
RUN mkdir -p /app/logs /app/data && chmod 777 /app/logs /app/data
```

The directories are created with `777` permissions to ensure the application can write to them regardless of the user context in which the container runs. While `777` is overly permissive, it's acceptable for these specific directories in a containerized environment where:
- The container filesystem is isolated from the host
- These directories are explicitly intended for runtime data
- The application runs as a non-privileged user by default in the ASP.NET runtime image

For production deployments with stricter security requirements, consider using a non-root user and more restrictive permissions (e.g., `755` or `775`) by modifying the Dockerfile.

### docker-compose.yml

The docker-compose configuration mounts host directories to the container paths:

```yaml
volumes:
  # Mount host directories for data and logs persistence
  - ./data:/app/data
  - ./logs:/app/logs
```

This configuration:
- Creates `./data` and `./logs` directories on the host if they don't exist
- Mounts them to the corresponding paths in the container
- Persists data across container restarts and recreations

## Usage Examples

### Local Development with docker-compose

1. **Start the containers:**
   ```bash
   docker-compose up
   ```

2. **Verify volume mounts:**
   ```bash
   # Check that directories were created
   ls -la ./data ./logs
   
   # View logs on the host
   tail -f ./logs/log*.txt
   
   # Check database file
   ls -la ./data/*.db
   ```

3. **Stop the containers:**
   ```bash
   docker-compose down
   ```

The data and logs will persist in the `./data` and `./logs` directories on your host machine.

### Production Deployment with Docker CLI

1. **Build the production image:**
   ```bash
   cd TaskFlow.Api
   docker build -f Dockerfile -t taskflow-api:latest ..
   ```

2. **Run with persistent volumes:**
   ```bash
   docker run -d \
     -p 8080:8080 \
     -v $(pwd)/data:/app/data \
     -v $(pwd)/logs:/app/logs \
     --name taskflow-api \
     taskflow-api:latest
   ```

3. **Run with custom paths:**
   ```bash
   docker run -d \
     -p 8080:8080 \
     -v /var/taskflow/data:/app/data \
     -v /var/taskflow/logs:/app/logs \
     -e LOG_PATH=/app/logs/taskflow.log \
     -e ConnectionStrings__DefaultConnection="Data Source=/app/data/production.db" \
     --name taskflow-api \
     taskflow-api:latest
   ```

### Azure App Service

When deploying to Azure App Service, the `/home` directory is persistent across restarts. You have two options:

**Option 1: Use Azure persistent storage (recommended)**

Override the paths to use `/home`:
```bash
az webapp config appsettings set \
  --resource-group TaskFlowRG \
  --name taskflowapi \
  --settings \
    LOG_PATH=/home/logs/log.txt \
    ConnectionStrings__DefaultConnection="Data Source=/home/data/tasks.db"
```

**Option 2: Use default paths with volume mounts**

The default `/app/data` and `/app/logs` paths will work, but data will only persist as long as the container is running. For production, use Option 1 or configure Azure File Storage mounts.

## Best Practices

### Development

1. **Add to .gitignore**: Ensure `./data` and `./logs` directories are in `.gitignore` to avoid committing runtime artifacts:
   ```gitignore
   data/
   logs/
   ```

2. **Clean up volumes**: Remove volumes when you want to start fresh:
   ```bash
   docker-compose down -v  # Remove volumes
   rm -rf ./data ./logs    # Remove host directories
   ```

3. **Inspect running container**: Check volume mounts and paths:
   ```bash
   docker exec -it taskflow-api ls -la /app/logs /app/data
   docker exec -it taskflow-api cat /app/logs/log*.txt
   ```

### Production

1. **Use named volumes**: For production deployments, consider using named Docker volumes instead of bind mounts:
   ```yaml
   volumes:
     - taskflow-data:/app/data
     - taskflow-logs:/app/logs
   
   volumes:
     taskflow-data:
     taskflow-logs:
   ```

2. **Backup database regularly**: The SQLite database file can be backed up using various methods:
   ```bash
   # Method 1: Copy the database file from the container to the host
   docker cp taskflow-api:/app/data/tasks.db ./backups/tasks-$(date +%Y%m%d).db
   
   # Method 2: Use SQLite's backup command (backs up to a different volume)
   docker exec taskflow-api sqlite3 /app/data/tasks.db ".backup '/tmp/backup.db'"
   docker cp taskflow-api:/tmp/backup.db ./backups/tasks-$(date +%Y%m%d).db
   
   # Method 3: Create backups directly to a mounted backup volume
   # Recommended: Use the actual TaskFlow API image to ensure environment matches production
   docker run -v $(pwd)/data:/app/data -v $(pwd)/backups:/backups \
     --rm taskflow-api:latest \
     sh -c "cp /app/data/tasks.db /backups/tasks-$(date +%Y%m%d).db"
   ```

3. **Log rotation**: Serilog is configured with daily rolling logs. The current log file is named `log.txt`, and old logs are automatically archived with date suffixes in the format `log<YYYYMMDD>.txt` (e.g., the file for December 1, 2023 would be `log20231201.txt`).

4. **Monitor disk space**: Ensure adequate disk space for logs and database growth.

5. **Security**: Protect the data directory as it contains the database file. Set appropriate permissions on production hosts.

## Troubleshooting

### Logs not appearing in ./logs

**Symptom**: Container runs but no log files appear in the host `./logs` directory.

**Diagnosis:**
```bash
# Check if the directory exists in the container
docker exec taskflow-api ls -la /app/logs

# Check the LOG_PATH environment variable
docker exec taskflow-api env | grep LOG_PATH

# Check container logs for Serilog errors
docker logs taskflow-api
```

**Solutions:**
- Ensure the volume mount is correct in docker-compose.yml
- Verify the LOG_PATH environment variable is set correctly
- Check directory permissions (should be 777)

### Database not persisting

**Symptom**: Data is lost when container restarts.

**Diagnosis:**
```bash
# Check if the database file exists
docker exec taskflow-api ls -la /app/data

# Verify the connection string
docker exec taskflow-api env | grep ConnectionStrings

# Check volume mounts
docker inspect taskflow-api | grep -A 10 Mounts
```

**Solutions:**
- Ensure `./data:/app/data` volume mount is present
- Verify the connection string points to `/app/data/tasks.db`
- Check that the database file exists on the host: `ls -la ./data/`

### Permission denied errors

**Symptom**: Application crashes with "Permission denied" when writing to logs or database.

**Solutions:**
```bash
# Fix permissions on host directories
chmod -R 777 ./data ./logs

# Rebuild the image to ensure directories are created with correct permissions
docker-compose build --no-cache

# Check the user the container runs as
docker exec taskflow-api whoami
```

### Different behavior in Azure vs. local

**Symptom**: Application works locally but fails in Azure App Service.

**Solutions:**
- Azure App Service persists `/home` by default, not `/app`
- Override paths using environment variables in Azure:
  - `LOG_PATH=/home/logs/log.txt`
  - `ConnectionStrings__DefaultConnection="Data Source=/home/data/tasks.db"`
- Or mount Azure File Storage to `/app/data` and `/app/logs`

## Migration from Previous Configuration

If you're upgrading from a previous version that used `/home/logs` and `/home/tasks.db`:

### For Local Development

1. **Update to latest version:**
   ```bash
   git pull origin main
   docker-compose down
   docker-compose build --no-cache
   ```

2. **Start with new configuration:**
   ```bash
   docker-compose up
   ```

The new configuration will create `./data` and `./logs` directories automatically.

### For Azure Deployment

Azure App Service uses `/home` for persistent storage. To maintain compatibility:

1. **Set environment variables in Azure Portal:**
   - `LOG_PATH=/home/logs/log.txt`
   - `ConnectionStrings__DefaultConnection="Data Source=/home/data/tasks.db"`

2. **Or update your deployment workflow** to include these settings.

Your existing data and logs in `/home` will continue to work without migration.

## Summary

- **Default container paths**: `/app/logs` for logs, `/app/data` for database
- **Environment variables**: `LOG_PATH` and `ConnectionStrings__DefaultConnection` for customization
- **Docker Compose**: Automatically mounts `./data` and `./logs` from the host
- **Azure App Service**: Override paths to use `/home` for persistent storage
- **Best practice**: Use the defaults for local development, override for specific deployment environments
