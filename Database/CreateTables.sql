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
        ('Other'),
        ('Split');
END

-- Migration: ensure "Split" category exists (for existing DBs created before Split was added)
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND type in (N'U'))
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Name] = N'Split')
        INSERT INTO [dbo].[Categories] ([Name]) VALUES (N'Split');
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
        [Category] INT NULL,
        [DatePaid] DATETIME2 NULL,
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [IsSplit] BIT NOT NULL DEFAULT 0,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [ModifiedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [FK_Expenses_PaymentMethods] FOREIGN KEY ([PaymentMethod]) REFERENCES [dbo].[PaymentMethods]([ID]),
        CONSTRAINT [FK_Expenses_Categories] FOREIGN KEY ([Category]) REFERENCES [dbo].[Categories]([Category_I])
    );
    
    -- Create indexes for better query performance
    CREATE INDEX [IX_Expenses_UserId] ON [dbo].[Expenses]([UserId]);
    CREATE INDEX [IX_Expenses_ExpenseDate] ON [dbo].[Expenses]([ExpenseDate]);
    CREATE INDEX [IX_Expenses_Category] ON [dbo].[Expenses]([Category]);
END

-- Add IsSplit to existing Expenses table if missing (migration)
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Expenses]') AND type in (N'U'))
   AND COL_LENGTH('dbo.Expenses', 'IsSplit') IS NULL
BEGIN
    ALTER TABLE [dbo].[Expenses] ADD [IsSplit] BIT NOT NULL DEFAULT 0;
END

-- Expenses_split Table (splits for parent expenses)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Expenses_split]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Expenses_split] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Expense_I] INT NOT NULL,
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [Description] NVARCHAR(500) NOT NULL,
        [Amount] DECIMAL(18,2) NOT NULL,
        [Category] INT NOT NULL,
        [CreatedDateTime] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [FK_Expenses_split_Expenses] FOREIGN KEY ([Expense_I]) REFERENCES [dbo].[Expenses]([Expense_I]) ON DELETE CASCADE,
        CONSTRAINT [FK_Expenses_split_Categories] FOREIGN KEY ([Category]) REFERENCES [dbo].[Categories]([Category_I])
    );
    CREATE INDEX [IX_Expenses_split_Expense_I] ON [dbo].[Expenses_split]([Expense_I]);
END
