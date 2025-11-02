# Logging Guide

## Overview

TaskFlow.Api uses Serilog for structured logging. Logs are written to both console and rolling file outputs.

## Log Configuration

### Log Paths

| Environment | Default Path |
|-------------|--------------|
| **Container** | `/app/logs/log.txt` |
| **Local Development** | `logs/log.txt` |

To override, set the `LOG_PATH` environment variable.

### Log Levels

Default configuration:
- Application code: **Information** level
- Microsoft framework: **Warning** level (reduced noise)

Configure in `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning"
      }
    }
  }
}
```

## Health Check Failure Logging

Health check failures are automatically logged for troubleshooting.

### Log Levels

- **Unhealthy** (HTTP 503): Error level
- **Degraded** (HTTP 200): Warning level
- **Healthy** (HTTP 200): Not logged

### Log Format

**Unhealthy:**
```
[Error] Health check FAILED at /health - Status: Unhealthy, Duration: 5032.12ms, Failed checks: [{"Name":"database","Description":"Database connection failed","Duration":5000.57,"Exception":"Unable to connect to database"}]
```

**Degraded:**
```
[Warning] Health check DEGRADED at /health/ready - Status: Degraded, Duration: 156.34ms, Degraded checks: [{"Name":"database","Description":"High response time","Duration":150.12}]
```

### Finding Logs

**Log files:** Check `/app/logs/log.txt` (containers) or `logs/log.txt` (local)

**Search examples:**
```bash
# Find failures
grep "Health check FAILED" /app/logs/log.txt

# View recent logs
tail -50 /app/logs/log.txt

# Docker
docker logs <container-name> | grep "Health check FAILED"
```

### Troubleshooting Common Issues

#### Database Connection Failure

**Log:**
```
[Error] Health check FAILED at /health/ready - Status: Unhealthy, Duration: 5032ms, Failed checks: [{"Name":"database",...}]
```

**Steps:**
1. Verify database service is running
2. Check connection string in `appsettings.json`
3. Ensure migrations completed (if `Database:MigrateOnStartup=true`)

#### Application Deadlock

**Log:**
```
[Error] Health check FAILED at /health/live - Status: Unhealthy, Duration: 30000ms, Failed checks: [{"Name":"self",...}]
```

**Steps:**
1. Check application logs for threading issues
2. Restart the application/container
3. Review recent code changes

#### Degraded Performance

**Log:**
```
[Warning] Health check DEGRADED at /health - Status: Degraded, Duration: 3456ms, Degraded checks: [{"Name":"database",...}]
```

**Steps:**
1. Check database query performance
2. Monitor resource usage (CPU, memory)
3. Review recent schema changes

### Health Check Endpoints

| Endpoint | Checks | Use Case |
|----------|--------|----------|
| `/health` | Database + Self-check | General monitoring |
| `/health/ready` | Database only | Readiness probe |
| `/health/live` | Self-check only | Liveness probe |

### Monitoring and Alerting

For production environments, integrate with log aggregation systems:
- **Application Insights**: Add `Serilog.Sinks.ApplicationInsights` package
- **ELK Stack**: Add `Serilog.Sinks.Elasticsearch` package
- **Seq**: Add `Serilog.Sinks.Seq` package

Configure alerts for health check failures by filtering on "Health check FAILED" messages.

## Application Log Events

Common log events in TaskFlow.Api:

**Startup:**
```
[Information] Starting TaskFlow API
[Information] Applying EF Core migrations on startup
[Information] Starting web host on port "8080"
```

**Errors:**
```
[Error] An unhandled exception occurred while processing the request
[Fatal] Host terminated unexpectedly
```

## Troubleshooting

### No Logs Written

Check directory permissions and disk space:
```bash
ls -la /app/logs
df -h
```

### Docker Logs Not Persisting

Mount the logs directory in `docker-compose.yml`:
```yaml
volumes:
  - ./logs:/app/logs
```
