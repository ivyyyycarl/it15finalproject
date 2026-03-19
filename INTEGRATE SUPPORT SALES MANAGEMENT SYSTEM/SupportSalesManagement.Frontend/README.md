# Support & Sales Management System - Frontend

A modern Blazor WebAssembly frontend for the Support & Sales Management System, built with Tailwind CSS for a responsive and beautiful user interface.

## Features

### 🔐 Authentication & Authorization
- User login and registration
- JWT-based authentication
- Role-based access control (Agent, Supervisor, Admin)
- Secure session management with local storage

### 📊 Dashboard
- Real-time statistics overview
- Recent tickets and orders display
- Key performance indicators
- Interactive charts and metrics

### 🎫 Ticket Management
- Create, view, and edit support tickets
- Filter by status, priority, and search
- Assign tickets to agents
- Add comments and updates
- Ticket status tracking

### 👥 Customer Management
- Comprehensive customer profiles
- Search and filter customers
- Customer interaction history
- Customer type classification
- Active/inactive status management

### 📦 Order Management
- Create and manage sales orders
- Order status tracking
- Customer order history
- Order detail management
- Date range filtering

### 🏷️ Product Management
- Product catalog management
- Inventory tracking
- Low stock alerts
- Product categorization
- Active/inactive product status

## Technology Stack

- **Frontend Framework**: Blazor WebAssembly (.NET 8.0)
- **UI Framework**: Tailwind CSS
- **Authentication**: JWT with Blazored.LocalStorage
- **HTTP Client**: HttpClient with API integration
- **State Management**: Blazor services and dependency injection

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- Node.js (for Tailwind CSS build process)
- The backend API running on `https://localhost:7123`

### Installation

1. Clone the repository
2. Navigate to the frontend directory:
   ```bash
   cd "SupportSalesManagement.Frontend"
   ```

3. Install dependencies:
   ```bash
   dotnet restore
   npm install
   ```

4. Build Tailwind CSS:
   ```bash
   npm run build-css-prod
   ```

5. Run the application:
   ```bash
   dotnet run
   ```

### Development

For development with live CSS updates:

```bash
npm run build-css
```

This will start Tailwind CSS in watch mode, automatically updating styles when you make changes.

## Project Structure

```
SupportSalesManagement.Frontend/
├── Models/                 # Data models and DTOs
│   ├── User.cs
│   ├── Customer.cs
│   ├── Ticket.cs
│   └── Order.cs
├── Services/               # Business logic services
│   ├── ApiClient.cs
│   ├── AuthenticationService.cs
│   └── CustomAuthenticationStateProvider.cs
├── Pages/                  # Razor pages
│   ├── Login.razor
│   ├── Register.razor
│   ├── Dashboard.razor
│   ├── Tickets.razor
│   ├── Customers.razor
│   ├── Orders.razor
│   └── Products.razor
├── Shared/                 # Shared components
│   ├── MainLayout.razor
│   └── RedirectToLogin.razor
├── wwwroot/
│   ├── css/
│   │   └── app.css         # Tailwind CSS styles
│   └── index.html
├── Program.cs              # Application configuration
└── SupportSalesManagement.Frontend.csproj
```

## Configuration

### API Endpoint
Update the base URL in `Program.cs` to match your backend API:

```csharp
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7123") });
```

### Tailwind CSS Configuration
- `tailwind.config.js`: Tailwind configuration
- `postcss.config.js`: PostCSS configuration
- `package.json`: Build scripts and dependencies

## Authentication Flow

1. **Login**: User submits credentials to `/api/auth/login`
2. **Token Storage**: JWT token stored in local storage
3. **Authorization**: Token sent with API requests
4. **State Management**: Authentication state managed through Blazor services

## Key Features

### Responsive Design
- Mobile-first approach
- Responsive navigation
- Adaptive layouts for all screen sizes

### Modern UI/UX
- Clean and intuitive interface
- Consistent design language
- Smooth transitions and animations
- Loading states and error handling

### Data Management
- Real-time data synchronization
- Optimistic UI updates
- Efficient data fetching
- Caching strategies

## Security Features

- JWT token validation
- Secure local storage usage
- Route-based authorization
- Input validation and sanitization
- XSS protection

## Performance Optimization

- Lazy loading of components
- Efficient data fetching
- Minimal bundle size
- Optimized CSS with Tailwind purging

## Browser Support

- Chrome (latest)
- Firefox (latest)
- Safari (latest)
- Edge (latest)

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## License

This project is licensed under the MIT License.
