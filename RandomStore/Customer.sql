CREATE TABLE [dbo].[Customer]
(
  [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT pk_Customer PRIMARY KEY DEFAULT newid(),
  [Title] NVARCHAR(50) NOT NULL,
  [FirstName] NVARCHAR(50) NOT NULL,
  [LastName] NVARCHAR(50) NOT NULL,
  [Email] NVARCHAR(50) NULL,
  [Phone] NVARCHAR(50) NULL,
);
GO
CREATE UNIQUE INDEX i_Customer_Email ON Customer(Email);
GO
--CREATE FULLTEXT INDEX ON Customer(FirstName, LastName, Email) KEY INDEX pk_Customer ON [RandomStoreFullTextCatalog];
GO