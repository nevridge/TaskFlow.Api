# Logging Guide

## Overview

TaskFlow.Api uses [Serilog](https://serilog.net/) for structured logging. Logs are written to both console and rolling file outputs, making it easy to troubleshoot issues in development, production, and containerized environments.

## Log Configuration

### Log Output Locations

By default, logs are written to:
- **Console**: All log levels according to the minimum level configuration
- **File**: Daily rolling files at the configured log path

### Default Log Paths

The log file path varies by environment:

| Environment | Default Path | Configurable Via |
|-------------|--------------|------------------|
| **Container** | `/app/logs/log.txt` | `LOG_PATH` environment variable |
| **Local Development** | `logs/log.txt` (relative to working directory) | `LOG_PATH` environment variable |

**Example**: To use a custom log path, set the `LOG_PATH` environment variable:

```bash
# Bash
export LOG_PATH=/var/log/taskflow/app.log

# PowerShell
$env:LOG_PATH = "C:\logs\taskflow\app.log"
```

### Log File Rotation

Log files rotate daily with the following naming pattern:
- Current day: `log.txt`
- Previous days: `log-yyyyMMdd.txt` (e.g., `log-20231215.txt`)

Files are flushed to disk every second to minimize log loss during crashes.

### Log Levels

Serilog supports the following log levels (from most to least severe):

1. **Fatal**: Critical errors that cause application termination
2. **Error**: Errors and exceptions that prevent normal operation
3. **Warning**: Warning messages for potentially harmful situations
4. **Information**: General informational messages about application flow
5. **Debug**: Detailed diagnostic information (disabled by default in production)
6. **Verbose**: Very detailed trace information (disabled by default)

Default minimum log level is **Information** for application code and **Warning** for Microsoft framework logs (to reduce noise).

### Configuration

Log levels can be configured in `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

## Health Check Failure Logging

Health check failures are automatically logged by Serilog to help with troubleshooting and monitoring. This section explains how to find and interpret health check logs.

### When Health Checks Are Logged

Health checks are logged whenever they return a non-healthy status:

- **Unhealthy** (HTTP 503): Logged at **Error** level
- **Degraded** (HTTP 200): Logged at **Warning** level
- **Healthy** (HTTP 200): Not logged (to avoid log noise)

### Log Entry Format

Health check failure logs include structured data for easy filtering and analysis:

**Error Level Log (Unhealthy):**
```
[Error] Health check FAILED at /health - Status: Unhealthy, Duration: 5032.12ms, Failed checks: [{"Name":"database","Description":"Database connection failed","Duration":5000.57,"Exception":"Unable to connect to database"}]
```

**Warning Level Log (Degraded):**
```
[Warning] Health check DEGRADED at /health/ready - Status: Degraded, Duration: 156.34ms, Degraded checks: [{"Name":"database","Description":"High response time","Duration":150.12}]
```

### Finding Health Check Logs

#### In Log Files

1. **Locate the log file** at the configured path (default: `/app/logs/log.txt` in containers, `logs/log.txt` locally)
2. **Search for health check entries**:

```bash
# Search for all health check failures
grep "Health check FAILED" /app/logs/log.txt

# Search for degraded health checks
grep "Health check DEGRADED" /app/logs/log.txt

# Search for health checks at a specific endpoint
grep "/health/ready" /app/logs/log.txt | grep "FAILED\|DEGRADED"

# View the last 50 lines of the log (useful for recent failures)
tail -50 /app/logs/log.txt
```

#### In Docker Logs

If running in Docker, you can view logs using Docker commands:

```bash
# View real-time logs
docker logs -f <container-name>

# View last 100 lines
docker logs --tail 100 <container-name>

# Search for health check failures
docker logs <container-name> 2>&1 | grep "Health check FAILED"
```

#### In Kubernetes

For Kubernetes deployments:

```bash
# View pod logs
kubectl logs <pod-name>

# Follow logs in real-time
kubectl logs -f <pod-name>

# Search for health check failures
kubectl logs <pod-name> | grep "Health check FAILED"
```

### Interpreting Health Check Failures

Health check logs provide detailed information to help diagnose issues:

#### Key Information in Logs

1. **Endpoint**: Which health check endpoint failed (`/health`, `/health/ready`, `/health/live`)
2. **Status**: Overall health status (Unhealthy or Degraded)
3. **Duration**: Total time taken for all health checks
4. **Failed/Degraded Checks**: Array of individual check results including:
   - **Name**: Check identifier (e.g., "database", "self")
   - **Description**: Human-readable description of the failure
   - **Duration**: Time taken for this specific check
   - **Exception**: Error message if an exception occurred

#### Example Failure Scenarios

##### Scenario 1: Database Connection Failure

**Log Entry:**
```
[2025-11-02T02:45:12.345Z] [Error] Health check FAILED at /health/ready - Status: Unhealthy, Duration: 5032.12ms, Failed checks: [{"Name":"database","Description":"Database connection failed","Duration":5000.57,"Exception":"Unable to connect to database"}]
```

**Interpretation:**
- The `/health/ready` endpoint failed (service is not ready to accept traffic)
- Database connectivity check took 5 seconds and timed out
- The database is unreachable or not responding

**Troubleshooting Steps:**
1. Verify the database service is running
2. Check database connection string in configuration
3. Verify network connectivity between the application and database
4. Check database logs for errors
5. Ensure database migrations have completed (if auto-migration is enabled)

##### Scenario 2: Application Deadlock

**Log Entry:**
```
[2025-11-02T03:15:45.123Z] [Error] Health check FAILED at /health/live - Status: Unhealthy, Duration: 30000.00ms, Failed checks: [{"Name":"self","Description":"Application is not responsive","Duration":30000.00,"Exception":"Health check timeout"}]
```

**Interpretation:**
- The `/health/live` endpoint failed (application is deadlocked or hung)
- Self-check timed out after 30 seconds
- The application process is running but not responding to requests

**Troubleshooting Steps:**
1. Check application logs for deadlock or threading issues
2. Review recent code changes that may have introduced blocking operations
3. Restart the application/container to recover
4. Consider implementing circuit breakers for external dependencies
5. Add application performance monitoring to identify slow operations

##### Scenario 3: Degraded Performance

**Log Entry:**
```
[2025-11-02T04:20:33.456Z] [Warning] Health check DEGRADED at /health - Status: Degraded, Duration: 3456.78ms, Degraded checks: [{"Name":"database","Description":"High response time","Duration":3450.12}]
```

**Interpretation:**
- Health check passed but performance is degraded
- Database queries are slow (3.45 seconds)
- Service is operational but may have performance issues

**Troubleshooting Steps:**
1. Check database query performance and indexes
2. Monitor database server resource usage (CPU, memory, disk I/O)
3. Review recent changes to database schema or queries
4. Consider scaling database resources if consistently slow
5. Check for long-running queries or locks

### Health Check Endpoints

TaskFlow.Api exposes three health check endpoints, each serving a different purpose:

| Endpoint | Purpose | Checks Included | Use Case |
|----------|---------|-----------------|----------|
| `/health` | Overall application health | Database + Self-check | General monitoring, load balancer health checks |
| `/health/ready` | Readiness for traffic | Database connectivity | Kubernetes readiness probe, load balancer registration |
| `/health/live` | Application responsiveness | Self-check only | Kubernetes liveness probe, deadlock detection |

**Important**: The `/health/live` endpoint only checks if the application is responsive and does NOT check database connectivity. This prevents restart loops caused by transient database issues.

### Configuring Log Aggregation and Alerting

For production deployments, consider integrating with log aggregation and monitoring solutions:

#### Application Insights (Azure)

Add the Serilog Application Insights sink to send logs to Azure:

```bash
dotnet add package Serilog.Sinks.ApplicationInsights
```

Configure in `Program.cs`:
```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.ApplicationInsights(
        configuration.GetValue<string>("ApplicationInsights:InstrumentationKey"),
        TelemetryConverter.Traces)
    .CreateLogger();
```

Set up alerts in Application Insights:
1. Go to Azure Portal → Application Insights → Alerts
2. Create alert rule: `traces | where message contains "Health check FAILED"`
3. Configure action group (email, SMS, webhook)

#### ELK Stack (Elasticsearch, Logstash, Kibana)

Add the Serilog Elasticsearch sink:

```bash
dotnet add package Serilog.Sinks.Elasticsearch
```

Configure in `Program.cs`:
```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://elasticsearch:9200"))
    {
        AutoRegisterTemplate = true,
        IndexFormat = "taskflow-{0:yyyy.MM.dd}"
    })
    .CreateLogger();
