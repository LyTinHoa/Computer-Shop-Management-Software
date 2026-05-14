-- Create the main database
CREATE DATABASE ComputerShopDB;

USE ComputerShopDB;

-- 1. Create STAFF table
CREATE TABLE Staff (
    staffID INT IDENTITY(1,1) PRIMARY KEY,
    fullName NVARCHAR(100) NOT NULL,
    username NVARCHAR(50) UNIQUE NOT NULL,
    password NVARCHAR(256) NOT NULL, 
    role NVARCHAR(50) NOT NULL
);

-- 2. Create CUSTOMER table
CREATE TABLE Customer (
    customerID INT IDENTITY(1,1) PRIMARY KEY,
    fullName NVARCHAR(100) NOT NULL,
    email NVARCHAR(100),
    phone NVARCHAR(20) UNIQUE NOT NULL
);

-- 3. Create PRODUCTS table
CREATE TABLE Products (
    productID INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(150) NOT NULL,
    category NVARCHAR(50),
    price DECIMAL(18, 2) NOT NULL,
    stockQuantity INT NOT NULL
);

-- 4. Create HARDWARE_SPECS table
CREATE TABLE HardwareSpecs (
    specID INT IDENTITY(1,1) PRIMARY KEY,
    productID INT UNIQUE NOT NULL, 
    cpu NVARCHAR(100),
    ram NVARCHAR(50),
    storage NVARCHAR(100),
    gpu NVARCHAR(100),
    CONSTRAINT FK_HardwareSpecs_Products FOREIGN KEY (productID) REFERENCES Products(productID)
);

-- 5. Create INVOICE table
-- UPDATED: Identity starts at 100 for a more realistic starting number
CREATE TABLE Invoice (
    invoiceID INT IDENTITY(100,1) PRIMARY KEY, 
    orderDate DATETIME NOT NULL,
    totalAmount DECIMAL(18, 2) NOT NULL,
    staffID INT NOT NULL,
    customerID INT NOT NULL,
    CONSTRAINT FK_Invoice_Staff FOREIGN KEY (staffID) REFERENCES Staff(staffID),
    CONSTRAINT FK_Invoice_Customer FOREIGN KEY (customerID) REFERENCES Customer(customerID)
);

-- 6. Create INVOICE_DETAILS table
CREATE TABLE InvoiceDetails (
    detailID INT IDENTITY(1,1) PRIMARY KEY,
    invoiceID INT NOT NULL,
    productID INT NOT NULL,
    quantity INT NOT NULL,
    unitPrice DECIMAL(18, 2) NOT NULL,
    CONSTRAINT FK_InvoiceDetails_Invoice FOREIGN KEY (invoiceID) REFERENCES Invoice(invoiceID),
    CONSTRAINT FK_InvoiceDetails_Products FOREIGN KEY (productID) REFERENCES Products(productID)
);

-- ==========================================
-- DEMO SEED DATA
-- ==========================================

