# Daily Ledger - Expense Tracking Application

A comprehensive expense tracking application built with .NET 9.0, featuring a clean MVC architecture, SQLite database, and beautiful console interface using Spectre.Console.

## 🏗️ Architecture Overview

This application follows a strict **MVC (Model-View-Controller)** architecture pattern:

### **📁 Project Structure**

```
📁 Daily Ledger (Console Application)
├── 📄 README.md (This file)
├── 📄 Program.cs (View Layer - Console UI)
├── 📄 ledger.csproj (Project configuration)
├── 📁 Controllers/
│   └── 📄 ExpenseController.cs (Controller Layer - Business Logic)
├── 📁 Models/
│   ├── 📄 Expense.cs (Model - Expense data structure)
│   └── 📄 Category.cs (Model - Category data structure)
├── 📁 Data/
│   └── 📄 Database.cs (Data Access - Entity Framework context)
└── 📁 Services/
    ├── 📄 ExpenseService.cs (Data operations for expenses)
    ├── 📄 CategoryService.cs (Data operations for categories)
    ├── 📄 ExportImportService.cs (CSV import/export functionality)
    └── 📄 DatabaseInitializationService.cs (Database setup and seeding)
```

## 🎯 MVC Architecture Implementation

### **1. Models (M) - Data Layer**
- **Expense.cs**: Represents individual expense records with properties for amount, date, category, and notes
- **Category.cs**: Represents expense categories with name and relationships to expenses
- **Location**: `Models/` directory
- **Purpose**: Define the data structure and relationships

### **2. Controllers (C) - Business Logic Layer**
- **ExpenseController.cs**: Central controller handling all business operations
- **Location**: `Controllers/` directory
- **Responsibilities**:
  - Expense management (CRUD operations)
  - Category management (CRUD operations)
  - Analytics and reporting
  - Data import/export coordination
  - Database initialization

### **3. Views (V) - Presentation Layer**
- **Program.cs**: Console-based user interface using Spectre.Console
- **Location**: Root directory
- **Features**:
  - Interactive menu system
  - Beautiful table displays
  - User input handling
  - Error presentation

## 🚀 Features

### **Core Functionality**
- ✅ **Expense Management**: Add, view, and track daily expenses
- ✅ **Category System**: Organize expenses with predefined and custom categories
- ✅ **Data Analytics**: View spending summaries and category breakdowns
- ✅ **Data Export**: Export expense data to CSV format
- ✅ **Data Import**: Import expense data from CSV files
- ✅ **Settings Management**: Manage categories and application settings

### **Technical Features**
- ✅ **Local Database**: SQLite with Entity Framework Core for offline storage
- ✅ **Beautiful UI**: Spectre.Console for rich console interface
- ✅ **Error Handling**: Comprehensive exception handling throughout
- ✅ **Data Validation**: Input validation and business rule enforcement
- ✅ **Sample Data**: Pre-populated with sample expenses for demonstration

## 🛠️ Technology Stack

| Component | Technology | Purpose |
|-----------|------------|---------|
| **Backend** | .NET 9.0 | Core application framework |
| **Database** | SQLite + Entity Framework Core | Local data storage |
| **Architecture** | MVC Pattern | Clean separation of concerns |
| **UI Framework** | Spectre.Console | Beautiful console interface |
| **Dependency Injection** | Microsoft.Extensions.DI | Service management |
| **Build System** | .NET CLI | Project building and running |

## 📋 Prerequisites

- **.NET 9.0 SDK** or later
- **Windows/Linux/macOS** compatible
- **SQLite** (included via SQLitePCLRaw)

## 🚀 Getting Started

### **1. Clone and Setup**
```bash
git clone <repository-url>
cd DailyLedger
```

### **2. Restore Dependencies**
```bash
dotnet restore ledger.csproj
```

### **3. Build the Application**
```bash
dotnet build ledger.csproj
```

### **4. Run the Application**
```bash
dotnet run --project ledger.csproj
```

## 💡 Usage Guide

### **Main Menu Options**

1. **View all expenses**: Display all recorded expenses in a formatted table
2. **Add new expense**: Create a new expense with category selection
3. **View category summaries**: Show spending breakdown by category with percentages
4. **Export data to CSV**: Export all expense data to a CSV file
5. **Import data from CSV**: Import expense data from a CSV file
6. **View settings**: Display all available expense categories
7. **Clear all data**: Remove all expenses and reset to default categories

