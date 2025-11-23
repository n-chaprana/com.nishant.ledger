# Daily Ledger - Expense Tracking Application

A comprehensive expense tracking application built with .NET 9.0, featuring a clean MVC architecture, SQLite database, and beautiful console interface using Spectre.Console.

## 🏗️ Architecture Overview

This application follows a strict **MVC (Model-View-Controller)** architecture pattern:

### **📁 Project Structure**

```
📁 Daily Ledger (Console Application)
├── 📄 README.md (This file - Project documentation)
├── 📄 LICENSE (MIT License)
├── 📄 LICENSE-ANALYSIS.md (OSS compliance report)
├── 📄 Program.cs (View Layer - Console UI)
├── 📄 ledger.csproj (Project configuration)
├── 📄 Ledger.sln (Solution file)
├── 📄 .gitignore (Git ignore rules)
├── 📁 Controllers/
│   └── 📄 ExpenseController.cs (Controller Layer - Business Logic)
├── 📁 Models/
│   ├── 📄 Expense.cs (Model - Expense data structure)
│   ├── 📄 Category.cs (Model - Category data structure)
│   └── 📄 CategorySummary.cs (Model - Category summary DTO)
├── 📁 Data/
│   └── 📄 Database.cs (Data Access - Entity Framework context)
├── 📁 Services/
│   ├── 📄 ExpenseService.cs (Data operations for expenses)
│   ├── 📄 CategoryService.cs (Data operations for categories)
│   ├── 📄 ExportImportService.cs (CSV import/export functionality)
│   └── 📄 DatabaseInitializationService.cs (Database setup and seeding)
└── 📁 Tests/ (Test Project - 139 comprehensive tests)
    ├── 📄 Ledger.Tests.csproj (Test project configuration)
    ├── 📄 README.md (Test suite documentation)
    ├── 📄 COVERAGE-REPORT.md (Coverage analysis guide)
    ├── 📄 CURRENT-COVERAGE.md (Latest coverage metrics)
    ├── 📁 Controllers/
    │   └── 📄 ExpenseControllerTests.cs (26 controller tests)
    ├── 📁 Services/
    │   ├── 📄 CategoryServiceTests.cs (17 category tests)
    │   ├── 📄 ExpenseServiceTests.cs (24 expense tests)
    │   ├── 📄 ExportImportServiceTests.cs (11 CSV tests)
    │   ├── 📄 EdgeCaseTests.cs (22 edge case tests)
    │   ├── 📄 ServiceCoverageTests.cs (20 coverage tests)
    │   └── 📄 AdditionalCoverageTests.cs (19 branch tests)
    └── 📁 TestHelpers/
        └── 📄 TestDbContextFactory.cs (Test database helper)
```

## 🎯 MVC Architecture Implementation

### **1. Models (M) - Data Layer**
**Location**: `Models/` directory

**Purpose**: Define the data structure and domain entities

**Components**:
- **Expense.cs**: Core expense entity
  - Properties: Id, Amount, Date, CategoryId, Notes
  - Relationships: Many-to-One with Category
  - Validation: Amount > 0, Date <= Today
  
- **Category.cs**: Expense category entity
  - Properties: Id, Name
  - Relationships: One-to-Many with Expenses
  - Constraints: Unique category names
  
- **CategorySummary.cs**: Data Transfer Object (DTO)
  - Properties: CategoryId, CategoryName, TotalAmount, ExpenseCount, Percentage
  - Purpose: Aggregated category analytics

**Design Principles**:
- ✅ Pure data structures (no business logic)
- ✅ Entity Framework Core navigation properties
- ✅ Data annotations for validation
- ✅ Immutable where appropriate

### **2. Controllers (C) - Business Logic Layer**
**Location**: `Controllers/` directory

**Purpose**: Orchestrate business operations and coordinate between layers

**Components**:
- **ExpenseController.cs**: Central business logic coordinator
  - **Expense Operations**: Add, Update, Delete, Get, GetAll, GetByDateRange
  - **Category Operations**: Add, Update, Delete, Get, GetAll
  - **Analytics**: GetCategorySummaries, GetTotalSpent, GetTotalSpentByDateRange
  - **Data Management**: ExportToCsv, ImportFromCsv, ClearAllData
  - **Initialization**: InitializeDatabase with default categories

**Responsibilities**:
- ✅ Business rule enforcement
- ✅ Service coordination
- ✅ Transaction management
- ✅ Error handling and validation
- ✅ Data transformation (Entity ↔ DTO)

**Design Patterns**:
- ✅ Dependency Injection for services
- ✅ Async/await for all operations
- ✅ Result pattern for operation outcomes
- ✅ Single Responsibility Principle

### **3. Services (S) - Data Access Layer**
**Location**: `Services/` directory

**Purpose**: Encapsulate data access and utility operations

**Components**:
- **ExpenseService.cs**: Expense data operations
  - CRUD operations for expenses
  - Querying and filtering
  - Business rule validation
  - Database transactions
  
