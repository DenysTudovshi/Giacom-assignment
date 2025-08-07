# Giacom Tech Test - COMPLETED ✅

## Background
Giacom Cloud Market is a B2B e-commerce platform which allows IT companies (resellers) to buy services indirectly from major vendors (Microsoft, Symantec, Webroot etc) in high volumes at low cost. IT companies then resell the purchased services on to their customers, making a small margin. Behind Cloud Market are several microservices, one of which is an Order API much like the one implemented in this project.

## Concepts
* Reseller = A customer of Giacom
* Customer = A customer of a Reseller
* Order = An order placed by a Reseller for a specific Customer
* Order Item = A service and product which belongs to an Order
* Order Status = The current state of an Order
* Product = An end-offering which can be purchased e.g. '100GB Mailbox'
* Service = The category the Product belongs to e.g. 'Email'
* Profit = The difference between Cost and Price

## Implementation Overview
This project has been fully implemented with all requested features and additional enhancements:

### ✅ Completed Tasks
All four required API endpoints have been successfully implemented:

1. **Get Orders by Status** - `GET /orders/status/{statusName}`
2. **Update Order Status** - `PATCH /orders/{orderId}/status`
3. **Create New Order** - `POST /orders`
4. **Calculate Profit by Month** - `GET /orders/profit/monthly`

### 🚀 Additional Features Implemented

- **Global Exception Handling**: Comprehensive error handling middleware that catches and properly formats all unhandled exceptions
- **FluentValidation**: Robust input validation for all API endpoints with detailed error messages
- **Swagger/OpenAPI Documentation**: Complete API documentation accessible via `/swagger` endpoint
- **Comprehensive Logging**: Structured logging throughout the application

## API Endpoints

### 1. Get All Orders
- **Endpoint**: `GET /orders`
- **Description**: Retrieves all orders in the system
- **Response**: List of order summaries

### 2. Get Order by ID
- **Endpoint**: `GET /orders/{orderId}`
- **Description**: Retrieves a specific order by its unique identifier
- **Parameters**: 
  - `orderId` (Guid): The unique identifier of the order
- **Response**: Detailed order information or 404 if not found

### 3. Get Orders by Status
- **Endpoint**: `GET /orders/status/{statusName}`
- **Description**: Retrieves all orders with the specified status
- **Parameters**: 
  - `statusName` (string): Status name (e.g., 'Pending', 'Processing', 'Completed', 'Failed')
- **Validation**: Status name is validated against available order statuses
- **Response**: List of orders matching the specified status

### 4. Create New Order
- **Endpoint**: `POST /orders`
- **Description**: Creates a new order with specified items
- **Request Body**: 
  ```json
  {
    "resellerId": "guid",
    "customerId": "guid",
    "items": [
      {
        "productId": "guid",
        "serviceId": "guid",
        "quantity": 1
      }
    ]
  }
  ```
- **Validation**: 
  - All GUIDs must be valid
  - Items collection cannot be empty
  - Quantity must be positive
  - ResellerId and CustomerId are required
- **Response**: Created order ID and confirmation message

### 5. Update Order Status
- **Endpoint**: `PATCH /orders/{orderId}/status`
- **Description**: Updates the status of an existing order
- **Parameters**: 
  - `orderId` (Guid): The unique identifier of the order
- **Request Body**: 
  ```json
  {
    "statusName": "Processing"
  }
  ```
- **Validation**: Status name is validated against available order statuses
- **Response**: Confirmation message or 404 if order not found

### 6. Calculate Profit by Month
- **Endpoint**: `GET /orders/profit/monthly`
- **Description**: Calculates profit by month for all completed orders
- **Query Parameters** (optional): 
  - `year` (int): Filter by specific year (e.g., 2024)
  - `month` (int): Filter by specific month (1-12)
- **Validation**: 
  - Year must be between 2020-2030 if provided
  - Month must be between 1-12 if provided
  - Month requires year to be specified
- **Response**: Monthly profit data including total profit and order count

## Pre-Requisites
* Visual Studio 2022 (or compatible IDE for working with .NET)
* .NET 8.0 SDK
* Git
* Docker (running Linux containers)
* Optional: MySQL Workbench / Heidi (database client)
* Optional: Postman (can also use any other API client)

## How to Run the Project

### Option 1: Development Mode
1. Clone this repository locally
2. Using a terminal, cd to the local repository and run `docker-compose up db` to start and seed the database
3. Open the solution file in `/src`
4. Start debugging or run the Order.WebAPI project
5. The API will be available at `http://localhost:8000`
6. Access Swagger documentation at `http://localhost:8000/swagger`

### Option 2: Full Docker Deployment (Recommended for Testing)
1. Clone this repository locally
2. Using a terminal, cd to the local repository
3. Run `docker-compose up` to build and start both the database and API service
4. The API will be available at `http://localhost:8000`
5. Access Swagger documentation at `http://localhost:8000/swagger`

### Testing the API
- **Browser**: Navigate to `http://localhost:8000/orders` to see all orders
- **Swagger UI**: Go to `http://localhost:8000/swagger` for interactive API documentation
- **Postman/API Client**: Import the endpoints and test all functionality

### Available Test Endpoints
- `GET /orders` - Get all orders
- `GET /orders/{orderId}` - Get specific order
- `GET /orders/status/{statusName}` - Get orders by status (try: Pending, Processing, Completed, Failed)
- `POST /orders` - Create new order
- `PATCH /orders/{orderId}/status` - Update order status
- `GET /orders/profit/monthly` - Get profit by month (with optional year/month filters)

## Technical Implementation Details

### Architecture
- **Clean Architecture**: Separated into Data, Service, Model, and WebAPI layers
- **Repository Pattern**: Abstracted data access through IOrderRepository
- **Dependency Injection**: Full DI container configuration
- **Entity Framework**: Code-first approach with MySQL provider

### Validation Features
- **FluentValidation**: Comprehensive input validation with custom rules
- **Automatic Validation**: Pipeline automatically validates requests
- **Detailed Error Messages**: Clear validation feedback for API consumers

### Error Handling
- **Global Exception Middleware**: Catches all unhandled exceptions
- **Structured Error Responses**: Consistent error format across all endpoints
- **Logging**: Comprehensive error logging with structured data

### API Documentation
- **Swagger/OpenAPI**: Complete API documentation with examples
- **XML Comments**: Detailed endpoint descriptions
- **Interactive Testing**: Built-in API testing interface

## Database Connection
To connect to the MySQL database directly, use these credentials:
* **Hostname**: localhost
* **Username**: order-service
* **Password**: nmCsdkhj20n@Sa*
* **Database**: orders

## Troubleshooting
If you experience issues with Docker containers:
1. Try deselecting Hyper-V Services in "Windows Features" (Search for Windows Features in Start Menu)
2. Select it again and restart your computer
3. Ensure Docker is running Linux containers mode

For database connection issues:
- Ensure the database container is running: `docker-compose up db`
- Check connection string in `appsettings.json`
- Verify MySQL container logs: `docker logs <container_name>`

Copyright (c) 2025, Giacom.
