# 👤 TC Cloud Games - Users Service

The Users microservice is responsible for user management, authentication, authorization, and access control for the TC Cloud Games platform. It implements Event Sourcing for complete audit trails and provides JWT-based authentication.

## 🏗️ Architecture Overview

This service follows **Hexagonal Architecture (Ports & Adapters)** with **Domain-Driven Design (DDD)** and **Event Sourcing**:

````
TC.CloudGames.Users/
├── 🎯 Core/ # Business Logic
│ ├── Domain/ # Domain Layer
│ │ ├── Aggregates/ # UserAggregate
│ │ ├── ValueObjects/ # Email, Password, Role
│ │ └── Abstractions/ # Domain Interfaces
│ └── Application/ # Application Layer
│ ├── UseCases/ # CQRS Commands & Queries
│ ├── Ports/ # Application Interfaces
│ └── Mappers/ # DTO Mappers
├── 🔌 Adapters/ # Infrastructure & API
│ ├── Inbound/ # API Layer
│ │ └── TC.CloudGames.Users.Api/ # REST API Endpoints
│ └── Outbound/ # Infrastructure Layer
│ └── TC.CloudGames.Users.Infrastructure/ # Database & Repositories
└── 🧪 test/ # Comprehensive Test Suite
└── TC.CloudGames.Users.Unit.Tests/
├── 1 - Unit.Testing/ # Domain & Application Tests
├── 2 - Architecture.Testing/ # Architecture Validation
├── 3 - Integration.Testing/ # Integration Tests
└── 4 - BDD.Testing/ # Behavior-Driven Tests
````

## 🚀 API Endpoints

### Authentication

#### User Registration
```http
POST /api/auth/register
Content-Type: application/json

{
  "name": "John Smith",
  "email": "john.smith@example.com",
  "username": "johnsmith",
  "password": "SecurePassword123!",
  "role": "User"
}
```

**Response:**
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "name": "John Smith",
  "email": "john.smith@example.com",
  "username": "johnsmith",
  "role": "User",
  "createdAt": "2024-01-15T10:30:00Z",
  "isActive": true
}
```

#### User Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "john.smith@example.com",
  "password": "SecurePassword123!"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2024-01-15T18:30:00Z",
  "user": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "name": "John Smith",
    "email": "john.smith@example.com",
    "username": "johnsmith",
    "role": "User"
  }
}
```

### User Management

#### Get User by ID
```http
GET /api/user/{id}
Authorization: Bearer {token}
```

#### Get User by Email
```http
GET /api/user/email/{email}
Authorization: Bearer {token}
```

#### Get User List
```http
GET /api/users?page=1&size=20&role=User
Authorization: Bearer {token}
```

#### Update User
```http
PUT /api/user/{id}
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "John Updated Smith",
  "email": "john.updated@example.com",
  "username": "johnupdated"
}
```


## 🔐 Security & Authentication

### JWT Token Structure
```json
{
  "sub": "123e4567-e89b-12d3-a456-426614174000",
  "name": "John Smith",
  "email": "john.smith@example.com",
  "username": "johnsmith",
  "role": "User",
  "iat": 1705315800,
  "exp": 1705343400,
  "iss": "TC.CloudGames.Users",
  "aud": "TC.CloudGames"
}
```

### Role-Based Access Control

#### User Role
- **Permissions**: View own profile, update own information
- **Restrictions**: Cannot access other users' data

#### Moderator Role
- **Permissions**: All User permissions + moderate content
- **Restrictions**: Cannot manage user roles

#### Admin Role
- **Permissions**: Full system access
- **Capabilities**: User management, role assignment, system configuration

### Password Security
- **Hashing**: BCrypt with salt
- **Requirements**: Minimum 8 characters, complexity rules
- **Storage**: Only hashed passwords stored
- **Verification**: Secure comparison against hash

## 🤝 Contributing

### Development Guidelines
1. **Architecture**: Follow hexagonal architecture principles
2. **Testing**: Maintain >90% code coverage
3. **Security**: Implement comprehensive security measures
4. **Documentation**: Update API documentation with changes
5. **Event Sourcing**: Ensure all state changes are event-driven

### Code Standards
- **C#**: Follow Microsoft coding conventions
- **Validation**: Use FluentValidation for input validation
- **Logging**: Use structured logging with correlation IDs
- **Error Handling**: Implement comprehensive error handling
- **Security**: Follow OWASP security guidelines

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
