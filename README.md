# ThePantry

A Blazor Server application for managing your home pantry, inventory, and shopping lists. It features UPC scanning integration with OpenFoodFacts for easy item entry.

## Features

- **Inventory Management**: Track items, quantities, and expiration dates.
- **UPC Scanning**: Integration with OpenFoodFacts to automatically fetch product details.
- **Shopping List**: Automatically track low-stock items and mark them as purchased.
- **Scan Monitor**: Background processing of scanned items.
- **Dashboard**: Quick overview of your pantry status.

## Tech Stack

- **Frontend**: Blazor Server (.NET 9/10)
- **Database**: Entity Framework Core with SQLite
- **Patterns**: MediatR for CQRS (Commands/Queries)
- **Background Tasks**: Hosted Services for scan processing

## Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- [Docker](https://www.docker.com/get-started) (optional, for containerized deployment)

### Local Development

1. Clone the repository.
2. Navigate to the project directory:
   ```bash
   cd ThePantry
   ```
3. Run the application:
   ```bash
   dotnet run --project ThePantry/ThePantry.csproj
   ```
4. Open your browser to `https://localhost:5001` or `http://localhost:5000`.

## Docker Deployment

You can run the application using Docker Compose:

```bash
docker-compose up -d
```

The application will be available at `http://localhost:8080`.

## Project Structure

- `ThePantry/Domain`: Core entities (`InventoryItem`, `ProductSku`, etc.)
- `ThePantry/Application`: Business logic, Commands, Queries, and Services.
- `ThePantry/Data`: EF Core DbContext and Migrations.
- `ThePantry/Pages`: Blazor components and UI logic.
- `ThePantry/Services`: Background workers and utility services.