- **CategoryService.cs**: Category data operations
  - CRUD operations for categories
  - Duplicate detection
  - Cascade delete prevention
  - Default category initialization
  
- **ExportImportService.cs**: Data import/export
  - CSV export with proper escaping
  - CSV import with validation
  - CSV injection protection
  - Error reporting
  
- **DatabaseInitializationService.cs**: Database setup
  - Schema creation
  - Default data seeding
  - Migration management

**Design Principles**:
- ✅ Repository pattern implementation
- ✅ Unit of Work pattern
- ✅ Separation of concerns
- ✅ Testability (interface-based)

### **4. Views (V) - Presentation Layer**
**Location**: Root directory (`Program.cs`)

**Purpose**: User interface and interaction

**Components**:
- **Program.cs**: Console-based UI using Spectre.Console
  - Interactive menu system
  - Beautiful table displays
  - User input handling
  - Error presentation
  - Data visualization

**Features**:
- ✅ Rich console formatting
- ✅ Color-coded output
- ✅ Table-based data display
- ✅ Input validation
- ✅ User-friendly error messages

### **5. Data Layer**
**Location**: `Data/` directory

**Purpose**: Database context and configuration

**Components**:
- **Database.cs (LedgerContext)**: Entity Framework Core DbContext
  - DbSet<Expense> Expenses
  - DbSet<Category> Categories
  - Model configuration
  - Relationship mapping
  - SQLite connection management

**Configuration**:
- ✅ Entity relationships (One-to-Many)
- ✅ Cascade delete rules
- ✅ Index optimization
- ✅ Connection string management

### **6. Test Layer**
**Location**: `Tests/` directory

**Purpose**: Comprehensive test coverage for all layers

**Test Organization**:
```
Tests/
├── Controllers/
│   └── ExpenseControllerTests.cs (26 tests)
│       - All controller methods tested
│       - Integration with services
│       - Error handling validation
│
├── Services/
│   ├── CategoryServiceTests.cs (17 tests)
│   │   - CRUD operations
│   │   - Validation rules
│   │   - Edge cases
│   │
│   ├── ExpenseServiceTests.cs (24 tests)
│   │   - CRUD operations
│   │   - Date range queries
│   │   - Amount calculations
│   │
│   ├── ExportImportServiceTests.cs (11 tests)
│   │   - CSV export/import
│   │   - Special character handling
│   │   - Security (CSV injection)
│   │
│   ├── EdgeCaseTests.cs (22 tests)
│   │   - Boundary conditions
│   │   - Large datasets
│   │   - Concurrent operations
│   │
│   ├── ServiceCoverageTests.cs (20 tests)
│   │   - Enhanced coverage
│   │   - Database operations
│   │   - Validation scenarios
│   │
│   └── AdditionalCoverageTests.cs (19 tests)
│       - Branch coverage
│       - Error paths
│       - CSV edge cases
│
└── TestHelpers/
    └── TestDbContextFactory.cs
        - In-memory database setup
        - Test isolation
        - Cleanup utilities
```

**Test Characteristics**:
- ✅ **139 comprehensive tests** (100% passing)
- ✅ **AAA Pattern**: Arrange-Act-Assert
- ✅ **Isolation**: Each test uses unique in-memory database
- ✅ **Fast**: ~1.8 seconds for full suite
- ✅ **Coverage**: ~88% business logic coverage
- ✅ **Security**: CSV injection protection tested
- ✅ **Edge Cases**: Boundary conditions validated

### **Architecture Benefits**

**Separation of Concerns**:
- ✅ Each layer has a single, well-defined responsibility
- ✅ Changes in one layer don't affect others
- ✅ Easy to understand and maintain

**Testability**:
- ✅ Dependency injection enables easy mocking
- ✅ In-memory database for fast tests
- ✅ Isolated test cases
- ✅ High code coverage

**Maintainability**:
- ✅ Clear code organization
- ✅ Consistent patterns throughout
- ✅ Well-documented code
- ✅ Easy to extend

**Scalability**:
- ✅ Ready for migration to mobile (MAUI)
- ✅ Can add new features without refactoring
- ✅ Database-agnostic design
- ✅ Service layer can be exposed as API

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

## 🧪 Testing & Code Coverage

### **Test Suite Overview**
The project includes a comprehensive test suite with **139 tests** covering all layers of the application.

**Test Statistics:**
- ✅ **Total Tests**: 139
- ✅ **Pass Rate**: 100% (139/139)
- ✅ **Execution Time**: ~1.8 seconds
- ✅ **Test Organization**: 7 test classes

### **Running Tests**
```bash
# Run all tests
dotnet test Ledger.sln

# Run tests with detailed output
dotnet test Ledger.sln --verbosity detailed

# Run tests with code coverage
dotnet test Ledger.sln /p:CollectCoverage=true
```

### **Code Coverage Metrics**

