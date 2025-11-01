# Quick Start Guide - Archu Application with Aspire

## Prerequisites

- .NET 8 SDK or later
- Docker Desktop (for SQL Server container)
- Visual Studio 2022 or VS Code

## Running the Application

### Option 1: Using Aspire (Recommended)

**Start the entire application stack with one command:**

```sh
dotnet run --project src\Archu.AppHost
```

**What happens:**
1. 🐳 SQL Server container starts with persistent storage
2. 🔧 Archu.Api backend starts on dynamic port
3. 👨‍💼 Archu.AdminApi starts on dynamic port
4. 🌐 Archu.Web frontend starts on dynamic port
5. 📊 Aspire Dashboard opens in browser

**Access:**
- **Aspire Dashboard**: http://localhost:15001
  - View all services status
  - Monitor logs in real-time
  - See request traces
  - Check resource usage

- **Web Application**: Click "web" endpoint in Aspire Dashboard
- **API Documentation**: Click "api" endpoint, then navigate to `/scalar/v1`

### Option 2: Using Local SQL Server

If you prefer to use your local SQL Server instance:

**Set environment variable:**

```powershell
# PowerShell
$env:ARCHU_USE_LOCAL_DB="true"
dotnet run --project src\Archu.AppHost
```

```cmd
# Command Prompt
set ARCHU_USE_LOCAL_DB=true
dotnet run --project src\Archu.AppHost
```

```bash
# Linux/macOS
export ARCHU_USE_LOCAL_DB=true
dotnet run --project src/Archu.AppHost
```

**Requirements:**
- SQL Server running locally
- Update connection string in `src\Archu.Api\appsettings.Development.json`
- Run migrations: `dotnet ef database update --project src\Archu.Infrastructure`

## First Time Setup

### 1. Run Database Migrations

```sh
cd src\Archu.Infrastructure
dotnet ef database update
```

### 2. Start the Application

```sh
# From repository root
dotnet run --project src\Archu.AppHost
```

### 3. Register a User

1. Open the Web app from Aspire Dashboard
2. Click "Sign Up" or navigate to `/register`
3. Fill in:
   - **Username**: yourname (3-50 characters)
   - **Email**: your@email.com
   - **Password**: YourPassword123! (min 8 characters)
4. Click "Create Account"
5. You'll be automatically logged in

### 4. Explore the Application

**Try these features:**

1. **View Products** (`/products`)
   - Browse the product catalog
   - Search for products
   - Protected route - requires login

2. **Create Product** (`/products/create`)
   - Add new products
   - Form validation
   - Protected route - requires login

3. **User Profile** (User dropdown menu)
   - View profile information
   - Access settings
   - Logout

## Application URLs

When running with Aspire, all URLs are dynamic. Find them in the **Aspire Dashboard**:

**Typical URLs** (ports may vary):
- Aspire Dashboard: http://localhost:15001
- Web App: https://localhost:50xx
- API: https://localhost:51xx
- Admin API: https://localhost:52xx

## Development Workflow

### Make Changes to Code

1. **Stop the application** (Ctrl+C in terminal)
2. **Make your changes**
3. **Restart**: `dotnet run --project src\Archu.AppHost`
4. **Aspire hot reloads** most changes automatically

### View Logs

**In Aspire Dashboard:**
1. Click on a service (api, web, admin-api)
2. Click "Logs" tab
3. See real-time logs with filtering

### Debug in Visual Studio

1. Open `Archu.sln`
2. Set `Archu.AppHost` as startup project
3. Press F5 to start debugging
4. Set breakpoints in any project
5. Debug across all services simultaneously

### Debug in VS Code

1. Open workspace folder
2. Install "C# Dev Kit" extension
3. Open `src\Archu.AppHost\Program.cs`
4. Press F5 to start debugging
5. Select ".NET Core Launch (web)" configuration

## Testing Authentication

### Login Flow

1. Navigate to `/login`
2. Enter credentials:
   ```
   Email: your@email.com
   Password: YourPassword123!
   ```
3. Click "Sign In"
4. Redirected to home page
5. User menu appears in top-right

### Protected Routes

Try accessing `/products` without logging in:
1. Navigate to `/products`
2. Redirected to `/login?returnUrl=/products`
3. After login, redirected back to `/products`

### Logout

1. Click user icon (top-right)
2. Click "Logout"
3. Token cleared from browser
4. Redirected to home page
5. User menu changes to "Login" and "Sign Up"

## API Testing

### Using Scalar API Documentation

1. Open Aspire Dashboard
2. Click "api" endpoint
3. Navigate to `/scalar/v1`
4. Interactive API documentation with:
   - All endpoints documented
   - Try-it-out functionality
   - Authentication support

