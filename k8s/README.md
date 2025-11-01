# Kubernetes Deployment

This directory contains example Kubernetes manifests for deploying TaskFlow.Api to a Kubernetes cluster.

## Files

- `deployment.yaml` - Complete deployment configuration including:
  - Deployment with 2 replicas
  - Service (LoadBalancer type)
  - PersistentVolumeClaims for data and logs

## Quick Start

### Prerequisites

- Kubernetes cluster (local or cloud)
- `kubectl` configured to access your cluster
- Docker image available (either pushed to a registry or built locally)

### Deploy to Kubernetes

1. **Build and push the Docker image** (if deploying to a remote cluster):
   ```bash
   docker build -f TaskFlow.Api/Dockerfile -t your-registry/taskflow-api:latest .
   docker push your-registry/taskflow-api:latest
   ```

2. **Update the image reference** in `deployment.yaml`:
   ```yaml
   image: your-registry/taskflow-api:latest
   ```

3. **Apply the manifests**:
   ```bash
   kubectl apply -f k8s/deployment.yaml
   ```

4. **Check deployment status**:
   ```bash
   kubectl get pods -l app=taskflow-api
   kubectl get svc taskflow-api
   ```

5. **View logs**:
   ```bash
   kubectl logs -l app=taskflow-api -f
   ```

## Health Check Configuration

The deployment includes carefully configured health probes to accommodate database migrations at startup:

### Liveness Probe
- **Path**: `/health/live`
- **Initial Delay**: 60 seconds
- **Purpose**: Ensures Kubernetes doesn't restart the pod during migrations
- **Check**: Lightweight, doesn't verify database connectivity

### Readiness Probe
- **Path**: `/health/ready`
- **Initial Delay**: 45 seconds
- **Purpose**: Ensures pod only receives traffic after migrations complete
- **Check**: Includes database connectivity validation

### Why These Delays?

When TaskFlow.Api starts with `Database__MigrateOnStartup=true`, it automatically applies Entity Framework migrations. This process can take 10-30 seconds depending on:
- Number of pending migrations
- Database performance
- First-time database initialization

The configured delays prevent:
- Premature pod restarts (liveness probe)
- Traffic routing to pods still running migrations (readiness probe)
- Failed deployments due to migration timing

**Important**: These values are conservative for typical SQLite migrations. Adjust based on your actual migration complexity and database type.

## Configuration

### Environment Variables

The deployment sets the following environment variables:

```yaml
- ASPNETCORE_ENVIRONMENT=Production
- ASPNETCORE_URLS=http://+:8080
- ASPNETCORE_HTTP_PORTS=8080
- Database__MigrateOnStartup=true
- DOTNET_RUNNING_IN_CONTAINER=true
```

To customize, edit the `env` section in `deployment.yaml`.

### Database Connection String

By default, the app uses SQLite at `/app/data/tasks.db`. For production, consider:

1. **Using a different database** (PostgreSQL, SQL Server):
   ```yaml
   env:
   - name: ConnectionStrings__DefaultConnection
     value: "Host=postgres-service;Database=taskflow;Username=user;Password=pass"
   ```

2. **Using Kubernetes Secrets** for sensitive data:
   ```bash
   kubectl create secret generic taskflow-db \
     --from-literal=connection-string='Host=postgres;Database=taskflow;...'
   ```
   
   Then reference in deployment:
   ```yaml
   env:
   - name: ConnectionStrings__DefaultConnection
     valueFrom:
       secretKeyRef:
         name: taskflow-db
         key: connection-string
   ```

### Persistent Storage

The deployment uses PersistentVolumeClaims (PVCs) for:
- `/app/data` - Database files (1Gi)
- `/app/logs` - Application logs (1Gi)

PVCs are automatically provisioned by your cluster's default StorageClass. To use a specific StorageClass:

```yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: taskflow-data-pvc
spec:
  storageClassName: your-storage-class
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 1Gi
```

### Resource Limits

The deployment includes resource requests and limits:

```yaml
resources:
  requests:
    memory: "256Mi"
    cpu: "250m"
  limits:
    memory: "512Mi"
    cpu: "500m"
```

Adjust these based on your workload requirements.

## Service Configuration

The deployment includes a LoadBalancer service:

```yaml
apiVersion: v1
kind: Service
metadata:
  name: taskflow-api
spec:
  type: LoadBalancer
  ports:
  - port: 80
    targetPort: 8080
```

### Alternative Service Types

For different deployment scenarios:

**NodePort** (local/on-premises):
```yaml
spec:
  type: NodePort
  ports:
  - port: 80
    targetPort: 8080
    nodePort: 30080  # Optional: specify port
```

**Ingress** (with ingress controller):
```yaml
spec:
  type: ClusterIP
  ports:
  - port: 80
    targetPort: 8080
```

Then create an Ingress resource:
```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: taskflow-api
spec:
  rules:
  - host: taskflow.example.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: taskflow-api
            port:
              number: 80
```

## Scaling

Scale the deployment:

```bash
# Scale to 3 replicas
kubectl scale deployment taskflow-api --replicas=3

# Auto-scale based on CPU
kubectl autoscale deployment taskflow-api --min=2 --max=10 --cpu-percent=70
```

**Note**: When using SQLite with ReadWriteOnce PVCs, multiple replicas will share the same database file, which can cause locking issues. For multi-replica production deployments, use a centralized database (PostgreSQL, SQL Server, MySQL).

## Monitoring

Check pod health:
```bash
# View pod status
kubectl get pods -l app=taskflow-api

# Describe pod (includes events)
kubectl describe pod -l app=taskflow-api

# View logs
kubectl logs -l app=taskflow-api --tail=100 -f

# Check health endpoints
kubectl port-forward svc/taskflow-api 8080:80
curl http://localhost:8080/health
```

## Troubleshooting

### Pods not becoming ready
1. Check logs: `kubectl logs -l app=taskflow-api`
2. Check events: `kubectl describe pod -l app=taskflow-api`
3. Verify migrations completed: Look for "Applying EF Core migrations" in logs
4. Increase `initialDelaySeconds` if migrations take longer

### Database issues
1. Check PVC status: `kubectl get pvc`
2. Verify PVC bound: `kubectl describe pvc taskflow-data-pvc`
3. Check database file permissions
4. Review connection string configuration

### Image pull errors
1. Verify image exists: `docker pull your-registry/taskflow-api:latest`
2. Check image pull secrets if using private registry
3. Ensure correct image name in deployment

### CrashLoopBackOff
1. Check application logs: `kubectl logs -l app=taskflow-api --previous`
2. Verify environment variables are correct
3. Check if liveness probe is failing too quickly (increase `initialDelaySeconds`)

## Clean Up

Remove all resources:
```bash
kubectl delete -f k8s/deployment.yaml
```

Or individually:
```bash
kubectl delete deployment taskflow-api
kubectl delete service taskflow-api
kubectl delete pvc taskflow-data-pvc taskflow-logs-pvc
```
