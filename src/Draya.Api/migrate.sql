IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [AppUsers] (
    [Id] uniqueidentifier NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [PasswordHash] nvarchar(512) NOT NULL,
    [Role] nvarchar(20) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [LastLoginAt] datetime2 NULL,
    CONSTRAINT [PK_AppUsers] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_AppUser_Role] CHECK ([Role] IN ('Teacher', 'Student', 'SuperAdmin'))
);

CREATE TABLE [PlatformAdmins] (
    [UserId] uniqueidentifier NOT NULL,
    [FullName] nvarchar(200) NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_PlatformAdmins] PRIMARY KEY ([UserId]),
    CONSTRAINT [FK_PlatformAdmins_AppUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AppUsers] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [RefreshTokens] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Token] nvarchar(256) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [RevokedAt] datetime2 NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_AppUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AppUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Students] (
    [UserId] uniqueidentifier NOT NULL,
    [FullName] nvarchar(200) NOT NULL,
    [ParentGuardianEmail] nvarchar(256) NOT NULL,
    [DateOfBirth] date NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Students] PRIMARY KEY ([UserId]),
    CONSTRAINT [FK_Students_AppUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AppUsers] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Teachers] (
    [UserId] uniqueidentifier NOT NULL,
    [FullName] nvarchar(200) NOT NULL,
    [Phone] nvarchar(30) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Teachers] PRIMARY KEY ([UserId]),
    CONSTRAINT [FK_Teachers_AppUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AppUsers] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_AppUser_Role] ON [AppUsers] ([Role]);

CREATE UNIQUE INDEX [UQ_AppUser_Email] ON [AppUsers] ([Email]);

CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);

CREATE UNIQUE INDEX [UQ_RefreshToken_Token] ON [RefreshTokens] ([Token]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260802202625_InitialIdentity', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [AppUsers] ADD [FailedLoginAttempts] int NOT NULL DEFAULT 0;

ALTER TABLE [AppUsers] ADD [LockoutEndUtc] datetime2 NULL;

CREATE TABLE [PasswordResetTokens] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [TokenHash] nvarchar(256) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UsedAt] datetime2 NULL,
    CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PasswordResetTokens_AppUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AppUsers] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_PasswordResetToken_UserId] ON [PasswordResetTokens] ([UserId]);

CREATE UNIQUE INDEX [UQ_PasswordResetToken_TokenHash] ON [PasswordResetTokens] ([TokenHash]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260802204039_AddSessionSecurity', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [SubscriptionPlans] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [MaxStudents] int NOT NULL,
    [MaxStorageMB] int NOT NULL,
    [MonthlyExamQuota] int NOT NULL,
    [PriceMonthly] decimal(10,2) NOT NULL DEFAULT 0.0,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_SubscriptionPlans] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_SubscriptionPlan_MaxStorageMB] CHECK ([MaxStorageMB] > 0),
    CONSTRAINT [CK_SubscriptionPlan_MaxStudents] CHECK ([MaxStudents] > 0),
    CONSTRAINT [CK_SubscriptionPlan_MonthlyExamQuota] CHECK ([MonthlyExamQuota] > 0),
    CONSTRAINT [CK_SubscriptionPlan_PriceMonthly] CHECK ([PriceMonthly] >= 0)
);

CREATE TABLE [UsageCounters] (
    [Id] uniqueidentifier NOT NULL,
    [TeacherId] uniqueidentifier NOT NULL,
    [PeriodMonth] date NOT NULL,
    [CurrentStudentsCount] int NOT NULL DEFAULT 0,
    [ExamsGeneratedCount] int NOT NULL DEFAULT 0,
    [StorageUsedMB] decimal(10,2) NOT NULL DEFAULT 0.0,
    CONSTRAINT [PK_UsageCounters] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_UsageCounter_CurrentStudentsCount] CHECK ([CurrentStudentsCount] >= 0),
    CONSTRAINT [CK_UsageCounter_ExamsGeneratedCount] CHECK ([ExamsGeneratedCount] >= 0),
    CONSTRAINT [CK_UsageCounter_StorageUsedMB] CHECK ([StorageUsedMB] >= 0)
);

CREATE TABLE [TeacherSubscriptions] (
    [Id] uniqueidentifier NOT NULL,
    [TeacherId] uniqueidentifier NOT NULL,
    [PlanId] uniqueidentifier NOT NULL,
    [StartDate] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [EndDate] datetime2 NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Active',
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_TeacherSubscriptions] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_TeacherSubscription_Status] CHECK ([Status] IN ('Active', 'Expired', 'Cancelled')),
    CONSTRAINT [FK_TeacherSubscriptions_SubscriptionPlans_PlanId] FOREIGN KEY ([PlanId]) REFERENCES [SubscriptionPlans] ([Id]) ON DELETE NO ACTION
);

CREATE UNIQUE INDEX [IX_SubscriptionPlans_Name] ON [SubscriptionPlans] ([Name]);

CREATE INDEX [IX_TeacherSubscription_TeacherId_Status] ON [TeacherSubscriptions] ([TeacherId], [Status]);

CREATE INDEX [IX_TeacherSubscriptions_PlanId] ON [TeacherSubscriptions] ([PlanId]);

