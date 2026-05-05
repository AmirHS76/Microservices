# ASP.NET Core Microservices + RabbitMQ + Gateway

This solution contains 3 clean architecture microservices:
- Register Service (`http://localhost:5001`)
- SSO Service (`http://localhost:5002`)
- User Service (`http://localhost:5003`)

And one reverse proxy gateway:
- Gateway (`http://localhost:5000`)

## Flow
1. `POST /register` through gateway registers a user in Register service.
2. Register service publishes `UserRegisteredEvent` to RabbitMQ.
3. SSO and User services consume the event:
- SSO creates the identity account used for authentication.
- User stores the user profile projection for management changes.
4. SSO records login audit entries when users authenticate.

## Gateway routes
- `POST http://localhost:5000/register`
- `POST http://localhost:5000/sso/auth/login`
- `POST http://localhost:5000/sso/auth/assign-role`
- `GET  http://localhost:5000/sso/audits`
- `GET  http://localhost:5000/user`
- `PUT  http://localhost:5000/user/{userId}`

List endpoints support generic pagination through query string values:
- `pageNumber`, default `1`
- `pageSize`, default `10`, maximum `100`

Example: `GET http://localhost:5000/user?pageNumber=2&pageSize=25`

API responses use a shared envelope:
```json
{
  "success": true,
  "message": "Request completed successfully.",
  "data": [],
  "errors": [],
  "pagination": {
    "pageNumber": 1,
    "pageSize": 10,
    "totalItems": 0,
    "totalPages": 0,
    "hasPreviousPage": false,
    "hasNextPage": false
  }
}
```

## Angular front-end
The Angular TypeScript client lives in `src/Web/UserManagement.Client`.

```bash
cd src/Web/UserManagement.Client
npm install
npm start
```

Open `http://localhost:4200`. The client calls the gateway at `http://localhost:5000` and includes:
- Global modern dark theme
- Login and register pages
- Protected user management console
- User search, sort, refresh, CSV export
- Admin role assignment
- Admin login audit view

## Run RabbitMQ
```bash
docker compose up -d rabbitmq
```
RabbitMQ management UI: `http://localhost:15672` (`guest` / `guest`).

## Run services
```bash
dotnet run --project src/Services/Register/Register.Api/Register.Api.csproj --urls http://localhost:5001
dotnet run --project src/Services/SSO/SSO.Api/SSO.Api.csproj --urls http://localhost:5002
dotnet run --project src/Services/User/User.Api/User.Api.csproj --urls http://localhost:5003
dotnet run --project src/Gateway/Gateway.Api/Gateway.Api.csproj --urls http://localhost:5000
```

## SQL Server + Logging
Each microservice now uses SQL Server via EF Core and reads its own connection string key:
- Register: `ConnectionStrings:RegisterDb`
- SSO: `ConnectionStrings:SsoDb`
- User: `ConnectionStrings:UserDb`

Each API and the gateway now use Serilog with:
- Console sink
- Rolling file sink (`logs/log-.txt`)

## Auth endpoints (SSO Service via Gateway)
- `POST /register` creates new users through the Register service.
- `POST /sso/auth/login` authenticates users and returns a JWT.

Use returned JWT bearer token for protected gateway routes.

### Role-based authorization
- New users are created with `User` role.
- SSO audits endpoint requires `Admin` role.
- Admin can assign roles via `POST /sso/auth/assign-role`.
- Set `AdminSeed:Email` and `AdminSeed:Password` in SSO Service config to bootstrap an admin user.