#### **Overall Coverage**
| Metric | Value | Status |
|--------|-------|--------|
| **Line Coverage** | **~50%** | ⚠️ Includes UI layer (0%) |
| **Business Logic Coverage** | **~88%** | ✅ Excellent |
| **Branch Coverage** | **~45%** | ⚠️ Includes UI layer |
| **Function Coverage** | **~85%** | ✅ Excellent |

#### **Coverage by Layer**

**Models Layer: 93.3%** ✅
- Category.cs: 100%
- Expense.cs: 100%
- CategorySummary.cs: 80%

**Services Layer: ~85%** ✅
- CategoryService: ~92%
- ExpenseService: ~85%
- ExportImportService: ~85%

**Controllers Layer: ~85%** ✅
- ExpenseController: ~85%
- All CRUD operations tested
- Error paths validated

**UI Layer (Program.cs): 0%** ⚠️
- Console UI tested manually
- Not included in unit tests (expected)

#### **Why Overall Coverage Appears Lower**
The overall coverage of ~50% includes the UI layer (Program.cs) which has 0% coverage. This is **expected and acceptable** for console applications:
- **Business Logic**: ~88% coverage ✅
- **UI Code**: 0% coverage (tested manually)
- **Combined**: ~50% overall

**Excluding UI, the business logic has excellent coverage at ~88%.**

### **Test Distribution**
1. **CategoryServiceTests**: 17 tests - Category CRUD operations
2. **ExpenseServiceTests**: 24 tests - Expense management
3. **ExportImportServiceTests**: 11 tests - CSV operations
4. **ExpenseControllerTests**: 26 tests - Controller layer
5. **EdgeCaseTests**: 22 tests - Boundary conditions
6. **ServiceCoverageTests**: 20 tests - Enhanced coverage
7. **AdditionalCoverageTests**: 19 tests - Branch coverage

### **Test Coverage Details**

**What IS Tested (✅):**
- ✅ All CRUD operations (Create, Read, Update, Delete)
- ✅ Input validation (empty, null, duplicates, amounts, dates)
- ✅ Business rules (amount > 0, no future dates, cascade deletes)
- ✅ Error handling and messaging
- ✅ CSV import/export with edge cases
- ✅ Special character handling
- ✅ Security (CSV injection protection)
- ✅ Concurrent operations
- ✅ Large datasets (150+ records)
- ✅ Decimal precision
- ✅ Database relationships

**What is NOT Tested (⚠️):**
- ⚠️ UI layer (Program.cs) - tested manually
- ⚠️ Database configuration (uses in-memory DB in tests)

### **Test Quality Metrics**
- ✅ **AAA Pattern**: All tests follow Arrange-Act-Assert
- ✅ **Isolation**: Each test uses unique in-memory database
- ✅ **Fast Execution**: 139 tests in ~1.8 seconds
- ✅ **Comprehensive**: Edge cases, error paths, security
- ✅ **Maintainable**: Well-organized, documented

### **Viewing Coverage Reports**
```bash
# Generate coverage report
dotnet test Ledger.sln /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# View detailed coverage analysis
# See Tests/COVERAGE-REPORT.md for comprehensive analysis
# See Tests/CURRENT-COVERAGE.md for latest metrics
```

### **Test Documentation**
- **Tests/README.md**: Complete test suite documentation
- **Tests/COVERAGE-REPORT.md**: Coverage analysis guide
- **Tests/CURRENT-COVERAGE.md**: Latest coverage metrics

### **Production Readiness**
✅ **EXCELLENT** - The application has enterprise-grade test coverage:
- 139 comprehensive tests
- 100% pass rate
- ~88% business logic coverage
- All critical paths tested
- Security measures verified
- Fast test execution

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

## 📄 License & Compliance

### **Application License**
This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

### **OSS License Analysis**
✅ **LICENSE STATUS: CLEAN** - Comprehensive analysis completed with no issues found.

**Key Findings:**
- ✅ **No Security Vulnerabilities**: All dependencies scanned and verified
- ✅ **Permissive Licenses Only**: MIT and Apache 2.0 licenses throughout
- ✅ **Commercial Ready**: Safe for commercial and open-source distribution
- ✅ **No Copyleft Risk**: No GPL or viral license contamination

**Detailed Analysis**: See [LICENSE-ANALYSIS.md](LICENSE-ANALYSIS.md) for comprehensive license compliance report.

### **Third-Party Dependencies**
| Package | License | Usage |
|---------|---------|-------|
| Microsoft.EntityFrameworkCore.Sqlite | MIT | Database operations |
| Spectre.Console | MIT | Console user interface |
| SQLitePCLRaw.bundle_green | Apache 2.0 | SQLite database engine |
| System.Text.Json | MIT | JSON serialization |

**All dependencies use permissive licenses compatible with commercial distribution.**

## 👥 Contributing

When contributing to this project:
1. Follow the established MVC architecture
2. Add appropriate error handling
3. Include XML documentation for public methods
4. Test all new features thoroughly
5. Update this README for significant changes

---

**Built with ❤️ using .NET 9.0, Entity Framework Core, and Spectre.Console**
