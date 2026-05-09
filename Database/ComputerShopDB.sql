-- Create the main database
CREATE DATABASE ComputerShopDB;
GO

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

-- 3. Create PRODUCTS table (Cleaned of the un-normalized string column)
CREATE TABLE Products (
    productID INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(150) NOT NULL,
    category NVARCHAR(50),
    price DECIMAL(18, 2) NOT NULL,
    stockQuantity INT NOT NULL
);

-- 4. Create HARDWARE_SPECS table (Normalized 1-to-1 with Products)
CREATE TABLE HardwareSpecs (
    specID INT IDENTITY(1,1) PRIMARY KEY,
    productID INT UNIQUE NOT NULL, 
    cpu NVARCHAR(100),
    ram NVARCHAR(50),
    storage NVARCHAR(100),
    gpu NVARCHAR(100),
    CONSTRAINT FK_HardwareSpecs_Products FOREIGN KEY (productID) 
        REFERENCES Products(productID) ON DELETE CASCADE
);

-- 5. Create INVOICE table
CREATE TABLE Invoice (
    invoiceID INT IDENTITY(1,1) PRIMARY KEY,
    orderDate DATETIME DEFAULT GETDATE(),
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

-- The SHA-256 hash for 'password123' is:
-- ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f

INSERT INTO Staff (fullName, username, password, role)
VALUES
('Alice Owner', 'manager', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f', 'Manager'),
('Bob Register', 'sales', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f', 'Sales'),
('Charlie Stock', 'inventory', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f', 'Inventory');
GO

-- Add a dummy customer so the invoice doesn't crash on the Foreign Key
INSERT INTO Customer (fullName, email, phone) VALUES ('Walk-in Customer', 'none@none.com', '0000000000');

-- Add some premium tech products
INSERT INTO Products (name, category, price, stockQuantity) VALUES
('NVIDIA RTX 4090 GPU', 'Graphics Card', 1599.99, 10),
('Intel Core i9-13900K', 'CPU', 589.50, 25),
('Samsung 990 PRO 2TB SSD', 'Storage', 169.99, 50),
('Corsair Vengeance 32GB RAM', 'Memory', 110.00, 40);
GO

