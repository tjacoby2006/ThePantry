# Project Plan: ThePantry

## Completed Tasks
- [x] Initial project setup (Blazor Server, EF Core SQLite, MediatR)
- [x] Domain models (`InventoryItem`, `ScanQueueItem`, `UsageHistory`)
- [x] Application layer (Commands/Queries for Inventory and Scanning)
- [x] OpenFoodFacts integration for product lookup
- [x] Background service for processing scanned UPCs
- [x] Basic UI Pages (`Dashboard`, `Inventory`, `AddItems`, `ScanMonitor`, `ShoppingList`)
- [x] Implement Edit and Delete for Inventory Items
- [x] Add filtering and sorting to Inventory page
- [x] Enhance Shopping List with "Mark as Purchased" functionality

## Current Status
- Inventory management is now more robust with edit/delete and sorting.
- Shopping list allows updating inventory when items are bought.

## Next Steps

### 1. Improve UI/UX
- [x] Add toast notifications for actions (e.g., "Item added").
- [x] Improve the `ScanMonitor` with real-time updates (SignalR or polling).
- [x] Add a "Low Stock" widget to the Dashboard.

### 2. Data Persistence & Export
- [ ] Add export to CSV/Excel for inventory.
- [ ] Implement database backup/restore.

### 3. Advanced Features
- [ ] Add "Manual Addition" to Shopping List for items not in inventory.
- [ ] Implement usage history tracking (when items are decremented).
