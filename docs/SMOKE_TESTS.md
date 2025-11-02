# Smoke Tests

This document describes the smoke/integration tests that run as part of the CI pipeline.

## Overview

The smoke tests are automated integration tests that verify the deployed application works correctly in a containerized environment. They run after the build job in the CI pipeline and test the application running in a Docker container.

## What Gets Tested

The smoke test job performs the following checks:

1. **Health Endpoint** (`/health`)
   - Verifies the endpoint returns HTTP 200
   - Validates the JSON response contains `"status": "Healthy"`
   - Confirms both database and application are running

2. **Readiness Endpoint** (`/health/ready`)
   - Verifies the endpoint returns HTTP 200
   - Confirms the database is connected and ready

3. **Liveness Endpoint** (`/health/live`)
   - Verifies the endpoint returns HTTP 200
   - Confirms the application process is responsive

4. **API Endpoint** (`/api/TaskItems`)
   - Verifies the API is accessible
   - Checks for HTTP 200 or 204 response

5. **Basic CRUD Operations**
   - Creates a new task item via POST
   - Retrieves the created task via GET
   - Verifies the data matches what was created

## Running Smoke Tests Locally

### Prerequisites

- Docker and Docker Compose installed
- Repository cloned locally

### Steps

1. **Start the application**:
   ```bash
   docker compose up -d
   ```

2. **Wait for the application to be ready** (optional, but recommended):
   ```bash
   for i in {1..30}; do
     status=$(curl -s -o /dev/null -w '%{http_code}' http://localhost:8080/health || echo "000")
     echo "Attempt $i: Health check status: $status"
     if [ "$status" = "200" ]; then
       echo "Application is healthy!"
       break
     fi
     sleep 2
   done
   ```

3. **Run the smoke tests**:
   ```bash
   # Test 1: Health endpoint
   curl -f http://localhost:8080/health
   
   # Test 2: Health response format
   curl -s http://localhost:8080/health | jq -e '.status == "Healthy"'
   
   # Test 3: Ready endpoint
   curl -f http://localhost:8080/health/ready
   
   # Test 4: Live endpoint
   curl -f http://localhost:8080/health/live
   
   # Test 5: API endpoint accessibility
   curl -s -o /dev/null -w '%{http_code}' http://localhost:8080/api/TaskItems
   
   # Test 6: Basic CRUD operations
   task_response=$(curl -s -X POST http://localhost:8080/api/TaskItems \
     -H "Content-Type: application/json" \
     -d '{"title": "Smoke Test Task", "description": "Integration test", "isComplete": false}')
   task_id=$(echo "$task_response" | jq -r '.id')
   echo "Created task with ID: $task_id"
   
   get_response=$(curl -s http://localhost:8080/api/TaskItems/$task_id)
   title=$(echo "$get_response" | jq -r '.title')
   echo "Retrieved task title: $title"
   ```

4. **Clean up**:
   ```bash
   docker compose down -v
   ```

### Quick Test Script

You can also run all tests at once using this script:

```bash
#!/bin/bash
set -e

echo "Starting application..."
docker compose up -d

echo "Waiting for application to be ready..."
for i in {1..30}; do
  status=$(curl -s -o /dev/null -w '%{http_code}' http://localhost:8080/health || echo "000")
  echo "Attempt $i: Health check status: $status"
  if [ "$status" = "200" ]; then
    echo "Application is healthy!"
    break
  fi
  sleep 2
done

echo "Running smoke tests..."

# Test 1: Health endpoint should return 200
echo "Test 1: Verifying /health endpoint"
curl -f http://localhost:8080/health || exit 1

# Test 2: Health endpoint should return valid JSON with status
echo "Test 2: Verifying health response format"
health_response=$(curl -s http://localhost:8080/health)
echo "$health_response" | jq -e '.status == "Healthy"' || exit 1

# Test 3: Ready endpoint should return 200
echo "Test 3: Verifying /health/ready endpoint"
curl -f http://localhost:8080/health/ready || exit 1

# Test 4: Live endpoint should return 200
echo "Test 4: Verifying /health/live endpoint"
curl -f http://localhost:8080/health/live || exit 1

# Test 5: API endpoint should be accessible
echo "Test 5: Verifying API endpoint accessibility"
api_response=$(curl -s -o /dev/null -w '%{http_code}' http://localhost:8080/api/TaskItems)
if [ "$api_response" = "200" ] || [ "$api_response" = "204" ]; then
  echo "API endpoint is accessible (status: $api_response)"
else
  echo "API endpoint returned unexpected status: $api_response"
  exit 1
fi

# Test 6: Basic CRUD operation
echo "Test 6: Verifying basic CRUD operations"
task_response=$(curl -s -X POST http://localhost:8080/api/TaskItems \
  -H "Content-Type: application/json" \
  -d '{"title": "Smoke Test Task", "description": "Integration test", "isComplete": false}')
task_id=$(echo "$task_response" | jq -r '.id')
echo "Created task with ID: $task_id"

# Verify the task was created
get_response=$(curl -s http://localhost:8080/api/TaskItems/$task_id)
title=$(echo "$get_response" | jq -r '.title')
if [ "$title" != "Smoke Test Task" ]; then
  echo "ERROR: CRUD test failed - title mismatch"
  exit 1
fi
echo "CRUD test passed - task created and retrieved successfully"

echo "All smoke tests passed!"

echo "Cleaning up..."
docker compose down -v
```

## CI/CD Integration

The smoke tests run automatically in the GitHub Actions CI pipeline as part of the `smoke-test` job. This job:

- Runs after the `build` job completes successfully
- Uses the `docker-compose.yml` configuration from the repository
- Tests the application in the same environment it would run in production
- Provides detailed output for each test step
- Ensures cleanup even if tests fail

### Viewing Results

To view smoke test results:

1. Go to the **Actions** tab in GitHub
2. Select the workflow run
3. Click on the **smoke-test** job
4. Expand the "Run smoke tests" step to see detailed output

## Troubleshooting

### Container fails to start

If the container fails to start:

1. Check Docker logs: `docker compose logs`
2. Verify Docker is running: `docker ps`
3. Check for port conflicts: `lsof -i :8080`

### Health check times out

If the health check takes too long:

1. Check if migrations are running: `docker compose logs | grep migration`
2. Increase the wait time in the test script
3. Verify the database is accessible

### Tests fail

If specific tests fail:

1. Check the application logs: `docker compose logs taskflow-api`
2. Verify the endpoint manually: `curl -v http://localhost:8080/health`
3. Check for database connection issues

## Branch Protection

Once the smoke tests are stable and reliable, consider enabling the `smoke-test` check as required in branch protection rules:

1. Go to **Settings** → **Branches**
2. Edit the branch protection rule for `main`
3. Under "Require status checks to pass before merging", add `smoke-test`
4. Save the changes

This ensures that all pull requests pass smoke tests before they can be merged.
