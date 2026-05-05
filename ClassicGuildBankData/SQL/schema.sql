-- =============================================================================
-- schema.sql
-- Idempotent schema creation replacing all EF Core migrations.
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[__EFMigrationsHistory]') AND type = N'U')
CREATE TABLE [__EFMigrationsHistory] (
    [MigrationId]    nvarchar(150) NOT NULL,
    [ProductVersion] nvarchar(32)  NOT NULL,
    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
);

-- ---------------------------------------------------------------------------
-- Identity tables
-- ---------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetRoles]') AND type = N'U')
CREATE TABLE [AspNetRoles] (
    [Id]               nvarchar(450) NOT NULL,
    [Name]             nvarchar(256) NULL,
    [NormalizedName]   nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'RoleNameIndex' AND object_id = OBJECT_ID(N'[dbo].[AspNetRoles]'))
    CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND type = N'U')
CREATE TABLE [AspNetUsers] (
    [Id]                   nvarchar(450)     NOT NULL,
    [UserName]             nvarchar(256)     NULL,
    [NormalizedUserName]   nvarchar(256)     NULL,
    [Email]                nvarchar(256)     NULL,
    [NormalizedEmail]      nvarchar(256)     NULL,
    [EmailConfirmed]       bit               NOT NULL,
    [PasswordHash]         nvarchar(max)     NULL,
    [SecurityStamp]        nvarchar(max)     NULL,
    [ConcurrencyStamp]     nvarchar(max)     NULL,
    [PhoneNumber]          nvarchar(max)     NULL,
    [PhoneNumberConfirmed] bit               NOT NULL,
    [TwoFactorEnabled]     bit               NOT NULL,
    [LockoutEnd]           datetimeoffset(7) NULL,
    [LockoutEnabled]       bit               NOT NULL,
    [AccessFailedCount]    int               NOT NULL,
    [LastSelectedGuildId]  uniqueidentifier  NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    [PatreonAccessToken]   nvarchar(max)     NULL,
    [PatreonRefreshToken]  nvarchar(max)     NULL,
    [PatreonExpiration]    int               NOT NULL DEFAULT 0,
    [LastUpdated]          datetime2(7)      NULL,
    [Patreon_Id]           nvarchar(max)     NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'EmailIndex' AND object_id = OBJECT_ID(N'[dbo].[AspNetUsers]'))
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UserNameIndex' AND object_id = OBJECT_ID(N'[dbo].[AspNetUsers]'))
    CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetRoleClaims]') AND type = N'U')
CREATE TABLE [AspNetRoleClaims] (
    [Id]         int           NOT NULL IDENTITY,
    [RoleId]     nvarchar(450) NOT NULL,
    [ClaimType]  nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles]([Id]) ON DELETE CASCADE
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetRoleClaims_RoleId' AND object_id = OBJECT_ID(N'[dbo].[AspNetRoleClaims]'))
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserClaims]') AND type = N'U')
CREATE TABLE [AspNetUserClaims] (
    [Id]         int           NOT NULL IDENTITY,
    [UserId]     nvarchar(450) NOT NULL,
    [ClaimType]  nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUserClaims_UserId' AND object_id = OBJECT_ID(N'[dbo].[AspNetUserClaims]'))
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserLogins]') AND type = N'U')
CREATE TABLE [AspNetUserLogins] (
    [LoginProvider]       nvarchar(450) NOT NULL,
    [ProviderKey]         nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId]              nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUserLogins_UserId' AND object_id = OBJECT_ID(N'[dbo].[AspNetUserLogins]'))
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserRoles]') AND type = N'U')
CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUserRoles_RoleId' AND object_id = OBJECT_ID(N'[dbo].[AspNetUserRoles]'))
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserTokens]') AND type = N'U')
CREATE TABLE [AspNetUserTokens] (
    [UserId]        nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name]          nvarchar(450) NOT NULL,
    [Value]         nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);

