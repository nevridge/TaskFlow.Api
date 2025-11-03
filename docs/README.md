# TaskFlow.Api Documentation

Welcome to the TaskFlow.Api documentation! This guide helps you navigate the documentation based on your needs.

## Quick Navigation

### Getting Started (New Users)

Start here if you're new to TaskFlow.Api:

1. **[Getting Started Guide](GETTING_STARTED.md)** - Setup and run locally in 5 minutes
2. **[API Reference](API.md)** - Explore available endpoints with examples
3. **[Architecture Overview](ARCHITECTURE.md)** - Understand the design and patterns

### Contributing (Developers)

Working on TaskFlow.Api or planning to contribute:

1. **[Contributing Guide](CONTRIBUTING.md)** - Development workflow, standards, and PR process
2. **[Architecture & Design](ARCHITECTURE.md)** - Design patterns, service registration, testing
3. **[Service Registration Pattern](SERVICE_REGISTRATION_PATTERN.md)** - Detailed guide on the DI pattern

### Deployment (DevOps)

Deploying TaskFlow.Api to various environments:

1. **[Deployment Guide](DEPLOYMENT.md)** - Comprehensive Docker and Azure deployment guide
2. **[Azure OIDC Authentication](AZURE_OIDC_AUTHENTICATION.md)** - Setting up passwordless Azure authentication
3. **[Resource Naming Convention](deploy.md)** - Azure resource naming standards

### Advanced Topics (Deep Dives)

Detailed technical documentation for specific scenarios:

#### Docker & Containers
- **[Docker Configuration Guide](DOCKER_CONFIGURATION.md)** - Detailed comparison of dev vs production Docker configs
- **[Volume Configuration](volumes.md)** - Docker volume management and persistence
- **[Volume Testing Guide](VOLUME_TESTING.md)** - Testing volume persistence across container restarts

#### Health Checks & Monitoring
- **[Health Check Testing](HEALTH_CHECK_TESTING.md)** - Testing health checks in Docker and Kubernetes
- **[Logging Guide](logging.md)** - Serilog configuration and log management

#### Azure Deployment
- **[QA Deployment Guide](QA_DEPLOYMENT.md)** - Ephemeral QA environments with fixed DNS
- **[Resource Naming Convention](deploy.md)** - Detailed Azure naming standards and validation

#### Security
- **[Security Scanning](SECURITY_SCANNING.md)** - CodeQL and Trivy integration

## Documentation Structure

### Primary Documentation (Start Here)

| Document | Purpose | Audience |
|----------|---------|----------|
| [Getting Started](GETTING_STARTED.md) | Quick setup and first run | All users |
| [API Reference](API.md) | Complete endpoint documentation | API consumers, developers |
| [Architecture](ARCHITECTURE.md) | Design decisions and patterns | Developers, reviewers |
| [Deployment](DEPLOYMENT.md) | Docker and Azure deployment | DevOps, developers |
| [Contributing](CONTRIBUTING.md) | Development workflow | Contributors |

### Reference Documentation (Deep Dives)

| Document | Purpose | When to Use |
|----------|---------|-------------|
| [Docker Configuration](DOCKER_CONFIGURATION.md) | Dev vs prod Docker comparison | Understanding Docker setup differences |
| [Volume Configuration](volumes.md) | Docker volume management | Configuring persistent storage |
| [Volume Testing](VOLUME_TESTING.md) | Testing volume persistence | Verifying data persistence |
| [Health Check Testing](HEALTH_CHECK_TESTING.md) | Testing health checks | Debugging health check issues |
| [QA Deployment](QA_DEPLOYMENT.md) | QA environment details | Setting up QA testing |
| [Resource Naming](deploy.md) | Azure naming standards | Understanding resource names |
| [Service Registration](SERVICE_REGISTRATION_PATTERN.md) | DI extension pattern | Adding new services |
| [Azure OIDC Auth](AZURE_OIDC_AUTHENTICATION.md) | Azure authentication setup | Configuring GitHub Actions deployment |
| [Security Scanning](SECURITY_SCANNING.md) | CodeQL and Trivy | Understanding security scans |
| [Logging](logging.md) | Serilog configuration | Configuring logging |

## Documentation Philosophy

TaskFlow.Api documentation follows an audience-focused approach:

**For Developers:**
- Get started quickly (<5 minutes)
- Clear examples and code snippets
- Progressive disclosure (overview → details)
- Troubleshooting guidance

**For Employers/Reviewers:**
- Quick assessment of skills and practices
- Visible quality indicators
- Architecture and design rationale
- CI/CD and deployment patterns

**Structure:**
- **README.md** (repository root) - Project overview, quick start, key highlights
- **Primary Documentation** (this directory) - Comprehensive guides for main topics
- **Reference Documentation** (this directory) - Deep technical details for specific scenarios

## Finding What You Need

**"I want to run the app locally"** → [Getting Started](GETTING_STARTED.md)

**"I need to understand the API endpoints"** → [API Reference](API.md)

**"I'm evaluating this project for hiring"** → Start with [Architecture](ARCHITECTURE.md), then [Deployment](DEPLOYMENT.md)

**"I want to contribute code"** → [Contributing Guide](CONTRIBUTING.md)

**"I need to deploy to Azure"** → [Deployment Guide](DEPLOYMENT.md)

**"I'm having Docker issues"** → [Deployment Guide](DEPLOYMENT.md#troubleshooting), then [Docker Configuration](DOCKER_CONFIGURATION.md)

**"I need to configure volumes"** → [Volume Configuration](volumes.md)

**"Health checks aren't working"** → [Health Check Testing](HEALTH_CHECK_TESTING.md)

**"I want to set up QA environment"** → [QA Deployment Guide](QA_DEPLOYMENT.md)

**"I need to add a new service"** → [Service Registration Pattern](SERVICE_REGISTRATION_PATTERN.md)

## Feedback and Improvements

Found an issue or have a suggestion for improving the documentation?

- **Open an issue** on GitHub
- **Submit a pull request** with improvements
- See [Contributing Guide](CONTRIBUTING.md) for details

---

[← Back to Repository README](../README.md)
