-- ============================================================================
-- Money Manager — production schema (DDL only)
-- ============================================================================
-- Run this script against a production database to create or upgrade tables.
-- Idempotent: safe to re-run on an existing database.
--
-- DO NOT run SeedCategories.sql or SeedPaymentMethods.sql in production.
-- Those scripts insert legacy catalog IDs for local dev / Aspire bootstrap only.
-- In production, create categories and payment methods via the API or your own import.
--
-- Also see GrantMmAppAccess.sql for Azure AD managed identity permissions.
-- ============================================================================

-- Categories
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
GO

-- Categories: upgrade existing table (split batches — SQL Server cannot use new columns in the same batch as ALTER ADD)
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND type in (N'U'))
   AND COL_LENGTH('dbo.Categories', 'ParentCategory_I') IS NULL
BEGIN
    ALTER TABLE [dbo].[Categories] ADD [ParentCategory_I] INT NULL;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND type in (N'U'))
   AND COL_LENGTH('dbo.Categories', 'Required') IS NULL
BEGIN
    ALTER TABLE [dbo].[Categories] ADD [Required] BIT NOT NULL DEFAULT 0;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND type in (N'U'))
   AND COL_LENGTH('dbo.Categories', 'Archived') IS NULL
BEGIN
    ALTER TABLE [dbo].[Categories] ADD [Archived] BIT NOT NULL DEFAULT 0;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND type in (N'U'))
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Categories_Parent')
BEGIN
    ALTER TABLE [dbo].[Categories] ADD CONSTRAINT [FK_Categories_Parent]
        FOREIGN KEY ([ParentCategory_I]) REFERENCES [dbo].[Categories]([Category_I]);
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND type in (N'U'))
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Categories_ParentCategory_I' AND object_id = OBJECT_ID(N'[dbo].[Categories]'))
BEGIN
    CREATE INDEX [IX_Categories_ParentCategory_I] ON [dbo].[Categories]([ParentCategory_I]);
END
GO

-- PaymentMethods
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PaymentMethods]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PaymentMethods] (
        [ID] INT IDENTITY(1,1) PRIMARY KEY,
        [PaymentMethod] NVARCHAR(100) NOT NULL
    );
END
GO

-- Expenses
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

    CREATE INDEX [IX_Expenses_UserId] ON [dbo].[Expenses]([UserId]);
    CREATE INDEX [IX_Expenses_ExpenseDate] ON [dbo].[Expenses]([ExpenseDate]);
    CREATE INDEX [IX_Expenses_Category] ON [dbo].[Expenses]([Category]);
END
GO

IF EXISTS (SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Expenses') AND name = N'PaymentMethod' AND is_nullable = 0)
BEGIN
    ALTER TABLE [dbo].[Expenses] ALTER COLUMN [PaymentMethod] INT NULL;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Expenses]') AND type in (N'U'))
   AND COL_LENGTH('dbo.Expenses', 'IsSplit') IS NULL
BEGIN
    ALTER TABLE [dbo].[Expenses] ADD [IsSplit] BIT NOT NULL DEFAULT 0;
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Expenses]') AND type in (N'U'))
   AND COL_LENGTH('dbo.Expenses', 'CreatedBy') IS NULL
BEGIN
    ALTER TABLE [dbo].[Expenses] ADD [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT '';
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Expenses]') AND type in (N'U'))
   AND COL_LENGTH('dbo.Expenses', 'CreatedBy') IS NOT NULL
   AND EXISTS (
       SELECT 1 FROM sys.columns
       WHERE object_id = OBJECT_ID(N'dbo.Expenses') AND name = N'CreatedBy' AND is_nullable = 1)
BEGIN
    UPDATE [dbo].[Expenses] SET [CreatedBy] = CAST([UserId] AS NVARCHAR(50)) WHERE [CreatedBy] IS NULL;
    ALTER TABLE [dbo].[Expenses] ALTER COLUMN [CreatedBy] NVARCHAR(100) NOT NULL;
END
GO

-- Expenses_split
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
GO
