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

## Testing

Test project:

```text
BreastCancer.Tests/BreastCancer.Tests.csproj
```

From the repository root:

```bash
# Run all tests
dotnet test BreastCancer.Tests/BreastCancer.Tests.csproj

# Run only unit tests
dotnet test BreastCancer.Tests/BreastCancer.Tests.csproj --filter FullyQualifiedName~Unit

# Run only integration tests
dotnet test BreastCancer.Tests/BreastCancer.Tests.csproj --filter FullyQualifiedName~Integration
```

Coverage:

```bash
# 1) Full coverage report (no threshold gate)
dotnet test BreastCancer.Tests/BreastCancer.Tests.csproj --configuration Release /p:CollectCoverage=true /p:CoverletOutput=./BreastCancer.Tests/TestResults/coverage-full/ /p:CoverletOutputFormat=cobertura

# 2) Scoped coverage gate (business logic only)
dotnet test BreastCancer.Tests/BreastCancer.Tests.csproj --configuration Release /p:CollectCoverage=true /p:CoverletOutput=./BreastCancer.Tests/TestResults/coverage-scoped/ /p:CoverletOutputFormat=cobertura /p:ExcludeByFile="**/Migrations/**%2c**/*.Designer.cs%2c**/Program.cs%2c**/Context/**%2c**/Templates/**" /p:Threshold=30 /p:ThresholdType=line /p:ThresholdStat=total

# Run both sequentially (Windows cmd)
dotnet test BreastCancer.Tests/BreastCancer.Tests.csproj --configuration Release /p:CollectCoverage=true /p:CoverletOutput=./BreastCancer.Tests/TestResults/coverage-full/ /p:CoverletOutputFormat=cobertura && dotnet test BreastCancer.Tests/BreastCancer.Tests.csproj --configuration Release /p:CollectCoverage=true /p:CoverletOutput=./BreastCancer.Tests/TestResults/coverage-scoped/ /p:CoverletOutputFormat=cobertura /p:ExcludeByFile="**/Migrations/**%2c**/*.Designer.cs%2c**/Program.cs%2c**/Context/**%2c**/Templates/**" /p:Threshold=30 /p:ThresholdType=line /p:ThresholdStat=total
```

Coverage file:

```text
BreastCancer.Tests/TestResults/coverage-full/coverage.cobertura.xml
BreastCancer.Tests/TestResults/coverage-scoped/coverage.cobertura.xml
```

CI (GitHub Actions):

- Workflow: `.github/workflows/dotnet-tests.yml`
- Trigger: pull requests, pushes to `main`/`master`
- Action: run tests + full coverage report + scoped coverage threshold gate + upload both coverage artifacts

## Project Structure

```
BreastCancer/
├── Controllers/          # API endpoints
├── Models/               # Entity models
├── Context/              # Database context
├── Repository/           # Data access layer
├── Service/              # Business logic
├── BreastCancer.Tests/   # Unit and integration tests
├── appsettings.json      # Local configuration
├── appsettings.Docker.json   # Docker configuration
├── docker-compose.yml    # Docker services
└── Dockerfile
```
