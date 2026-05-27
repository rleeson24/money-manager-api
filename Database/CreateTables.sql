-- Money Manager Database Schema
-- SQL Server

-- Categories Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Categories] (
        [Category_I] INT IDENTITY(1,1) PRIMARY KEY,
        [Name] NVARCHAR(100) NOT NULL,
        [ParentCategory_I] INT NULL,
        [Required] BIT NOT NULL DEFAULT 0,
        [Archived] BIT NOT NULL DEFAULT 0,
        CONSTRAINT [FK_Categories_Parent] FOREIGN KEY ([ParentCategory_I]) REFERENCES [dbo].[Categories]([Category_I])
    );
    CREATE INDEX [IX_Categories_ParentCategory_I] ON [dbo].[Categories]([ParentCategory_I]);
END

-- Migration: add hierarchy columns to existing Categories table
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND type in (N'U'))
   AND COL_LENGTH('dbo.Categories', 'ParentCategory_I') IS NULL
BEGIN
    ALTER TABLE [dbo].[Categories] ADD [ParentCategory_I] INT NULL;
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND type in (N'U'))
   AND COL_LENGTH('dbo.Categories', 'Required') IS NULL
BEGIN
    ALTER TABLE [dbo].[Categories] ADD [Required] BIT NOT NULL DEFAULT 0;
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND type in (N'U'))
   AND COL_LENGTH('dbo.Categories', 'Archived') IS NULL
BEGIN
    ALTER TABLE [dbo].[Categories] ADD [Archived] BIT NOT NULL DEFAULT 0;
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND type in (N'U'))
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Categories_Parent')
BEGIN
    ALTER TABLE [dbo].[Categories] ADD CONSTRAINT [FK_Categories_Parent]
        FOREIGN KEY ([ParentCategory_I]) REFERENCES [dbo].[Categories]([Category_I]);
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND type in (N'U'))
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Categories_ParentCategory_I' AND object_id = OBJECT_ID(N'[dbo].[Categories]'))
BEGIN
    CREATE INDEX [IX_Categories_ParentCategory_I] ON [dbo].[Categories]([ParentCategory_I]);
END

-- PaymentMethods Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PaymentMethods]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PaymentMethods] (
        [ID] INT IDENTITY(1,1) PRIMARY KEY,
        [PaymentMethod] NVARCHAR(100) NOT NULL
    );
END

-- Expenses Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Expenses]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Expenses] (
        [Expense_I] INT IDENTITY(1,1) PRIMARY KEY,
        [ExpenseDate] DATETIME2 NOT NULL,
        [Expense] NVARCHAR(500) NOT NULL,
        [Amount] DECIMAL(18,2) NOT NULL,
        [PaymentMethod] INT NULL,
        [Category] INT NULL,
        [DatePaid] DATETIME2 NULL,
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [IsSplit] BIT NOT NULL DEFAULT 0,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [ModifiedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NOT NULL,
        CONSTRAINT [FK_Expenses_PaymentMethods] FOREIGN KEY ([PaymentMethod]) REFERENCES [dbo].[PaymentMethods]([ID]),
        CONSTRAINT [FK_Expenses_Categories] FOREIGN KEY ([Category]) REFERENCES [dbo].[Categories]([Category_I])
    );
    
    -- Create indexes for better query performance
    CREATE INDEX [IX_Expenses_UserId] ON [dbo].[Expenses]([UserId]);
    CREATE INDEX [IX_Expenses_ExpenseDate] ON [dbo].[Expenses]([ExpenseDate]);
    CREATE INDEX [IX_Expenses_Category] ON [dbo].[Expenses]([Category]);
END

-- Allow NULL PaymentMethod on Expenses (optional payment method)
IF EXISTS (SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Expenses') AND name = N'PaymentMethod' AND is_nullable = 0)
BEGIN
    ALTER TABLE [dbo].[Expenses] ALTER COLUMN [PaymentMethod] INT NULL;
END

-- Add IsSplit to existing Expenses table if missing (migration)
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Expenses]') AND type in (N'U'))
   AND COL_LENGTH('dbo.Expenses', 'IsSplit') IS NULL
BEGIN
    ALTER TABLE [dbo].[Expenses] ADD [IsSplit] BIT NOT NULL DEFAULT 0;
END

-- Add CreatedBy to existing Expenses table if missing (migration)
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Expenses]') AND type in (N'U'))
   AND COL_LENGTH('dbo.Expenses', 'CreatedBy') IS NULL
BEGIN
    ALTER TABLE [dbo].[Expenses] ADD [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT '';
END

-- Make CreatedBy NOT NULL if it was added as NULL (migration: backfill then alter)
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Expenses]') AND type in (N'U'))
   AND COL_LENGTH('dbo.Expenses', 'CreatedBy') IS NOT NULL
BEGIN
    UPDATE [dbo].[Expenses] SET [CreatedBy] = CAST([UserId] AS NVARCHAR(50)) WHERE [CreatedBy] IS NULL;
    ALTER TABLE [dbo].[Expenses] ALTER COLUMN [CreatedBy] NVARCHAR(100) NOT NULL;
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

-- Catalog seed data: applied by AspireSqlDevelopmentBootstrap (C#) and SeedCategories.sql / SeedPaymentMethods.sql for manual runs.
