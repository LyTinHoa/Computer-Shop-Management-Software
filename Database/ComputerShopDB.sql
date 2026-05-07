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
