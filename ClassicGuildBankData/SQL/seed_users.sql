-- =============================================================================
-- seed_users.sql
-- Seeds the default admin user, guild, character, and demo inventory.
-- Idempotent - safe to run multiple times.
-- =============================================================================

DECLARE @PasswordHash  NVARCHAR(MAX) = N'AQAAAAEAACcQAAAAEEsXCOIo5lyFGVDqKAtkv2gLWoUXrwWxy6xdjUyZm/qF7A2I1r8o7/Ix7wxTQqsdWQ==';
DECLARE @UserId        NVARCHAR(450) = N'00000001-0000-0000-0000-000000000001';
DECLARE @GuildId       UNIQUEIDENTIFIER = '72D8FD17-9D7B-47E3-882E-E81CD8FB91D0';
DECLARE @CharacterId   UNIQUEIDENTIFIER = '1A6C7066-F013-4378-AA56-24C53F963288';
DECLARE @BankBagId     UNIQUEIDENTIFIER = 'F5091DD3-A11F-4864-A6DF-2A78594A90A9';
DECLARE @BankBag1Id    UNIQUEIDENTIFIER = 'D69A53A8-BA4D-4FEA-94CE-61CE613D00F6';
DECLARE @BackpackId    UNIQUEIDENTIFIER = '484D54A0-77DF-4446-AC56-430CC049042F';

-- ---------------------------------------------------------------------------
-- User
-- ---------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [Id] = @UserId)
INSERT INTO [AspNetUsers] (
    [Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail],
    [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp],
    [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnabled], [AccessFailedCount],
    [PatreonExpiration]
) VALUES (
    @UserId, N'athielking', N'ATHIELKING', N'athielking@gmail.com', N'ATHIELKING@GMAIL.COM',
    1, @PasswordHash, CONVERT(NVARCHAR(MAX), NEWID()), CONVERT(NVARCHAR(MAX), NEWID()),
    0, 0, 1, 0,
    0
);

-- ---------------------------------------------------------------------------
-- Guild
-- ---------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM [Guild] WHERE [Id] = @GuildId)
INSERT INTO [Guild] ([Id], [UserId], [Name], [PublicLinkEnabled])
VALUES (@GuildId, @UserId, N'Loch Modan Yacht Club', 0);

-- Guild member
IF NOT EXISTS (SELECT 1 FROM [GuildRole] WHERE [GuildId] = @GuildId AND [UserId] = @UserId)
INSERT INTO [GuildRole] ([GuildId], [UserId], [DisplayName], [CanUpload])
VALUES (@GuildId, @UserId, N'athielking', 0);

-- ---------------------------------------------------------------------------
-- Character
-- ---------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM [Character] WHERE [Id] = @CharacterId)
INSERT INTO [Character] ([Id], [Name], [GuildId], [Gold])
VALUES (@CharacterId, N'athielking', @GuildId, 0);

-- ---------------------------------------------------------------------------
-- Bags
-- ---------------------------------------------------------------------------

-- Main bank (28-slot, BagContainerId = -1)
IF NOT EXISTS (SELECT 1 FROM [Bag] WHERE [Id] = @BankBagId)
INSERT INTO [Bag] ([Id], [CharacterId], [ItemId], [isBank], [BagContainerId])
VALUES (@BankBagId, @CharacterId, NULL, 1, -1);

-- Bank bag 1: 18-slot Traveler's Backpack in bank slot 5 (ItemId 17966)
IF NOT EXISTS (SELECT 1 FROM [Bag] WHERE [Id] = @BankBag1Id)
INSERT INTO [Bag] ([Id], [CharacterId], [ItemId], [isBank], [BagContainerId])
VALUES (@BankBag1Id, @CharacterId, 17966, 1, 5);

-- Backpack (16-slot, BagContainerId = 0)
IF NOT EXISTS (SELECT 1 FROM [Bag] WHERE [Id] = @BackpackId)
INSERT INTO [Bag] ([Id], [CharacterId], [ItemId], [isBank], [BagContainerId])
VALUES (@BackpackId, @CharacterId, NULL, 0, 0);

-- ---------------------------------------------------------------------------
-- Bag slots
-- ---------------------------------------------------------------------------

-- Main bank: 28 slots
INSERT INTO [BagSlot] ([BagId], [SlotId], [ItemId], [Quantity])
SELECT @BankBagId, s.[SlotId],
    CASE s.[SlotId] WHEN 0 THEN 14047 WHEN 1 THEN 13445 WHEN 4 THEN 7078 ELSE NULL END,
    0
FROM (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),
             (14),(15),(16),(17),(18),(19),(20),(21),(22),(23),(24),(25),(26),(27)) AS s([SlotId])
WHERE NOT EXISTS (SELECT 1 FROM [BagSlot] WHERE [BagId] = @BankBagId AND [SlotId] = s.[SlotId]);

-- Bank bag 1: 18 slots
INSERT INTO [BagSlot] ([BagId], [SlotId], [ItemId], [Quantity])
SELECT @BankBag1Id, s.[SlotId],
    CASE s.[SlotId] WHEN 0 THEN 13457 ELSE NULL END,
    0
FROM (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),(15),(16),(17)) AS s([SlotId])
WHERE NOT EXISTS (SELECT 1 FROM [BagSlot] WHERE [BagId] = @BankBag1Id AND [SlotId] = s.[SlotId]);

-- Backpack: 16 slots
INSERT INTO [BagSlot] ([BagId], [SlotId], [ItemId], [Quantity])
SELECT @BackpackId, s.[SlotId],
    CASE s.[SlotId] WHEN 0 THEN 19019 WHEN 1 THEN 17182 ELSE NULL END,
    0
FROM (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),(15)) AS s([SlotId])
WHERE NOT EXISTS (SELECT 1 FROM [BagSlot] WHERE [BagId] = @BackpackId AND [SlotId] = s.[SlotId]);
