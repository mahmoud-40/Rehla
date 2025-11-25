# BreastCancer

### Prerequisites
- Docker & Docker Compose
- .NET 8 SDK (for local development)

### Running the Project
1. **Clone the repository**
2. **Start all services:**
   ```bash
   docker-compose up -d
   ```
3. **Wait for services to be healthy** (check with `docker-compose ps`)
4. **Access the applications:**
   - **API & Swagger Docs**: http://localhost:8080/swagger
   - **Keycloak Admin**: http://localhost:8081
     - Username: `admin`
     - Password: `admin123`

### Demo Users for Testing
- **Doctor**: `demo.doctor` / `doctor123`
- **Patient**: `demo.patient` / `patient123`  
- **Admin**: `admin` / `admin123`
- **Caregiver**: `demo.caregiver` / `caregiver123`
- 
## Development Workflow

### Branching Strategy
```
main (protected)
├── feature/authentication
├── feature/patient-management  
├── feature/ai-prediction
└── hotfix/critical-bug
```

### Working on Features
1. **Create a feature branch:**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make your changes and commit:**
   ```bash
   git add .
   git commit -m "feat: add patient profile management"
   ```

3. **Push and create Pull Request:**
   ```bash
   git push origin feature/your-feature-name
   ```
   Then create PR on GitHub

4. **Get code review:**
   - Wait for **at least 1 review** from another team member
   - Address any review comments

5. **Merge after approval:**
   - Once approved, merge to `main`
   - Delete the feature branch

### Commit Message Convention
- `feat:` New features
- `fix:` Bug fixes  
- `docs:` Documentation
- `refactor:` Code restructuring
- `test:` Adding tests
