# ThePantry

A completely vibe coded/AI slopped Blazor Server application for managing your home pantry, inventory, and shopping lists- all without unit tests. It features UPC scanning integration with OpenFoodFacts for easy item entry.

## Features

- **Inventory Management**: Track items, quantities, and expiration dates.
- **UPC Scanning**: Scan UPCs from your phone (with a satisfying ding) and integrate with OpenFoodFacts to automatically fetch product details.
- **Shopping List**: Automatically track low-stock items and mark them as purchased.
- **Scan Monitor**: Background processing of scanned items.
- **Dashboard**: Quick overview of your pantry status.
- **Combine Products**: Buying different brands of the same product? Combine them under a single name with multiple SKUs.

## Tech Stack

- **Frontend**: Blazor Server (.NET 10)
- **Database**: Entity Framework Core with SQLite
- **Patterns**: MediatR for CQRS (Commands/Queries)
- **Background Tasks**: Hosted Services for scan processing

## Docker Deployment

You can run the application using Docker Compose. An example configuration is provided in `compose.example.yml`.

1. Clone the repository from your target docker folder:
   ```bash
   git clone https://github.com/tjacoby2006/ThePantry ThePantry
   ```
2. Copy the example compose and env files and adjust as needed:
   ```bash
   cp ThePantry/compose.example.yml compose.yml
   cp ThePantry/.env .env
   ```
3. Run the container:
   ```bash
   docker compose up -d
   ```
4. Open your browser to `https://localhost:8080`.

### Updating

You can update easily using the following commands from the docker compose folder:

```bash
cd ThePantry
git pull
cd ..
docker compose build
docker compose down
docker compose up -d
```