### Test Authentication Endpoint

**Register:**
```http
POST https://localhost:51xx/api/v1/authentication/register
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "TestPassword123!",
  "userName": "testuser"
}
```

**Login:**
```http
POST https://localhost:51xx/api/v1/authentication/login
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "TestPassword123!"
}
```

**Get Products (with authentication):**
```http
GET https://localhost:51xx/api/v1/products
Authorization: Bearer <your-access-token>
```

## Troubleshooting

### "Docker Desktop is not running"

**Solution:**
```sh
# Start Docker Desktop
# Then restart Aspire
dotnet run --project src\Archu.AppHost
```

Or use local SQL Server:
```sh
$env:ARCHU_USE_LOCAL_DB="true"
dotnet run --project src\Archu.AppHost
```

### "Port already in use"

**Solution:**
```sh
# Stop all services
Ctrl+C

# Kill dotnet processes
taskkill /F /IM dotnet.exe

# Restart
dotnet run --project src\Archu.AppHost
```

### "Database connection failed"

**Using Docker:**
```sh
# Check Docker container
docker ps | findstr sql

# Restart Aspire (will recreate container)
dotnet run --project src\Archu.AppHost
```

**Using Local SQL:**
```sh
# Verify SQL Server is running
# Check connection string in appsettings.Development.json
# Run migrations
cd src\Archu.Infrastructure
dotnet ef database update
```

### "API not responding"

1. Check Aspire Dashboard for API status
2. View API logs in Aspire Dashboard
3. Verify SQL Server is connected (check API logs)
4. Restart: `dotnet run --project src\Archu.AppHost`

### "Login not working"

1. **Check API is running** (Aspire Dashboard)
2. **Check browser console** (F12) for errors
3. **Verify API URL** in Web app logs
4. **Check CORS** in API logs
5. **Clear browser storage**:
   - F12 → Application → Local Storage
   - Delete all items
   - Refresh page

## Project Structure

```
Archu/
├── src/
│   ├── Archu.AppHost/          # Aspire orchestration & hosting
│   ├── Archu.Api/              # Public REST API
│   ├── Archu.AdminApi/         # Administrative REST API
│   ├── Archu.Web/              # Blazor WebAssembly frontend
│   ├── Archu.ApiClient/        # Typed HTTP client library
│   ├── Archu.Application/      # CQRS handlers & business logic
│   ├── Archu.Contracts/        # Shared request/response DTOs
│   ├── Archu.Domain/           # Domain entities and value objects
│   ├── Archu.Infrastructure/   # Persistence and external integrations
│   ├── Archu.ServiceDefaults/  # Shared Aspire configuration
│   └── Archu.Ui/               # Shared UI components
├── tests/
│   ├── Archu.IntegrationTests/ # API integration test suite
│   ├── Archu.ApiClient.Tests/  # API client unit tests
│   ├── Archu.Ui.Tests/         # UI accessibility tests
│   └── Archu.UnitTests/        # Domain & application unit tests
├── docs/                       # Comprehensive documentation hub
└── README.md                   # Solution overview and quick links
```

## Next Steps

1. **Explore the Code**
   - Check out Clean Architecture layers
   - Review authentication implementation
   - Study API client patterns

2. **Add Features**
   - Create new pages in Archu.Web
   - Add new API endpoints
   - Extend the domain model

3. **Deploy**
   - See [Archu.AppHost README](src/Archu.AppHost/README.md) for deployment and orchestration guidance
   - Azure Container Apps ready
   - Docker compose support

## Documentation

- **AppHost Orchestration**: [src/Archu.AppHost/README.md](src/Archu.AppHost/README.md)
- **Authentication**: [docs/AUTHENTICATION_GUIDE.md](docs/AUTHENTICATION_GUIDE.md)
- **API Client**: [src/Archu.ApiClient/README.md](src/Archu.ApiClient/README.md)

## Getting Help

- **Aspire Dashboard**: Real-time service monitoring
- **Logs**: Check Aspire Dashboard for all service logs
- **GitHub Issues**: Report bugs or request features
- **Documentation**: See individual project README files

---

**Quick Commands:**

```sh
# Start everything
dotnet run --project src\Archu.AppHost

# Use local database
$env:ARCHU_USE_LOCAL_DB="true"
dotnet run --project src\Archu.AppHost

# Run migrations
cd src\Archu.Infrastructure
dotnet ef database update

# Build solution
dotnet build

# Clean and rebuild
dotnet clean
dotnet build
```

**Happy Coding! 🚀**