-- ---------------------------------------------------------------------------
-- Application tables
-- ---------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Item]') AND type = N'U')
CREATE TABLE [Item] (
    [Id]       int           NOT NULL IDENTITY,
    [Name]     nvarchar(max) NULL,
    [Icon]     nvarchar(max) NULL,
    [Quality]  nvarchar(max) NULL,
    [Class]    int           NOT NULL DEFAULT 0,
    [Subclass] int           NULL,
    [RuName]   nvarchar(max) NULL,
    [DeName]   nvarchar(max) NULL,
    [FrName]   nvarchar(max) NULL,
    [CnName]   nvarchar(max) NULL,
    [ItName]   nvarchar(max) NULL,
    [EsName]   nvarchar(max) NULL,
    [KoName]   nvarchar(max) NULL,
    [PtName]   nvarchar(max) NULL,
    CONSTRAINT [PK_Item] PRIMARY KEY ([Id])
);

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Guild]') AND type = N'U')
CREATE TABLE [Guild] (
    [Id]                uniqueidentifier NOT NULL,
    [UserId]            nvarchar(max)    NULL,
    [Name]              nvarchar(max)    NULL,
    [InviteUrl]         nvarchar(max)    NULL,
    [PublicLinkEnabled] bit              NOT NULL DEFAULT 0,
    [PublicUrl]         nvarchar(max)    NULL,
    CONSTRAINT [PK_Guild] PRIMARY KEY ([Id])
);

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Character]') AND type = N'U')
CREATE TABLE [Character] (
    [Id]          uniqueidentifier NOT NULL,
    [Name]        nvarchar(max)    NULL,
    [Gold]        int              NOT NULL DEFAULT 0,
    [LastUpdated] datetime2(7)     NULL,
    [GuildId]     uniqueidentifier NOT NULL,
    CONSTRAINT [PK_Character] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Character_Guild_GuildId] FOREIGN KEY ([GuildId]) REFERENCES [Guild]([Id]) ON DELETE CASCADE
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Character_GuildId' AND object_id = OBJECT_ID(N'[dbo].[Character]'))
    CREATE INDEX [IX_Character_GuildId] ON [Character] ([GuildId]);

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[GuildRole]') AND type = N'U')
CREATE TABLE [GuildRole] (
    [GuildId]     uniqueidentifier NOT NULL,
    [UserId]      nvarchar(450)    NOT NULL,
    [DisplayName] nvarchar(max)    NULL,
    [CanUpload]   bit              NOT NULL DEFAULT 0,
    CONSTRAINT [PK_GuildRole] PRIMARY KEY ([GuildId], [UserId]),
    CONSTRAINT [FK_GuildRole_Guild_GuildId] FOREIGN KEY ([GuildId]) REFERENCES [Guild]([Id]) ON DELETE CASCADE
);

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Bag]') AND type = N'U')
CREATE TABLE [Bag] (
    [Id]             uniqueidentifier NOT NULL,
    [CharacterId]    uniqueidentifier NOT NULL,
    [ItemId]         int              NULL,
    [isBank]         bit              NOT NULL,
    [BagContainerId] int              NOT NULL DEFAULT 0,
    CONSTRAINT [PK_Bag] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Bag_Character_CharacterId] FOREIGN KEY ([CharacterId]) REFERENCES [Character]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Bag_Item_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [Item]([Id])
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Bag_CharacterId' AND object_id = OBJECT_ID(N'[dbo].[Bag]'))
    CREATE INDEX [IX_Bag_CharacterId] ON [Bag] ([CharacterId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Bag_ItemId' AND object_id = OBJECT_ID(N'[dbo].[Bag]'))
    CREATE INDEX [IX_Bag_ItemId] ON [Bag] ([ItemId]);

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BagSlot]') AND type = N'U')
CREATE TABLE [BagSlot] (
    [BagId]    uniqueidentifier NOT NULL,
    [SlotId]   int              NOT NULL,
    [ItemId]   int              NULL,
    [Quantity] int              NOT NULL DEFAULT 0,
    CONSTRAINT [PK_BagSlot] PRIMARY KEY ([BagId], [SlotId]),
    CONSTRAINT [FK_BagSlot_Bag_BagId] FOREIGN KEY ([BagId]) REFERENCES [Bag]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BagSlot_Item_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [Item]([Id]) ON DELETE SET NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BagSlot_ItemId' AND object_id = OBJECT_ID(N'[dbo].[BagSlot]'))
    CREATE INDEX [IX_BagSlot_ItemId] ON [BagSlot] ([ItemId]);

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Transaction]') AND type = N'U')
CREATE TABLE [Transaction] (
    [Id]              uniqueidentifier NOT NULL,
    [GuildId]         uniqueidentifier NOT NULL,
    [Type]            nvarchar(max)    NULL,
    [CharacterName]   nvarchar(max)    NULL,
    [Money]           int              NULL,
    [TransactionDate] datetime2(7)     NOT NULL,
    CONSTRAINT [PK_Transaction] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Transaction_Guild_GuildId] FOREIGN KEY ([GuildId]) REFERENCES [Guild]([Id]) ON DELETE CASCADE
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Transaction_GuildId' AND object_id = OBJECT_ID(N'[dbo].[Transaction]'))
    CREATE INDEX [IX_Transaction_GuildId] ON [Transaction] ([GuildId]);

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TransactionDetail]') AND type = N'U')
CREATE TABLE [TransactionDetail] (
    [Id]            uniqueidentifier NOT NULL,
    [TransactionId] uniqueidentifier NOT NULL,
    [ItemId]        int              NULL,
    [Quantity]      int              NOT NULL,
    CONSTRAINT [PK_TransactionDetail] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TransactionDetail_Transaction_TransactionId] FOREIGN KEY ([TransactionId]) REFERENCES [Transaction]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TransactionDetail_Item_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [Item]([Id]) ON DELETE SET NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TransactionDetail_TransactionId' AND object_id = OBJECT_ID(N'[dbo].[TransactionDetail]'))
    CREATE INDEX [IX_TransactionDetail_TransactionId] ON [TransactionDetail] ([TransactionId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TransactionDetail_ItemId' AND object_id = OBJECT_ID(N'[dbo].[TransactionDetail]'))
    CREATE INDEX [IX_TransactionDetail_ItemId] ON [TransactionDetail] ([ItemId]);

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ItemRequest]') AND type = N'U')
CREATE TABLE [ItemRequest] (
    [Id]            uniqueidentifier NOT NULL,
    [GuildId]       uniqueidentifier NOT NULL,
    [CharacterName] nvarchar(max)    NULL,
    [Gold]          int              NOT NULL,
    [Status]        nvarchar(max)    NULL,
    [Reason]        nvarchar(max)    NULL,
    [UserId]        nvarchar(max)    NULL,
    [DateRequested] datetime2(7)     NULL,
    CONSTRAINT [PK_ItemRequest] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItemRequest_Guild_GuildId] FOREIGN KEY ([GuildId]) REFERENCES [Guild]([Id]) ON DELETE CASCADE
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ItemRequest_GuildId' AND object_id = OBJECT_ID(N'[dbo].[ItemRequest]'))
    CREATE INDEX [IX_ItemRequest_GuildId] ON [ItemRequest] ([GuildId]);

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ItemRequestDetail]') AND type = N'U')
CREATE TABLE [ItemRequestDetail] (
    [Id]            uniqueidentifier NOT NULL,
    [ItemRequestId] uniqueidentifier NOT NULL,
    [ItemId]        int              NULL,
    [Quantity]      int              NOT NULL,
    CONSTRAINT [PK_ItemRequestDetail] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItemRequestDetail_ItemRequest_ItemRequestId] FOREIGN KEY ([ItemRequestId]) REFERENCES [ItemRequest]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ItemRequestDetail_Item_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [Item]([Id]) ON DELETE SET NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ItemRequestDetail_ItemRequestId' AND object_id = OBJECT_ID(N'[dbo].[ItemRequestDetail]'))
    CREATE INDEX [IX_ItemRequestDetail_ItemRequestId] ON [ItemRequestDetail] ([ItemRequestId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ItemRequestDetail_ItemId' AND object_id = OBJECT_ID(N'[dbo].[ItemRequestDetail]'))
    CREATE INDEX [IX_ItemRequestDetail_ItemId] ON [ItemRequestDetail] ([ItemId]);

-- ---------------------------------------------------------------------------
-- Mark all migrations as applied so Database.Migrate() is a no-op on startup
-- ---------------------------------------------------------------------------

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
SELECT m.[MigrationId], '9.0.4'
FROM (VALUES
    ('20190609224636_init'),
    ('20190706021009_PatreonUser'),
    ('20190713165257_LastSelectedGuild'),
    ('20190731000846_url'),
    ('20190802090540_guild-member'),
    ('20190823013045_BagSlotQuantity'),
    ('20190825170510_ItemClassification'),
    ('20190827201447_Bag_Item_Relation'),
    ('20190830023919_public-link'),
    ('20190905001623_LocaleNames'),
    ('20190908001848_PtKoNames'),
    ('20190913102858_AddCanUpload'),
    ('20190923020404_transactions'),
    ('20190928104008_transaction2'),
    ('20191126021813_last-updated'),
    ('20191207105618_item-request'),
    ('20191214152651_DateRequested'),
    ('20191217135247_TransactionDetails'),
    ('20200327172510_BagContainerId_Notes')
) AS m([MigrationId])
WHERE NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = m.[MigrationId]
);
