# ClassicFit Apparel Fashion: Sales & Support Workspace

A comprehensive ASP.NET Core Web API for managing customer support tickets, calls, customer interactions, and sales orders.

## Project Structure

The project is organized into the following folders:

### 📁 Models
Contains all domain entities:
- `User.cs` - User management with role-based access (Agent, Supervisor, Admin)
- `Customer.cs` - Customer information and contact details
- `Call.cs` - Call logs and interaction tracking
- `Ticket.cs` - Support ticket management
- `TicketComment.cs` - Ticket comments and updates
- `Product.cs` - Product catalog and inventory
- `Order.cs` - Sales order management
- `OrderDetail.cs` - Order line items
- `WeatherForecast.cs` - Sample weather data (can be removed)

### 📁 DTOs
Data Transfer Objects for API operations:
- `UserDTOs.cs` - User authentication and management DTOs
- `CustomerDTOs.cs` - Customer data transfer objects
- `TicketDTOs.cs` - Ticket management DTOs
- `CallDTOs.cs` - Call logging DTOs
- `OrderDTOs.cs` - Order processing DTOs

### 📁 Services
Service interfaces for business logic:
- `IUserService.cs` - User management operations
- `ICustomerService.cs` - Customer management operations
- `ITicketService.cs` - Ticket management operations
- `ICallService.cs` - Call logging operations
- `IOrderService.cs` - Order processing operations
- `IProductService.cs` - Product management operations

### 📁 Controllers
API controllers for HTTP endpoints:
- `AuthController.cs` - Authentication endpoints (login, register)
- `UsersController.cs` - User management endpoints
- `CustomersController.cs` - Customer management endpoints
- `TicketsController.cs` - Ticket management endpoints
- `CallsController.cs` - Call logging endpoints
- `OrdersController.cs` - Order processing endpoints
- `ProductsController.cs` - Product management endpoints
- `WeatherForecastController.cs` - Sample weather endpoint (can be removed)

### 📁 Middleware
Custom middleware for cross-cutting concerns:
- `JwtMiddleware.cs` - JWT token validation
- `ErrorHandlerMiddleware.cs` - Global error handling

### 📁 Configuration
Configuration classes:
- `JwtSettings.cs` - JWT configuration settings
- `DatabaseSettings.cs` - Database connection settings

### 📁 Helpers
Utility classes:
- `PasswordHasher.cs` - Password hashing and verification
- `JwtHelper.cs` - JWT token generation and validation
- `NumberGenerator.cs` - Generate ticket and order numbers

### 📁 Data
Database context and migrations (to be created)

## Features

### 🔐 Authentication & Authorization
- JWT-based authentication
- Role-based access control (Agent, Supervisor, Admin)
- Password hashing with salt
- Token expiration handling

### 📞 Call Management
- Log inbound/outbound calls
- Track call duration and outcomes
- Associate calls with customers and agents
- Call escalation support

### 🎫 Ticket Management
- Create, update, and resolve tickets
- Assign tickets to agents
- Ticket categorization and priority levels
- Comment system for ticket updates
- Ticket status tracking

### 👥 Customer Management
- Customer profile management
- Interaction history tracking
- Customer search and filtering
- Customer type classification

### 📦 Order Processing
- Create and manage sales orders
- Product catalog integration
- Order status tracking
- Calculate order totals with taxes and discounts

### 📊 Product Management
- Product catalog with categories
- Inventory tracking
- Low stock alerts
- SKU generation

## API Endpoints

### Authentication
- `POST /api/auth/login` - User login
- `POST /api/auth/register` - User registration
- `POST /api/auth/change-password` - Change password
- `POST /api/auth/reset-password` - Reset password

### Users
- `GET /api/users` - Get all users
- `GET /api/users/{id}` - Get user by ID
- `POST /api/users` - Create new user
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

### Tickets
- `GET /api/tickets` - Get all tickets
- `GET /api/tickets/{id}` - Get ticket by ID
- `GET /api/tickets/agent/{agentId}` - Get tickets by agent
- `GET /api/tickets/customer/{customerId}` - Get tickets by customer
- `POST /api/tickets` - Create new ticket
- `PUT /api/tickets/{id}` - Update ticket
- `POST /api/tickets/{id}/assign` - Assign ticket to agent
- `POST /api/tickets/{id}/escalate` - Escalate ticket
- `POST /api/tickets/{id}/comments` - Add comment to ticket

### Calls
- `GET /api/calls` - Get all calls
- `GET /api/calls/{id}` - Get call by ID
- `GET /api/calls/agent/{agentId}` - Get calls by agent
- `GET /api/calls/customer/{customerId}` - Get calls by customer
- `POST /api/calls` - Create new call
- `POST /api/calls/{id}/start` - Start call
- `POST /api/calls/{id}/end` - End call

### Customers
- `GET /api/customers` - Get all customers
- `GET /api/customers/{id}` - Get customer by ID
- `GET /api/customers/{id}/interactions` - Get customer interaction history
- `POST /api/customers` - Create new customer
- `PUT /api/customers/{id}` - Update customer
- `GET /api/customers/search` - Search customers

### Orders
- `GET /api/orders` - Get all orders
- `GET /api/orders/{id}` - Get order by ID
- `GET /api/orders/agent/{agentId}` - Get orders by agent
- `GET /api/orders/customer/{customerId}` - Get orders by customer
- `POST /api/orders` - Create new order
- `PUT /api/orders/{id}` - Update order
- `PATCH /api/orders/{id}/status` - Update order status

### Products
- `GET /api/products` - Get all products
- `GET /api/products/active` - Get active products
- `GET /api/products/{id}` - Get product by ID
- `GET /api/products/low-stock` - Get low stock products
- `POST /api/products` - Create new product
- `PUT /api/products/{id}` - Update product
- `PATCH /api/products/{id}/stock` - Update product stock

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- SQL Server (or LocalDB for development)

### Installation
1. Clone the repository
2. Navigate to the project directory
3. Update connection string in `appsettings.json`
4. Run the following commands:

```bash
dotnet restore
dotnet build
dotnet run
```

### Configuration
Update the following settings in `appsettings.json`:
- `JwtSettings.Key` - Secret key for JWT token generation
- `DatabaseSettings.ConnectionString` - Database connection string

### Running the Application
The API will be available at `https://localhost:7123` (or as configured in `launchSettings.json`)

### Swagger Documentation
Once running, access Swagger UI at the root URL to explore and test the API endpoints.

## Security Features
- JWT-based authentication
- Password hashing with salt
- Role-based authorization
- CORS configuration
- Input validation
- Error handling middleware

## Next Steps
1. Implement service classes for business logic
2. Set up Entity Framework DbContext
3. Create database migrations
4. Add unit and integration tests
5. Implement logging and monitoring
6. Add API rate limiting
7. Create frontend application (Blazor WebAssembly)

## License
This project is licensed under the MIT License.