CREATE UNIQUE INDEX [UQ_UsageCounter_TeacherId_PeriodMonth] ON [UsageCounters] ([TeacherId], [PeriodMonth]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260802205929_AddSubscriptionsAndUsageCounter', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [Enrollments] (
    [Id] uniqueidentifier NOT NULL,
    [StudentId] uniqueidentifier NOT NULL,
    [ClassroomId] uniqueidentifier NOT NULL,
    [PaymentTransactionId] uniqueidentifier NULL,
    [EnrolledAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [Status] nvarchar(20) NOT NULL DEFAULT N'Active',
    CONSTRAINT [PK_Enrollments] PRIMARY KEY ([Id])
);

CREATE TABLE [Subjects] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_Subjects] PRIMARY KEY ([Id])
);

CREATE TABLE [Classrooms] (
    [Id] uniqueidentifier NOT NULL,
    [TeacherId] uniqueidentifier NOT NULL,
    [SubjectId] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [EnrollmentCode] nvarchar(20) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Classrooms] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Classrooms_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([Id])
);

CREATE INDEX [IX_Classroom_TeacherId] ON [Classrooms] ([TeacherId]);

CREATE INDEX [IX_Classrooms_SubjectId] ON [Classrooms] ([SubjectId]);

CREATE UNIQUE INDEX [UQ_Classroom_EnrollmentCode] ON [Classrooms] ([EnrollmentCode]);

CREATE INDEX [IX_Enrollment_ClassroomId] ON [Enrollments] ([ClassroomId]);

CREATE UNIQUE INDEX [UQ_Enrollment_StudentId_ClassroomId] ON [Enrollments] ([StudentId], [ClassroomId]);

CREATE UNIQUE INDEX [UQ_Subject_Name] ON [Subjects] ([Name]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260802213130_AddClassroomsAndSubjects', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260805145700_InitialCreate', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'IsActive', N'MaxStorageMB', N'MaxStudents', N'MonthlyExamQuota', N'Name') AND [object_id] = OBJECT_ID(N'[SubscriptionPlans]'))
    SET IDENTITY_INSERT [SubscriptionPlans] ON;
INSERT INTO [SubscriptionPlans] ([Id], [CreatedAt], [IsActive], [MaxStorageMB], [MaxStudents], [MonthlyExamQuota], [Name])
VALUES ('b1e9f1d2-4c3a-4e5b-9f6a-8d7e6c5b4a3f', '2024-01-01T00:00:00.0000000Z', CAST(1 AS bit), 500, 30, 3, N'Free');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'IsActive', N'MaxStorageMB', N'MaxStudents', N'MonthlyExamQuota', N'Name') AND [object_id] = OBJECT_ID(N'[SubscriptionPlans]'))
    SET IDENTITY_INSERT [SubscriptionPlans] OFF;

INSERT INTO TeacherSubscriptions (Id, TeacherId, PlanId, StartDate, EndDate, Status, CreatedAt)
SELECT NEWID(), t.UserId, 'b1e9f1d2-4c3a-4e5b-9f6a-8d7e6c5b4a3f', SYSUTCDATETIME(), NULL, 'Active', SYSUTCDATETIME()
FROM Teachers t
WHERE NOT EXISTS (
    SELECT 1 FROM TeacherSubscriptions ts WHERE ts.TeacherId = t.UserId AND ts.Status = 'Active'
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260808204328_SeedFreePlanAndBackfill', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [PlatformAdmins] DROP CONSTRAINT [FK_PlatformAdmins_AppUsers_UserId];

ALTER TABLE [RefreshTokens] DROP CONSTRAINT [FK_RefreshTokens_AppUsers_UserId];

ALTER TABLE [Students] DROP CONSTRAINT [FK_Students_AppUsers_UserId];

ALTER TABLE [Teachers] DROP CONSTRAINT [FK_Teachers_AppUsers_UserId];

CREATE TABLE [AspNetRoles] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetUsers] (
    [Id] uniqueidentifier NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [LastLoginAt] datetime2 NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AppUsers') BEGIN INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, IsActive, CreatedAt, LastLoginAt) SELECT Id, Email, UPPER(Email), Email, UPPER(Email), 1, PasswordHash, NEWID(), NEWID(), 0, 0, 1, FailedLoginAttempts, IsActive, CreatedAt, LastLoginAt FROM AppUsers; END

DELETE FROM RefreshTokens WHERE UserId NOT IN (SELECT Id FROM AspNetUsers);

DROP TABLE [PasswordResetTokens];

DROP TABLE [AppUsers];

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] uniqueidentifier NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] uniqueidentifier NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserRoles] (
    [UserId] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserTokens] (
    [UserId] uniqueidentifier NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

ALTER TABLE [PlatformAdmins] ADD CONSTRAINT [FK_PlatformAdmins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [RefreshTokens] ADD CONSTRAINT [FK_RefreshTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE;

ALTER TABLE [Students] ADD CONSTRAINT [FK_Students_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Teachers] ADD CONSTRAINT [FK_Teachers_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260811132055_RefactorToAspNetCoreIdentity', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
DROP TABLE [TeacherSubscriptions];

DROP TABLE [SubscriptionPlans];

ALTER TABLE [UsageCounters] DROP CONSTRAINT [CK_UsageCounter_CurrentStudentsCount];

ALTER TABLE [UsageCounters] DROP CONSTRAINT [CK_UsageCounter_ExamsGeneratedCount];

ALTER TABLE [UsageCounters] DROP CONSTRAINT [CK_UsageCounter_StorageUsedMB];

DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UsageCounters]') AND [c].[name] = N'StorageUsedMB');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [UsageCounters] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [UsageCounters] DROP COLUMN [StorageUsedMB];

EXEC sp_rename N'[UsageCounters].[ExamsGeneratedCount]', N'PaidExamsGenerated', 'COLUMN';

EXEC sp_rename N'[UsageCounters].[CurrentStudentsCount]', N'FreeExamsUsed', 'COLUMN';

ALTER TABLE [UsageCounters] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME());

ALTER TABLE [UsageCounters] ADD [UpdatedAt] datetime2 NULL;

CREATE TABLE [PaymentTransactions] (
    [Id] uniqueidentifier NOT NULL,
    [Purpose] nvarchar(30) NOT NULL,
    [PayerId] uniqueidentifier NOT NULL,
    [ClassroomId] uniqueidentifier NULL,
    [GrossAmount] decimal(18,2) NOT NULL,
    [CommissionPercent] decimal(5,2) NULL,
    [CommissionAmount] decimal(18,2) NULL,
    [TeacherAmount] decimal(18,2) NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_PaymentTransactions] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_PaymentTransaction_GrossAmount] CHECK ([GrossAmount] >= 0)
);

