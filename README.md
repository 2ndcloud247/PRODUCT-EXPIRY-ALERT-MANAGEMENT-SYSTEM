# Product Expiry Alert Management System

**Design and Implementation of a Product Expiry Alert Management System**

| | |
|---|---|
| **Name** | Iderima Lawrentta Onosedeba |
| **Matric No.** | IAUE/2025/PGD/0029 |
| **Department** | Computer Science |
| **Programme** | Postgraduate Diploma (PGD), Information Technology option |
| **School** | Ignatius Ajuru University of Education, Rumuolumini, Port Harcourt |

---

## 1. What the system does

Shops, pharmacies and supermarkets lose money, and can put customers at risk, when products expire on the shelf without anyone noticing. This desktop application:

1. **Records** every product or batch with its quantity, price, supplier, location and **expiry date**.
2. **Automatically checks** every product and sorts it into one of four groups:

   | Status | Rule (default) | Colour | What to do |
   |---|---|---|---|
   | **Expired** | expiry date has passed | Red | Remove from sale now |
   | **Critical** | expires within **7** days | Orange | Sell first, discount, or return to supplier |
   | **Expiring Soon** | expires within **30** days | Yellow | Plan to move the stock |
   | **Safe** | more than 30 days left | Green | No action needed |

3. **Alerts the user** in three ways:
   - a pop-up summary when the program opens
   - a Windows notification near the clock when a new alert appears (it checks every 30 minutes)
   - a red alert counter at the top of every screen and on the Dashboard
4. **Produces reports** that can be printed or exported to Excel.
5. **Protects the data** with user accounts, roles (Admin / Staff), hashed passwords and an activity log.

---

## 2. How to run it

### Option A: Visual Studio (recommended)
1. Open **`PRODUCT EXPIRY ALERT MANAGEMENT SYSTEM.slnx`** in Visual Studio 2022 or newer.
2. Press **F5** (Start). Visual Studio downloads the SQLite package automatically the first time.

### Option B: Command line
```bash
dotnet run --project "PRODUCT EXPIRY ALERT MANAGEMENT SYSTEM/PRODUCT EXPIRY ALERT MANAGEMENT SYSTEM.csproj"
```

### First login
| Username | Password |
|---|---|
| `admin` | `admin123` |

Change this password after logging in: **Settings > Change my password**.

On the first run, the system creates the database and adds **sample products** (some expired, some critical, some safe) so the alerts can be demonstrated straight away. You can edit or delete them.

### Requirements
- Windows 10 or 11
- .NET Framework 4.7.2 or newer (already included in Windows 10/11)
- No database server is needed. SQLite is built in.

---

## 3. Screens (modules)

| Screen | Purpose |
|---|---|
| **Login** | Only registered users can enter. Locks after 5 failed attempts. |
| **Dashboard** | Summary cards (Total / Expired / Critical / Expiring Soon / Safe), "needs attention" table, doughnut chart, value of expired stock and value at risk. Click a card to open the matching products. |
| **Products** | Add, edit, delete, search and filter products. Rows are coloured by status. **Mark as Disposed** keeps a record of an expired item that was removed. |
| **Alerts** | All alerts raised by the system. **Acknowledge** them after taking action, or **Dispose** the product. **Check Now** runs the check immediately. |
| **Reports** | 1. Expired products, 2. Products expiring in a date range, 3. Full stock status, 4. Disposed products (losses), 5. Summary by category. **Print** (with preview) or **Export** to Excel (CSV). |
| **Categories & Suppliers** | Manage the lists used when adding products. |
| **Users** *(Admin)* | Create staff accounts, change roles, disable accounts, reset passwords. |
| **Activity Log** *(Admin)* | Audit trail: who did what and when. |
| **Settings** | Change the Critical / Expiring Soon days, how often to check, the business name on reports, your password, and **back up the database**. |
| **Help & About** | Built-in user guide, explanation of the alert engine, and project details. |

---

## 4. System design

### 4.1 Architecture (three layers)

```
+-----------------------------------------------------------+
|  PRESENTATION LAYER  (Forms/, Views/, UI/)                 |
|  LoginForm, MainForm, DashboardView, ProductsView, ...     |
+-----------------------------+-----------------------------+
                              |
+-----------------------------v-----------------------------+
|  BUSINESS LOGIC LAYER  (Services/)                         |
|  AlertService (alert engine), ProductService, UserService, |
|  SettingsService, LookupService, ActivityLogService        |
+-----------------------------+-----------------------------+
                              |
+-----------------------------v-----------------------------+
|  DATA LAYER  (Data/Database.cs)                            |
|  SQLite database file: expiry_alert.db                     |
+-----------------------------------------------------------+
```

### 4.2 Project structure

