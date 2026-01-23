-- Money Manager Database Schema
-- SQL Server

-- Categories Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Categories] (
        [Category_I] INT IDENTITY(1,1) PRIMARY KEY,
        [Name] NVARCHAR(100) NOT NULL
    );
    
    -- Insert default categories
    INSERT INTO [dbo].[Categories] ([Name]) VALUES
        ('Food'),
        ('Transportation'),
        ('Housing'),
        ('Utilities'),
        ('Entertainment'),
        ('Healthcare'),
        ('Shopping'),
        ('Education'),
        ('Health & Fitness'),
        ('Other');
END

-- PaymentMethods Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PaymentMethods]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PaymentMethods] (
        [ID] INT IDENTITY(1,1) PRIMARY KEY,
        [PaymentMethod] NVARCHAR(100) NOT NULL
    );
    
    -- Insert default payment methods
    INSERT INTO [dbo].[PaymentMethods] ([PaymentMethod]) VALUES
        ('Credit Card'),
        ('Debit Card'),
        ('Cash'),
        ('Bank Transfer'),
        ('PayPal'),
        ('Venmo');
END

-- Expenses Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Expenses]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Expenses] (
        [Expense_I] INT IDENTITY(1,1) PRIMARY KEY,
        [ExpenseDate] DATETIME2 NOT NULL,
        [Expense] NVARCHAR(500) NOT NULL,
        [Amount] DECIMAL(18,2) NOT NULL,
        [PaymentMethod] INT NOT NULL,
        [Category] NVARCHAR(100) NOT NULL,
        [DatePaid] DATETIME2 NULL,
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [ModifiedDate] DATETIME2 NULL,
        CONSTRAINT [FK_Expenses_PaymentMethods] FOREIGN KEY ([PaymentMethod]) REFERENCES [dbo].[PaymentMethods]([ID])
    );
    
    -- Create indexes for better query performance
    CREATE INDEX [IX_Expenses_UserId] ON [dbo].[Expenses]([UserId]);
    CREATE INDEX [IX_Expenses_ExpenseDate] ON [dbo].[Expenses]([ExpenseDate]);
    CREATE INDEX [IX_Expenses_Category] ON [dbo].[Expenses]([Category]);
END