CREATE TABLE [PlatformSettings] (
    [Id] uniqueidentifier NOT NULL,
    [AIExamPrice] decimal(18,2) NOT NULL DEFAULT 20.0,
    [FreeMonthlyAIExamQuota] int NOT NULL DEFAULT 3,
    [PlatformCommissionPercent] decimal(5,2) NOT NULL DEFAULT 5.0,
    [UpdatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdatedByAdminId] uniqueidentifier NULL,
    CONSTRAINT [PK_PlatformSettings] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_PlatformSetting_AIExamPrice] CHECK ([AIExamPrice] >= 0),
    CONSTRAINT [CK_PlatformSetting_FreeMonthlyAIExamQuota] CHECK ([FreeMonthlyAIExamQuota] >= 0),
    CONSTRAINT [CK_PlatformSetting_PlatformCommissionPercent] CHECK ([PlatformCommissionPercent] >= 0 AND [PlatformCommissionPercent] <= 100)
);

CREATE TABLE [TeacherPayoutAccounts] (
    [Id] uniqueidentifier NOT NULL,
    [TeacherId] uniqueidentifier NOT NULL,
    [AccountType] nvarchar(20) NOT NULL,
    [AccountName] nvarchar(200) NOT NULL,
    [AccountIdentifier] nvarchar(200) NOT NULL,
    [IsDefault] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_TeacherPayoutAccounts] PRIMARY KEY ([Id])
);

CREATE TABLE [TeacherWallets] (
    [Id] uniqueidentifier NOT NULL,
    [TeacherId] uniqueidentifier NOT NULL,
    [EarnedBalance] decimal(18,2) NOT NULL DEFAULT 0.0,
    [PurchasedBalance] decimal(18,2) NOT NULL DEFAULT 0.0,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_TeacherWallets] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_TeacherWallet_EarnedBalance] CHECK ([EarnedBalance] >= 0),
    CONSTRAINT [CK_TeacherWallet_PurchasedBalance] CHECK ([PurchasedBalance] >= 0)
);

CREATE TABLE [WalletTransactions] (
    [Id] uniqueidentifier NOT NULL,
    [TeacherId] uniqueidentifier NOT NULL,
    [Type] nvarchar(30) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [BalanceType] nvarchar(10) NOT NULL,
    [ReferenceId] uniqueidentifier NULL,
    [Description] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_WalletTransactions] PRIMARY KEY ([Id])
);