```
PRODUCT EXPIRY ALERT MANAGEMENT SYSTEM/
├── Program.cs                  Entry point: prepare DB > Login > Main window
├── Data/
│   ├── Database.cs             Connection, table creation (schema), helpers
│   └── SampleData.cs           Demo data inserted on the first run
├── Models/Models.cs            Product, User, AlertRecord, ExpiryStatus, Session
├── Services/
│   ├── AlertService.cs         THE ALERT ENGINE: status rules, alert generation
│   ├── ProductService.cs       Product CRUD (create, read, update, delete)
│   ├── UserService.cs          Login, accounts, password hashing
│   ├── SettingsService.cs      Configurable values
│   ├── LookupService.cs        Categories and suppliers
│   ├── ActivityLogService.cs   Audit trail
│   └── ExportService.cs        Export any table to CSV/Excel
├── Forms/
│   ├── LoginForm.cs
│   ├── MainForm.cs             Sidebar menu, top bar, alert timer, tray notifications
│   ├── ProductEditorForm.cs    Add/Edit product with live status preview
│   └── AlertPopupForm.cs       Start-up alert summary
├── Views/                      The screens shown inside the main window
└── UI/
    ├── Theme.cs                Colours, fonts and styling helpers
    └── AppIcon.cs              Logo drawn in code
```

### 4.3 Database design (SQLite)

The database is the single file **`expiry_alert.db`** next to the program (`bin\Debug\net472\`). You can open it with the free tool **DB Browser for SQLite**.

**Users**: `UserId` (PK), `Username` (unique), `FullName`, `PasswordHash`, `Role` (Admin/Staff), `IsActive`, `CreatedAt`

**Categories**: `CategoryId` (PK), `Name` (unique), `Description`

**Suppliers**: `SupplierId` (PK), `Name` (unique), `Phone`, `Email`, `Address`

**Products**: `ProductId` (PK), `Name`, `CategoryId` (FK), `SupplierId` (FK), `BatchNumber`, `Barcode`, `Quantity`, `UnitPrice`, `ManufactureDate`, `ExpiryDate`, `Location`, `Status` (Active/Disposed), `Notes`, `CreatedAt`, `UpdatedAt`

**AlertLog**: `AlertId` (PK), `ProductId` (FK), `AlertType` (Expired/Critical/Expiring Soon), `Message`, `AlertDate`, `IsAcknowledged`, `AcknowledgedBy`, `AcknowledgedAt`. *UNIQUE(ProductId, AlertType) prevents duplicate alerts.*

**Settings**: `Key` (PK), `Value`

**ActivityLog**: `LogId` (PK), `Username`, `Action`, `Details`, `LogDate`

**Relationships**
```
Categories 1 ──< Products >── 1 Suppliers
                    │
                    1
                    │
                    ^
                 AlertLog
```

### 4.4 The alert algorithm

```
FOR each product WHERE Status = 'Active'
    daysLeft = ExpiryDate - Today
    IF daysLeft < 0                 THEN status = EXPIRED
    ELSE IF daysLeft <= CriticalDays THEN status = CRITICAL
    ELSE IF daysLeft <= WarningDays  THEN status = EXPIRING SOON
    ELSE                                  status = SAFE

    IF status <> SAFE THEN
        INSERT alert (product, status) into AlertLog  -- ignored if it already exists
END FOR
IF new alerts were created THEN show a Windows notification
```

It runs **at start-up**, **every N minutes** (a timer), **after a product is saved**, and when the user clicks **Check Now**.

Alert life-cycle: **Raised (Pending) >> Acknowledged >> (optionally) Product Disposed**

### 4.5 Security
- Passwords are stored as **salted SHA-256 hashes** and never as plain text.
- **Role-based access**: only Admins see Users, Activity Log and can change the alert rules.
- All SQL uses **parameters** (`@name`), which protects against SQL injection.
- The system always keeps at least one active Admin, so nobody can be locked out.
- Every important action is written to the **Activity Log**.

---

## 5. Technology used

| Item | Choice | Why |
|---|---|---|
| Language | C# | Strongly typed, widely taught, good for Windows apps |
| Framework | .NET Framework 4.7.2, Windows Forms | Built into Windows, no extra installation |
| Database | SQLite (`Microsoft.Data.Sqlite`) | Serverless, single-file, zero configuration, reliable |
| IDE | Visual Studio | Free Community edition |

---

## 6. Suggested test cases (for Chapter 4)

| # | Test | Steps | Expected result |
|---|---|---|---|
| 1 | Valid login | admin / admin123 | Main window opens |
| 2 | Invalid login | admin / wrong | "Wrong username or password" |
| 3 | Add product | Products > Add, expiry = today + 3 days | Row shown orange, "Critical", new alert raised |
| 4 | Validation | Add product with no name | Warning: "Please enter the product name" |
| 5 | Date rule | Manufacture date after expiry date | Warning shown, not saved |
| 6 | Expired detection | Product with past expiry | Status "Expired" (red), appears on Dashboard |
| 7 | Acknowledge | Alerts > select > Acknowledge | Alert moves to history, counter decreases |
| 8 | Dispose | Products > select expired > Mark as Disposed | Status "Disposed", alerts closed |
| 9 | Change rules | Settings: Expiring Soon = 60 days | More products become yellow |
| 10 | Report | Reports > Expired Products > Print | Print preview with totals |
| 11 | Export | Any screen > Export | CSV opens in Excel |
| 12 | Role access | Log in as a Staff user | Users and Activity Log menus are hidden |
| 13 | Backup | Settings > Backup Database | .db copy saved to the chosen folder |

---

## 7. Possible future improvements
- Barcode scanner input
- Email or SMS alerts to the manager
- Multi-branch support with a central server database
- Sales/POS integration so quantities update automatically
