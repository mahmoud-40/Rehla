# Rehla - Backend API

An AI-powered platform that predicts breast cancer recurrence and provides continuous support for survivors. The system connects patients with doctors and caregivers, offering medical guidance, mental health support, and lifestyle tracking in one place.

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running

## Quick Start

```bash
# Clone the repository
git clone https://github.com/mahmoud-40/BreastCancer.git
cd BreastCancer

# Start all services
docker-compose up -d --build

# Verify containers are running
docker-compose ps
```

Wait ~30 seconds for SQL Server to initialize. Both containers should show `Up` status.

## Services

| Service | URL |
|---------|-----|
| API | `http://localhost:8086/api` |
| Swagger | `http://localhost:8086/swagger` |
| SQL Server | `localhost,1433` |

## Database Connection

| Property | Value |
|----------|-------|
| Server | `localhost,1433` |
| Database | `BreastCancerDB` |
| User | `sa` |
| Password | `BC@Password123!` |

## Authentication

Protected endpoints require a JWT token:

```
Authorization: Bearer <token>
```

Use Swagger UI to test endpoints and authenticate.

## Docker Commands

```bash
# Start services
docker-compose up -d --build

# Stop services
docker-compose down

# View logs
docker-compose logs -f backend

# Restart backend
docker-compose up -d --build backend

# Reset everything (clears database)
docker-compose down -v
docker-compose up -d --build
```

## Troubleshooting

**Container keeps restarting**
```bash
docker-compose logs backend
```

**Cannot connect to database**
- Ensure SQL Server is healthy: `docker-compose ps`
- Wait 30 seconds after startup for initialization

**Port conflict**
```bash
docker-compose down
netstat -ano | findstr :8086
```

## Local Development

For running without Docker:

1. Install .NET 8 SDK and SQL Server
2. Update connection string in `appsettings.json`
3. Run:
   ```bash
   dotnet ef database update
   dotnet run
   ```

## Project Structure

```
BreastCancer/
├── Controllers/          # API endpoints
├── Models/               # Entity models
├── Context/              # Database context
├── Repository/           # Data access layer
├── Service/              # Business logic
├── appsettings.json      # Local configuration
├── appsettings.Docker.json   # Docker configuration
├── docker-compose.yml    # Docker services
└── Dockerfile
```