CREATE TABLE [WithdrawalRequests] (
    [Id] uniqueidentifier NOT NULL,
    [TeacherId] uniqueidentifier NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [RequestedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [ProcessedAt] datetime2 NULL,
    [AdminNote] nvarchar(1000) NULL,
    [RejectionReason] nvarchar(500) NULL,
    CONSTRAINT [PK_WithdrawalRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_WithdrawalRequest_Amount] CHECK ([Amount] > 0)
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AIExamPrice', N'FreeMonthlyAIExamQuota', N'PlatformCommissionPercent', N'UpdatedAt', N'UpdatedByAdminId') AND [object_id] = OBJECT_ID(N'[PlatformSettings]'))
    SET IDENTITY_INSERT [PlatformSettings] ON;
INSERT INTO [PlatformSettings] ([Id], [AIExamPrice], [FreeMonthlyAIExamQuota], [PlatformCommissionPercent], [UpdatedAt], [UpdatedByAdminId])
VALUES ('11111111-1111-1111-1111-111111111111', 20.0, 3, 5.0, '2026-01-01T00:00:00.0000000Z', NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AIExamPrice', N'FreeMonthlyAIExamQuota', N'PlatformCommissionPercent', N'UpdatedAt', N'UpdatedByAdminId') AND [object_id] = OBJECT_ID(N'[PlatformSettings]'))
    SET IDENTITY_INSERT [PlatformSettings] OFF;

ALTER TABLE [UsageCounters] ADD CONSTRAINT [CK_UsageCounter_FreeExamsUsed] CHECK ([FreeExamsUsed] >= 0);

ALTER TABLE [UsageCounters] ADD CONSTRAINT [CK_UsageCounter_PaidExamsGenerated] CHECK ([PaidExamsGenerated] >= 0);

CREATE INDEX [IX_PaymentTransaction_ClassroomId] ON [PaymentTransactions] ([ClassroomId]);

CREATE INDEX [IX_PaymentTransaction_PayerId] ON [PaymentTransactions] ([PayerId]);

CREATE INDEX [IX_TeacherPayoutAccount_TeacherId] ON [TeacherPayoutAccounts] ([TeacherId]);

CREATE UNIQUE INDEX [UQ_TeacherWallet_TeacherId] ON [TeacherWallets] ([TeacherId]);

CREATE INDEX [IX_WalletTransaction_CreatedAt] ON [WalletTransactions] ([CreatedAt]);

CREATE INDEX [IX_WalletTransaction_TeacherId] ON [WalletTransactions] ([TeacherId]);

CREATE INDEX [IX_WithdrawalRequest_Status] ON [WithdrawalRequests] ([Status]);

CREATE INDEX [IX_WithdrawalRequest_TeacherId] ON [WithdrawalRequests] ([TeacherId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260811205431_RefactorSubscriptionsToWalletAndCommissionModel', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [ClassroomTypes] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_ClassroomTypes] PRIMARY KEY ([Id])
);

CREATE TABLE [GradeLevels] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [SortOrder] int NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_GradeLevels] PRIMARY KEY ([Id])
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name', N'Description', N'IsActive', N'CreatedAt') AND [object_id] = OBJECT_ID(N'[ClassroomTypes]'))
    SET IDENTITY_INSERT [ClassroomTypes] ON;
INSERT INTO [ClassroomTypes] ([Id], [Name], [Description], [IsActive], [CreatedAt])
VALUES ('11111111-1111-1111-1111-111111111111', N'Standard Group', N'Default classroom type for existing classrooms', CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name', N'Description', N'IsActive', N'CreatedAt') AND [object_id] = OBJECT_ID(N'[ClassroomTypes]'))
    SET IDENTITY_INSERT [ClassroomTypes] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name', N'Description', N'SortOrder', N'IsActive', N'CreatedAt') AND [object_id] = OBJECT_ID(N'[GradeLevels]'))
    SET IDENTITY_INSERT [GradeLevels] ON;
INSERT INTO [GradeLevels] ([Id], [Name], [Description], [SortOrder], [IsActive], [CreatedAt])
VALUES ('22222222-2222-2222-2222-222222222222', N'General Level', N'Default grade level for existing classrooms', 1, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name', N'Description', N'SortOrder', N'IsActive', N'CreatedAt') AND [object_id] = OBJECT_ID(N'[GradeLevels]'))
    SET IDENTITY_INSERT [GradeLevels] OFF;

ALTER TABLE [Classrooms] ADD [ClassroomTypeId] uniqueidentifier NOT NULL DEFAULT '11111111-1111-1111-1111-111111111111';

ALTER TABLE [Classrooms] ADD [EndDate] datetime2 NOT NULL DEFAULT '2099-12-31T00:00:00.0000000Z';

ALTER TABLE [Classrooms] ADD [GradeLevelId] uniqueidentifier NOT NULL DEFAULT '22222222-2222-2222-2222-222222222222';

ALTER TABLE [Classrooms] ADD [Price] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [Classrooms] ADD [StartDate] datetime2 NOT NULL DEFAULT '2026-01-01T00:00:00.0000000Z';

CREATE INDEX [IX_Classrooms_ClassroomTypeId] ON [Classrooms] ([ClassroomTypeId]);

CREATE INDEX [IX_Classrooms_GradeLevelId] ON [Classrooms] ([GradeLevelId]);

ALTER TABLE [Classrooms] ADD CONSTRAINT [FK_Classrooms_ClassroomTypes_ClassroomTypeId] FOREIGN KEY ([ClassroomTypeId]) REFERENCES [ClassroomTypes] ([Id]);

ALTER TABLE [Classrooms] ADD CONSTRAINT [FK_Classrooms_GradeLevels_GradeLevelId] FOREIGN KEY ([GradeLevelId]) REFERENCES [GradeLevels] ([Id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260812151541_AddClassroomTypeGradeLevelAndFields', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Teachers] ADD [Specialization] nvarchar(max) NULL;

CREATE TABLE [LearningMaterials] (
    [Id] uniqueidentifier NOT NULL,
    [ClassroomId] uniqueidentifier NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [MaterialType] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_LearningMaterials] PRIMARY KEY ([Id])
);

CREATE TABLE [MaterialVersions] (
    [Id] uniqueidentifier NOT NULL,
    [MaterialId] uniqueidentifier NOT NULL,
    [VersionNumber] int NOT NULL,
    [FileUrl] nvarchar(max) NOT NULL,
    [ParseStatus] int NOT NULL,
    [ParseErrorMessage] nvarchar(max) NULL,
    [UploadedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MaterialVersions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MaterialVersions_LearningMaterials_MaterialId] FOREIGN KEY ([MaterialId]) REFERENCES [LearningMaterials] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [VideoDetails] (
    [Id] uniqueidentifier NOT NULL,
    [MaterialId] uniqueidentifier NOT NULL,
    [DurationSeconds] int NOT NULL,
    [Resolution] nvarchar(max) NULL,
    CONSTRAINT [PK_VideoDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_VideoDetails_LearningMaterials_MaterialId] FOREIGN KEY ([MaterialId]) REFERENCES [LearningMaterials] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [MaterialChunks] (
    [Id] uniqueidentifier NOT NULL,
    [VersionId] uniqueidentifier NOT NULL,
    [TextContent] nvarchar(max) NOT NULL,
    [EmbeddingId] nvarchar(max) NULL,
    [ChunkIndex] int NOT NULL,
    CONSTRAINT [PK_MaterialChunks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MaterialChunks_MaterialVersions_VersionId] FOREIGN KEY ([VersionId]) REFERENCES [MaterialVersions] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_MaterialChunks_VersionId] ON [MaterialChunks] ([VersionId]);

CREATE INDEX [IX_MaterialVersions_MaterialId] ON [MaterialVersions] ([MaterialId]);

CREATE INDEX [IX_VideoDetails_MaterialId] ON [VideoDetails] ([MaterialId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260813141844_AddTeacherSpecialization', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Teachers] ADD [Description] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260813151512_AddTeacherDescription', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
DROP INDEX [IX_VideoDetails_MaterialId] ON [VideoDetails];

DECLARE @var1 nvarchar(max);
SELECT @var1 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Teachers]') AND [c].[name] = N'Description');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Teachers] DROP CONSTRAINT ' + @var1 + ';');
ALTER TABLE [Teachers] DROP COLUMN [Description];

DECLARE @var2 nvarchar(max);
SELECT @var2 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Teachers]') AND [c].[name] = N'Specialization');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Teachers] DROP CONSTRAINT ' + @var2 + ';');
ALTER TABLE [Teachers] DROP COLUMN [Specialization];

ALTER TABLE [VideoDetails] ADD [EmbedUrl] nvarchar(max) NULL;

ALTER TABLE [VideoDetails] ADD [Provider] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [VideoDetails] ADD [ProviderVideoId] nvarchar(max) NULL;

DECLARE @var3 nvarchar(max);
SELECT @var3 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MaterialVersions]') AND [c].[name] = N'FileUrl');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [MaterialVersions] DROP CONSTRAINT ' + @var3 + ';');
ALTER TABLE [MaterialVersions] ALTER COLUMN [FileUrl] nvarchar(max) NULL;

CREATE UNIQUE INDEX [IX_VideoDetails_MaterialId] ON [VideoDetails] ([MaterialId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260813184725_AddYouTubeVideoFields', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Teachers] ADD [Description] nvarchar(max) NULL;

ALTER TABLE [Teachers] ADD [Specialization] nvarchar(max) NULL;

CREATE TABLE [Questions] (
    [Id] uniqueidentifier NOT NULL,
    [ClassroomId] uniqueidentifier NOT NULL,
    [AuthorId] uniqueidentifier NOT NULL,
    [Content] nvarchar(2000) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [VoteCount] int NOT NULL,
    [ReplyCount] int NOT NULL,
    [HasTeacherAnswer] bit NOT NULL,
    CONSTRAINT [PK_Questions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Questions_Classrooms_ClassroomId] FOREIGN KEY ([ClassroomId]) REFERENCES [Classrooms] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [QuestionReplies] (
    [Id] uniqueidentifier NOT NULL,
    [QuestionId] uniqueidentifier NOT NULL,
    [AuthorId] uniqueidentifier NOT NULL,
    [Content] nvarchar(2000) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [IsTeacherAnswer] bit NOT NULL,
    CONSTRAINT [PK_QuestionReplies] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QuestionReplies_Questions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [Questions] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [QuestionVotes] (
    [QuestionId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_QuestionVotes] PRIMARY KEY ([QuestionId], [UserId]),
    CONSTRAINT [FK_QuestionVotes_Questions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [Questions] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_QuestionReplies_AuthorId] ON [QuestionReplies] ([AuthorId]);

CREATE INDEX [IX_QuestionReplies_QuestionId] ON [QuestionReplies] ([QuestionId]);

CREATE UNIQUE INDEX [IX_QuestionReplies_QuestionId_IsTeacherAnswer] ON [QuestionReplies] ([QuestionId], [IsTeacherAnswer]) WHERE [IsTeacherAnswer] = 1;

CREATE INDEX [IX_Questions_AuthorId] ON [Questions] ([AuthorId]);

CREATE INDEX [IX_Questions_ClassroomId] ON [Questions] ([ClassroomId]);

CREATE INDEX [IX_Questions_CreatedAt] ON [Questions] ([CreatedAt]);

CREATE INDEX [IX_QuestionVotes_UserId] ON [QuestionVotes] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260814164036_AddClassroomQaChannel', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
DECLARE @var4 nvarchar(max);
SELECT @var4 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[VideoDetails]') AND [c].[name] = N'EmbedUrl');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [VideoDetails] DROP CONSTRAINT ' + @var4 + ';');
ALTER TABLE [VideoDetails] DROP COLUMN [EmbedUrl];

DECLARE @var5 nvarchar(max);
SELECT @var5 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[VideoDetails]') AND [c].[name] = N'Provider');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [VideoDetails] DROP CONSTRAINT ' + @var5 + ';');
ALTER TABLE [VideoDetails] DROP COLUMN [Provider];

DECLARE @var6 nvarchar(max);
SELECT @var6 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[VideoDetails]') AND [c].[name] = N'ProviderVideoId');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [VideoDetails] DROP CONSTRAINT ' + @var6 + ';');
ALTER TABLE [VideoDetails] DROP COLUMN [ProviderVideoId];

EXEC sp_rename N'[MaterialVersions].[FileUrl]', N'ResourceType', 'COLUMN';

ALTER TABLE [MaterialVersions] ADD [Format] nvarchar(max) NULL;

ALTER TABLE [MaterialVersions] ADD [Provider] nvarchar(max) NULL;

ALTER TABLE [MaterialVersions] ADD [ProviderAssetId] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260815012658_UpdateMediaStorageSchema', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Teachers] ADD [ProfilePictureUrl] nvarchar(1000) NULL;

ALTER TABLE [Students] ADD [ProfilePictureUrl] nvarchar(1000) NULL;

ALTER TABLE [Questions] ADD [ImageUrl] nvarchar(1000) NULL;

ALTER TABLE [QuestionReplies] ADD [ImageUrl] nvarchar(1000) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260816003746_AddProfileAndQuestionImages', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [MaterialVersions] ADD [SecureUrl] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260816152400_AddSecureUrlToMaterialVersion', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [PaymentTransactions] ADD [RedirectionUrl] nvarchar(max) NOT NULL DEFAULT N'';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260816213736_AddRedirectionUrlToPaymentTransaction', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
DECLARE @var7 nvarchar(max);
SELECT @var7 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LearningMaterials]') AND [c].[name] = N'Title');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [LearningMaterials] DROP CONSTRAINT ' + @var7 + ';');
ALTER TABLE [LearningMaterials] ALTER COLUMN [Title] nvarchar(300) NOT NULL;

ALTER TABLE [LearningMaterials] ADD [SectionId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

CREATE TABLE [ClassroomSections] (
    [Id] uniqueidentifier NOT NULL,
    [ClassroomId] uniqueidentifier NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(1000) NOT NULL,
    [Order] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ClassroomSections] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClassroomSections_Classrooms_ClassroomId] FOREIGN KEY ([ClassroomId]) REFERENCES [Classrooms] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_LearningMaterials_SectionId] ON [LearningMaterials] ([SectionId]);

CREATE INDEX [IX_ClassroomSections_ClassroomId] ON [ClassroomSections] ([ClassroomId]);


                -- 0. Delete orphaned LearningMaterials (where ClassroomId doesn't exist in Classrooms table)
                -- This prevents FK conflicts later
                DELETE FROM [LearningMaterials]
                WHERE NOT EXISTS (SELECT 1 FROM [Classrooms] c WHERE c.[Id] = [LearningMaterials].[ClassroomId])
            


                -- 1. Create a General section for each existing Classroom
                INSERT INTO [ClassroomSections] ([Id], [ClassroomId], [Title], [Description], [Order], [CreatedAt])
                SELECT NEWID(), [Id], 'General', 'General section for unassigned materials', 0, GETUTCDATE()
                FROM [Classrooms]
            


                -- 2. Update all existing LearningMaterials to be associated with their Classroom's General section
                UPDATE lm
                SET lm.[SectionId] = cs.[Id]
                FROM [LearningMaterials] lm
                INNER JOIN [ClassroomSections] cs ON lm.[ClassroomId] = cs.[ClassroomId]
                WHERE cs.[Title] = 'General'
            

ALTER TABLE [LearningMaterials] ADD CONSTRAINT [FK_LearningMaterials_ClassroomSections_SectionId] FOREIGN KEY ([SectionId]) REFERENCES [ClassroomSections] ([Id]) ON DELETE CASCADE;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260818121937_AddClassroomSections', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [WithdrawalRequests] ADD [PayoutAccountId] uniqueidentifier NULL;

CREATE INDEX [IX_WithdrawalRequests_PayoutAccountId] ON [WithdrawalRequests] ([PayoutAccountId]);

ALTER TABLE [WithdrawalRequests] ADD CONSTRAINT [FK_WithdrawalRequests_TeacherPayoutAccounts_PayoutAccountId] FOREIGN KEY ([PayoutAccountId]) REFERENCES [TeacherPayoutAccounts] ([Id]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260818143720_AddPayoutAccountIdToWithdrawalsNullable', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Students] ADD [ParentGuardianName] nvarchar(100) NOT NULL DEFAULT N'';

ALTER TABLE [Students] ADD [ParentGuardianPhone] nvarchar(20) NOT NULL DEFAULT N'';

ALTER TABLE [Enrollments] ADD [CompletedLessons] int NOT NULL DEFAULT 0;

ALTER TABLE [Enrollments] ADD [LastAccessedAt] datetime2 NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260818160717_AddStudentGuardianAndEnrollmentProgress', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [PiiMappings] (
    [StudentId] uniqueidentifier NOT NULL,
    [AnonymizedId] uniqueidentifier NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PiiMappings] PRIMARY KEY ([StudentId])
);

CREATE UNIQUE INDEX [IX_PiiMappings_AnonymizedId] ON [PiiMappings] ([AnonymizedId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260818163913_AddPiiMappingTable', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [ExamGenerations] (
    [Id] uniqueidentifier NOT NULL,
    [IdempotencyKey] nvarchar(128) NOT NULL,
    [TeacherId] uniqueidentifier NOT NULL,
    [ClassroomId] uniqueidentifier NOT NULL,
    [MaterialVersionId] uniqueidentifier NOT NULL,
    [RequestedCount] int NOT NULL,
    [GeneratedCount] int NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CompletedAt] datetime2 NULL,
    [ErrorMessage] nvarchar(max) NULL,
    CONSTRAINT [PK_ExamGenerations] PRIMARY KEY ([Id])
);

CREATE UNIQUE INDEX [IX_ExamGenerations_IdempotencyKey] ON [ExamGenerations] ([IdempotencyKey]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260818170755_AddExamGenerationsTable', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Classrooms] ADD [ImageUrl] nvarchar(1000) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260818174459_AddImageUrlToClassroomAndQuestions', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [ClassroomFeedback] (
    [Id] uniqueidentifier NOT NULL,
    [ClassroomId] uniqueidentifier NOT NULL,
    [StudentId] uniqueidentifier NOT NULL,
    [Rating] int NOT NULL,
    [Comment] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_ClassroomFeedback] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_ClassroomFeedback_Rating] CHECK ([Rating] >= 1 AND [Rating] <= 5),
    CONSTRAINT [FK_ClassroomFeedback_Classrooms_ClassroomId] FOREIGN KEY ([ClassroomId]) REFERENCES [Classrooms] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [UQ_ClassroomFeedback_ClassroomId_StudentId] ON [ClassroomFeedback] ([ClassroomId], [StudentId]);

CREATE INDEX [IX_ClassroomFeedback_ClassroomId] ON [ClassroomFeedback] ([ClassroomId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260818183000_AddClassroomFeedback', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
EXEC sp_rename N'[ExamGenerations].[MaterialVersionId]', N'SectionId', 'COLUMN';

CREATE TABLE [Exams] (
    [Id] uniqueidentifier NOT NULL,
    [ClassroomId] uniqueidentifier NOT NULL,
    [SectionId] uniqueidentifier NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [Topic] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Exams] PRIMARY KEY ([Id])
);

CREATE TABLE [ExamQuestions] (
    [Id] uniqueidentifier NOT NULL,
    [ExamId] uniqueidentifier NOT NULL,
    [Text] nvarchar(max) NOT NULL,
    [Type] nvarchar(max) NOT NULL,
    [Difficulty] nvarchar(max) NOT NULL,
    [SourceChunkIds] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_ExamQuestions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ExamQuestions_Exams_ExamId] FOREIGN KEY ([ExamId]) REFERENCES [Exams] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ExamQuestionOptions] (
    [Id] uniqueidentifier NOT NULL,
    [ExamQuestionId] uniqueidentifier NOT NULL,
    [Text] nvarchar(max) NOT NULL,
    [IsCorrect] bit NOT NULL,
    CONSTRAINT [PK_ExamQuestionOptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ExamQuestionOptions_ExamQuestions_ExamQuestionId] FOREIGN KEY ([ExamQuestionId]) REFERENCES [ExamQuestions] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_ExamQuestionOptions_ExamQuestionId] ON [ExamQuestionOptions] ([ExamQuestionId]);

CREATE INDEX [IX_ExamQuestions_ExamId] ON [ExamQuestions] ([ExamId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260818183534_UpdateExamToUseSectionId', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [ExamGenerations] ADD [ExamId] uniqueidentifier NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260818211036_AddExamIdToExamGeneration', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE INDEX [IX_Exams_SectionId] ON [Exams] ([SectionId]);

ALTER TABLE [Exams] ADD CONSTRAINT [FK_Exams_ClassroomSections_SectionId] FOREIGN KEY ([SectionId]) REFERENCES [ClassroomSections] ([Id]) ON DELETE CASCADE;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260818215446_AddExamsToClassroomSection', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [ExamQuestions] ADD [Rubric] nvarchar(max) NULL;

CREATE TABLE [ExamGradingJobs] (
    [Id] uniqueidentifier NOT NULL,
    [StudentExamAttemptId] uniqueidentifier NOT NULL,
    [IdempotencyKey] nvarchar(450) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CompletedAt] datetime2 NULL,
    [ErrorMessage] nvarchar(max) NULL,
    CONSTRAINT [PK_ExamGradingJobs] PRIMARY KEY ([Id])
);

CREATE TABLE [StudentExamAttempts] (
    [Id] uniqueidentifier NOT NULL,
    [ExamId] uniqueidentifier NOT NULL,
    [StudentId] uniqueidentifier NOT NULL,
    [StartedAt] datetime2 NOT NULL,
    [SubmittedAt] datetime2 NULL,
    [IsSubmitted] bit NOT NULL,
    [FinalScore] decimal(18,2) NULL,
    [NeedsTeacherReview] bit NOT NULL,
    CONSTRAINT [PK_StudentExamAttempts] PRIMARY KEY ([Id])
);

CREATE TABLE [StudentAnswers] (
    [Id] uniqueidentifier NOT NULL,
    [StudentExamAttemptId] uniqueidentifier NOT NULL,
    [ExamQuestionId] uniqueidentifier NOT NULL,
    [AnswerText] nvarchar(4000) NOT NULL,
    [SelectedOptionId] uniqueidentifier NULL,
    CONSTRAINT [PK_StudentAnswers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentAnswers_StudentExamAttempts_StudentExamAttemptId] FOREIGN KEY ([StudentExamAttemptId]) REFERENCES [StudentExamAttempts] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AnswerGradingResults] (
    [Id] uniqueidentifier NOT NULL,
    [StudentAnswerId] uniqueidentifier NOT NULL,
    [Score] decimal(18,2) NOT NULL,
    [MaxScore] decimal(18,2) NOT NULL,
    [ConfidenceScore] decimal(18,2) NULL,
    [Rationale] nvarchar(max) NULL,
    [IsAiGraded] bit NOT NULL,
    [NeedsTeacherReview] bit NOT NULL,
    [TeacherOverrideScore] decimal(18,2) NULL,
    CONSTRAINT [PK_AnswerGradingResults] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AnswerGradingResults_StudentAnswers_StudentAnswerId] FOREIGN KEY ([StudentAnswerId]) REFERENCES [StudentAnswers] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_AnswerGradingResults_StudentAnswerId] ON [AnswerGradingResults] ([StudentAnswerId]);

CREATE UNIQUE INDEX [IX_ExamGradingJobs_IdempotencyKey] ON [ExamGradingJobs] ([IdempotencyKey]);

CREATE INDEX [IX_StudentAnswers_StudentExamAttemptId] ON [StudentAnswers] ([StudentExamAttemptId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260819022133_AddAgent2GradingModels', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Exams] ADD [AllowedAttempts] int NOT NULL DEFAULT 0;

ALTER TABLE [Exams] ADD [DurationMinutes] int NOT NULL DEFAULT 0;

ALTER TABLE [Exams] ADD [EndDate] datetime2 NULL;

ALTER TABLE [Exams] ADD [StartDate] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260820200846_AddExamStartEndAttempts', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [PerformanceReports] (
    [Id] uniqueidentifier NOT NULL,
    [StudentId] uniqueidentifier NOT NULL,
    [ExamAttemptId] uniqueidentifier NOT NULL,
    [SummaryText] nvarchar(max) NOT NULL,
    [GeneratedAt] datetime2 NOT NULL,
    [IsApproved] bit NOT NULL,
    [SubjectProficiencies] nvarchar(max) NULL,
    [TrendPoints] nvarchar(max) NULL,
    [WeakTopics] nvarchar(max) NULL,
    CONSTRAINT [PK_PerformanceReports] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260820220244_AddPerformanceReports', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Students] ADD [CurrentStreak] int NOT NULL DEFAULT 0;

ALTER TABLE [Students] ADD [LastActivityDate] datetime2 NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260821025430_UpdatePerformanceReports', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [PerformanceReports] ADD [AverageExamDurationMinutes] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [PerformanceReports] ADD [ClassroomPercentile] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [PerformanceReports] ADD [CompletedLessons] int NOT NULL DEFAULT 0;

ALTER TABLE [PerformanceReports] ADD [TotalQuestionsAsked] int NOT NULL DEFAULT 0;

ALTER TABLE [PerformanceReports] ADD [TotalQuestionsReplied] int NOT NULL DEFAULT 0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260822024335_AddReportMetrics', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [AnswerGradingResults] ADD [IsFinalized] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [AnswerGradingResults] ADD [ReviewedAt] datetime2 NULL;

ALTER TABLE [AnswerGradingResults] ADD [ReviewedByTeacherId] uniqueidentifier NULL;

CREATE TABLE [StudentWeaknesses] (
    [Id] uniqueidentifier NOT NULL,
    [StudentId] uniqueidentifier NOT NULL,
    [TopicId] uniqueidentifier NOT NULL,
    [TopicNameSnapshot] nvarchar(255) NOT NULL,
    [CurrentProficiencyPercent] decimal(5,2) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [LastUpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_StudentWeaknesses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentWeaknesses_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([UserId]) ON DELETE CASCADE
);

CREATE TABLE [StudentWeaknessHistories] (
    [Id] uniqueidentifier NOT NULL,
    [StudentWeaknessId] uniqueidentifier NOT NULL,
    [PreviousProficiencyPercent] decimal(5,2) NOT NULL,
    [NewProficiencyPercent] decimal(5,2) NOT NULL,
    [PreviousIsActive] bit NOT NULL,
    [NewIsActive] bit NOT NULL,
    [SourceAttemptId] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_StudentWeaknessHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentWeaknessHistories_StudentExamAttempts_SourceAttemptId] FOREIGN KEY ([SourceAttemptId]) REFERENCES [StudentExamAttempts] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_StudentWeaknessHistories_StudentWeaknesses_StudentWeaknessId] FOREIGN KEY ([StudentWeaknessId]) REFERENCES [StudentWeaknesses] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [WeaknessReviews] (
    [Id] uniqueidentifier NOT NULL,
    [StudentWeaknessId] uniqueidentifier NOT NULL,
    [ProficiencyAtGeneration] decimal(5,2) NOT NULL,
    [AiExplanation] nvarchar(max) NOT NULL,
    [KeyConcepts] nvarchar(max) NOT NULL,
    [CommonMistakes] nvarchar(max) NOT NULL,
    [Recommendations] nvarchar(max) NOT NULL,
    [GeneratedAt] datetime2 NOT NULL,
    [IsOutdated] bit NOT NULL,
    CONSTRAINT [PK_WeaknessReviews] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WeaknessReviews_StudentWeaknesses_StudentWeaknessId] FOREIGN KEY ([StudentWeaknessId]) REFERENCES [StudentWeaknesses] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_StudentWeaknesses_StudentId_TopicId] ON [StudentWeaknesses] ([StudentId], [TopicId]);

CREATE INDEX [IX_StudentWeaknessHistories_SourceAttemptId] ON [StudentWeaknessHistories] ([SourceAttemptId]);

CREATE INDEX [IX_StudentWeaknessHistories_StudentWeaknessId] ON [StudentWeaknessHistories] ([StudentWeaknessId]);

CREATE UNIQUE INDEX [IX_WeaknessReviews_StudentWeaknessId] ON [WeaknessReviews] ([StudentWeaknessId]) WHERE [IsOutdated] = 0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260823174429_AddWeaknessAndHistoryModels', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [Notifications] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Title] nvarchar(150) NOT NULL,
    [Message] nvarchar(500) NOT NULL,
    [Type] nvarchar(max) NOT NULL,
    [Link] nvarchar(255) NULL,
    [Read] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
);

CREATE INDEX [IX_Notifications_CreatedAt] ON [Notifications] ([CreatedAt]);

CREATE INDEX [IX_Notifications_Read] ON [Notifications] ([Read]);

CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260823224902_AddNotificationSystem', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [StudentExamAttempts] ADD [MaxScore] decimal(18,2) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260824034535_AddMaxScoreToAttempt', N'10.0.10');

COMMIT;
GO