-- STAFF (Password is 'password123' for all)
INSERT INTO Staff (fullName, username, password, role) VALUES
('Alice Owner', 'manager', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f', 'Manager'),
('Bob Register', 'sales', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f', 'Sales'),
('Charlie Wares Jr.', 'inventory', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f', 'Inventory');

-- CUSTOMERS
INSERT INTO Customer (fullName, email, phone) VALUES
('John Doe', 'john.doe@email.com', '555-0101'),
('Jane Smith', 'jane.smith@email.com', '555-0102'),
('Michael Johnson', 'mj.tech@email.com', '555-0103'),
('Sarah Williams', 'sarah.w@email.com', '555-0104'),
('David Brown', 'dbrown.dev@email.com', '555-0105');

-- PRODUCTS (Includes diverse categories and intentional low stock items)
INSERT INTO Products (name, category, price, stockQuantity) VALUES
('NVIDIA RTX 4090 24GB', 'Graphics Cards', 1599.99, 5),
('NVIDIA RTX 4070 Ti 12GB', 'Graphics Cards', 799.99, 2),    -- Low Stock
('Intel Core i9-14900K', 'Processors', 589.50, 12),
('AMD Ryzen 9 7950X', 'Processors', 549.00, 3),            -- Low Stock
('Samsung 990 PRO 2TB', 'Storage', 169.99, 25),
('WD Black SN850X 1TB', 'Storage', 89.99, 1),              -- Critical Stock
('Corsair Vengeance 32GB DDR5', 'Memory', 119.99, 40),
('G.Skill Trident Z5 64GB DDR5', 'Memory', 219.99, 8),
('ASUS ROG Strix Z790-E', 'Motherboards', 399.99, 15),
('MSI MAG B650 Tomahawk', 'Motherboards', 219.99, 2);      -- Low Stock

-- HARDWARE SPECS
INSERT INTO HardwareSpecs (productID, cpu, ram, storage, gpu) VALUES
(1, 'N/A', 'N/A', 'N/A', '24GB GDDR6X'),
(2, 'N/A', 'N/A', 'N/A', '12GB GDDR6X'),
(3, '24 Cores / 32 Threads', 'N/A', 'N/A', 'Integrated UHD 770'),
(4, '16 Cores / 32 Threads', 'N/A', 'N/A', 'Integrated RDNA 2'),
(5, 'N/A', 'N/A', '2TB NVMe PCIe 4.0', 'N/A'),
(6, 'N/A', 'N/A', '1TB NVMe PCIe 4.0', 'N/A'),
(7, 'N/A', '32GB (2x16) 6000MHz', 'N/A', 'N/A'),
(8, 'N/A', '64GB (2x32) 6400MHz', 'N/A', 'N/A'),
(9, 'LGA 1700 Socket', '4x DDR5 Slots', '4x M.2 Slots', 'PCIe 5.0 x16'),
(10, 'AM5 Socket', '4x DDR5 Slots', '3x M.2 Slots', 'PCIe 4.0 x16');


-- ==========================================
-- DYNAMICALLY STAGED INVOICES
-- ==========================================
-- Daily Sales (Today)
INSERT INTO Invoice (orderDate, totalAmount, staffID, customerID) VALUES (GETDATE(), 2189.49, 2, 1);
INSERT INTO Invoice (orderDate, totalAmount, staffID, customerID) VALUES (GETDATE(), 339.98, 2, 2);
INSERT INTO Invoice (orderDate, totalAmount, staffID, customerID) VALUES (GETDATE(), 1199.98, 1, 3);

-- Weekly Sales (Last 3 to 7 days)
INSERT INTO Invoice (orderDate, totalAmount, staffID, customerID) VALUES (DATEADD(day, -2, GETDATE()), 768.99, 2, 4);
INSERT INTO Invoice (orderDate, totalAmount, staffID, customerID) VALUES (DATEADD(day, -4, GETDATE()), 479.96, 1, 1);
INSERT INTO Invoice (orderDate, totalAmount, staffID, customerID) VALUES (DATEADD(day, -6, GETDATE()), 1599.99, 2, 5);

-- Monthly Sales (Last 10 to 40 days)
INSERT INTO Invoice (orderDate, totalAmount, staffID, customerID) VALUES (DATEADD(day, -12, GETDATE()), 219.99, 2, 2);
INSERT INTO Invoice (orderDate, totalAmount, staffID, customerID) VALUES (DATEADD(day, -18, GETDATE()), 929.48, 1, 3);
INSERT INTO Invoice (orderDate, totalAmount, staffID, customerID) VALUES (DATEADD(day, -25, GETDATE()), 89.99, 2, 4);
INSERT INTO Invoice (orderDate, totalAmount, staffID, customerID) VALUES (DATEADD(day, -35, GETDATE()), 989.49, 1, 1);
INSERT INTO Invoice (orderDate, totalAmount, staffID, customerID) VALUES (DATEADD(day, -40, GETDATE()), 3199.98, 2, 5);

-- INVOICE DETAILS (Updated to match the new 100-based Invoice IDs)
-- Invoice 100 (Today: $2189.49)
INSERT INTO InvoiceDetails (invoiceID, productID, quantity, unitPrice) VALUES (100, 1, 1, 1599.99);
INSERT INTO InvoiceDetails (invoiceID, productID, quantity, unitPrice) VALUES (100, 3, 1, 589.50);

-- Invoice 101 (Today: $339.98)
INSERT INTO InvoiceDetails (invoiceID, productID, quantity, unitPrice) VALUES (101, 5, 2, 169.99);

-- Invoice 102 (Today: $1199.98)
INSERT INTO InvoiceDetails (invoiceID, productID, quantity, unitPrice) VALUES (102, 2, 1, 799.99);
INSERT INTO InvoiceDetails (invoiceID, productID, quantity, unitPrice) VALUES (102, 9, 1, 399.99);

-- Invoice 103 (-2 days: $768.99)
INSERT INTO InvoiceDetails (invoiceID, productID, quantity, unitPrice) VALUES (103, 4, 1, 549.00);
INSERT INTO InvoiceDetails (invoiceID, productID, quantity, unitPrice) VALUES (103, 10, 1, 219.99);

-- Invoice 104 (-4 days: $479.96)
INSERT INTO InvoiceDetails (invoiceID, productID, quantity, unitPrice) VALUES (104, 7, 4, 119.99);

-- Invoice 105 (-6 days: $1599.99)
INSERT INTO InvoiceDetails (invoiceID, productID, quantity, unitPrice) VALUES (105, 1, 1, 1599.99);

-- Invoice 106 (-12 days: $219.99)
INSERT INTO InvoiceDetails (invoiceID, productID, quantity, unitPrice) VALUES (106, 8, 1, 219.99);

-- Invoice 107 (-18 days: $929.48)
INSERT INTO InvoiceDetails (invoiceID, productID, quantity, unitPrice) VALUES (107, 3, 1, 589.50);
INSERT INTO InvoiceDetails (invoiceID, productID, quantity, unitPrice) VALUES (107, 5, 2, 169.99);

-- Invoice 108 (-25 days: $89.99)
INSERT INTO InvoiceDetails (invoiceID, productID, quantity, unitPrice) VALUES (108, 6, 1, 89.99);

-- Invoice 109 (-35 days: $989.49)
INSERT INTO InvoiceDetails (invoiceID, productID, quantity, unitPrice) VALUES (109, 9, 1, 399.99);
INSERT INTO InvoiceDetails (invoiceID, productID, quantity, unitPrice) VALUES (109, 3, 1, 589.50);

-- Invoice 110 (-40 days: $3199.98)
INSERT INTO InvoiceDetails (invoiceID, productID, quantity, unitPrice) VALUES (110, 1, 2, 1599.99);