```

Create Kibana alerts for health check failures using index patterns.

#### Seq (Structured Log Server)

Add the Serilog Seq sink:

```bash
dotnet add package Serilog.Sinks.Seq
```

Configure in `Program.cs`:
```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Seq("http://seq:5341")
    .CreateLogger();
```

Seq provides powerful filtering and alerting capabilities with built-in support for structured Serilog events.

### Best Practices

1. **Monitor health check logs proactively**: Set up automated alerts for health check failures
2. **Correlate with metrics**: Combine health check logs with application metrics (CPU, memory, request latency)
3. **Review logs regularly**: Even if no alerts fire, periodically review logs for degraded performance patterns
4. **Adjust health check timeouts**: If health checks frequently timeout, consider increasing timeout values in probe configuration
5. **Test health checks**: Regularly test health checks by simulating failures in development/staging environments
6. **Centralize logs**: Use log aggregation to collect logs from all instances in one place
7. **Retain logs appropriately**: Configure log retention policies based on compliance and troubleshooting needs (30-90 days recommended)

## General Application Logging

### Viewing Application Logs

Logs contain general application events including:
- Application startup and shutdown
- Database migration execution
- HTTP request/response information (configurable)
- Validation errors
- Business logic errors
- Performance metrics

### Log Format

Logs use Serilog's text format with timestamps and structured properties:

```
[2025-11-02T02:46:15.123Z] [Information] Starting TaskFlow API (bootstrap logger)
[2025-11-02T02:46:16.456Z] [Information] Applying EF Core migrations on startup (Environment: "Development")
[2025-11-02T02:46:18.789Z] [Information] Starting web host on port "8080"
```

### Common Log Entries

#### Startup Logs

```
[Information] Starting TaskFlow API (bootstrap logger)
[Information] Applying EF Core migrations on startup (Environment: "Development")
[Information] Created database directory: "/app/data"
[Information] Starting web host on port "8080"
```

#### Error Logs

```
[Error] An unhandled exception occurred while processing the request
System.InvalidOperationException: Database connection failed
   at TaskFlow.Api.Data.TaskDbContext...