### **Adding Expenses**
1. Select "Add new expense" from the main menu
2. Choose an existing category or create a new one
3. Enter the expense amount, date, and optional notes
4. The expense is saved to the local database

### **Viewing Analytics**
1. Select "View category summaries" to see spending patterns
2. View total amounts and transaction counts by category
3. See percentage breakdown of spending

## 🗄️ Database Schema

### **Tables**

#### **Expenses Table**
| Column | Type | Description |
|--------|------|-------------|
| Id | INTEGER | Primary key, auto-increment |
| Amount | DECIMAL | Expense amount |
| Date | DATETIME | Date of expense |
| CategoryId | INTEGER | Foreign key to Categories |
| Notes | TEXT | Optional expense notes |

#### **Categories Table**
| Column | Type | Description |
|--------|------|-------------|
| Id | INTEGER | Primary key, auto-increment |
| Name | TEXT | Category name |

### **Default Categories**
- Food & Dining
- Transportation
- Entertainment
- Bills & Utilities
- Shopping
- Healthcare
- Education
- Travel
- Other

## 🔧 Configuration

### **Database Configuration**
- **Database File**: `Ledger.db` (created in application directory)
- **Connection**: SQLite with Entity Framework Core
- **Initialization**: Automatic database creation and seeding

### **Application Settings**
- **Currency**: Defaults to system currency formatting
- **Date Format**: yyyy-MM-dd
- **Categories**: Predefined set with ability to add custom ones

## 📊 Sample Data

The application includes sample expense data for demonstration:
- **Food & Dining**: ₹15.50 (Lunch at cafe)
- **Transportation**: ₹45.00 (Gas fill-up)
- **Entertainment**: ₹12.99 (Movie ticket)

## 🔒 Security & Privacy

- **Local Storage Only**: All data stored locally on device
- **No Cloud Sync**: Complete privacy and offline functionality
- **No External APIs**: Self-contained application
- **Data Export Control**: Users manually control data sharing

## 🚀 Deployment

### **Console Application**
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

### **Mobile Deployment** (Future)
The architecture supports easy migration to:
- **.NET MAUI** for cross-platform mobile apps
- **Xamarin.Forms** for iOS/Android development
- **Windows Forms** for desktop applications

## 🧪 Testing

### **Running Tests**
```bash
# Unit tests (when implemented)
dotnet test

# Application testing
dotnet run --project ledger.csproj
```

## 📈 Performance

- **Database**: Optimized for 10,000+ expense records
- **Memory**: Efficient LINQ queries with proper disposal
- **UI**: Responsive console interface with Spectre.Console
- **Storage**: Compact SQLite database format

## 🔮 Future Enhancements

### **Phase 2 - Advanced Features**
- [ ] Data visualization with charts and graphs
- [ ] Advanced filtering and search capabilities
- [ ] Budget tracking and alerts
- [ ] Recurring expense management

### **Phase 3 - Mobile App**
- [ ] .NET MAUI cross-platform mobile application
- [ ] Touch-optimized user interface
- [ ] Camera integration for receipt scanning
- [ ] Push notifications for budget alerts

### **Phase 4 - Advanced Features**
- [ ] Cloud synchronization (optional)
- [ ] Multi-device support
- [ ] Advanced reporting and analytics
- [ ] Third-party integrations

## 🐛 Troubleshooting

### **Common Issues**

**Build Errors:**
```bash
dotnet clean
dotnet restore ledger.csproj
dotnet build ledger.csproj
```

**Database Issues:**
- Delete `Ledger.db` file to reset database
- Check file permissions in application directory

**Runtime Errors:**
- Ensure .NET 9.0 SDK is installed
- Check SQLitePCLRaw bundle initialization

## 📝 Development Notes

### **Code Organization**
- **Controllers**: Handle business logic and coordinate between Models and Views
- **Services**: Data access layer and utility functions
- **Models**: Pure data structures without business logic
- **Views**: User interface and presentation logic

### **Best Practices Implemented**
- ✅ Dependency Injection pattern
- ✅ Async/await for all database operations
- ✅ Proper error handling and logging
- ✅ Input validation and sanitization
- ✅ Clean separation of concerns
- ✅ Consistent coding standards

## 📄 License

This project is developed as a demonstration of clean architecture principles and modern .NET development practices.

## 👥 Contributing

When contributing to this project:
1. Follow the established MVC architecture
2. Add appropriate error handling
3. Include XML documentation for public methods
4. Test all new features thoroughly
5. Update this README for significant changes

---

**Built with ❤️ using .NET 9.0, Entity Framework Core, and Spectre.Console**