```

#### Fatal Logs (Application Crash)

```
[Fatal] Host terminated unexpectedly
System.Exception: Critical error
   at TaskFlow.Api.Program...
```

### Debugging Tips

1. **Increase log verbosity**: Temporarily set `MinimumLevel` to `Debug` in `appsettings.json`
2. **Enable request logging**: Add Serilog's request logging middleware for HTTP details
3. **Use structured properties**: Log with properties (`Log.Information("User {UserId} created", userId)`) for better filtering
4. **Correlation IDs**: Consider adding correlation IDs to track requests across services

## Troubleshooting Logging Issues

### Logs Not Being Written

**Symptoms**: No log files are created or logs are missing

**Possible Causes**:
1. **Insufficient permissions**: Application doesn't have write access to log directory
2. **Invalid log path**: LOG_PATH environment variable points to non-existent or inaccessible location
3. **Disk space**: No available disk space for log files

**Solutions**:
```bash
# Check directory permissions
ls -la /app/logs

# Create log directory with proper permissions
mkdir -p /app/logs
chmod 755 /app/logs

# Check disk space
df -h
```

### Logs Missing in Docker Containers

**Symptoms**: Logs not persisting after container restart

**Cause**: Log directory not mounted as a volume

**Solution**: Mount log directory as volume in `docker-compose.yml`:
```yaml
volumes:
  - ./logs:/app/logs
```

Or with Docker CLI:
```bash
docker run -v $(pwd)/logs:/app/logs ...
```

### Log Files Growing Too Large

**Symptoms**: Log files consuming excessive disk space

**Solutions**:
1. **Increase log rotation frequency**: Modify rolling interval in Serilog configuration
2. **Adjust log retention**: Delete old log files (`.txt` files older than X days)
3. **Reduce log verbosity**: Increase minimum log level to Warning or Error
4. **Filter noisy logs**: Add overrides for specific namespaces that log excessively

Example retention cleanup (cron job):
```bash
# Delete logs older than 30 days
find /app/logs -name "log-*.txt" -mtime +30 -delete
```

## Related Documentation

- [Health Check Configuration](../README.md#health-checks) - Health check endpoints and configuration
- [Docker Deployment](../README.md#docker-deployment) - Log volume configuration for containers
- [Service Registration Pattern](SERVICE_REGISTRATION_PATTERN.md) - How logging is configured in the DI container
