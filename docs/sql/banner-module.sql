IF OBJECT_ID('dbo.Banners', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Banners
    (
        banner_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        name NVARCHAR(200) NOT NULL,
        desktop_image_url NVARCHAR(500) NOT NULL,
        mobile_image_url NVARCHAR(500) NULL,
        alt_text NVARCHAR(255) NULL,
        notes NVARCHAR(MAX) NULL,
        is_active BIT NOT NULL CONSTRAINT DF_Banners_IsActive DEFAULT(1),
        is_deleted BIT NOT NULL CONSTRAINT DF_Banners_IsDeleted DEFAULT(0),
        created_at DATETIME NULL CONSTRAINT DF_Banners_CreatedAt DEFAULT(GETDATE()),
        updated_at DATETIME NULL CONSTRAINT DF_Banners_UpdatedAt DEFAULT(GETDATE())
    );
END;

IF OBJECT_ID('dbo.BannerPositions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BannerPositions
    (
        banner_position_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        code VARCHAR(120) NOT NULL,
        name NVARCHAR(200) NOT NULL,
        description NVARCHAR(500) NULL,
        is_active BIT NOT NULL CONSTRAINT DF_BannerPositions_IsActive DEFAULT(1),
        created_at DATETIME NULL CONSTRAINT DF_BannerPositions_CreatedAt DEFAULT(GETDATE()),
        updated_at DATETIME NULL CONSTRAINT DF_BannerPositions_UpdatedAt DEFAULT(GETDATE())
    );
END;

IF OBJECT_ID('dbo.BannerTargets', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BannerTargets
    (
        banner_target_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        banner_id INT NOT NULL,
        target_type VARCHAR(30) NOT NULL,
        category_id INT NULL,
        brand_id INT NULL,
        product_id INT NULL,
        external_url NVARCHAR(500) NULL,
        open_in_new_tab BIT NOT NULL CONSTRAINT DF_BannerTargets_OpenInNewTab DEFAULT(0),
        created_at DATETIME NULL CONSTRAINT DF_BannerTargets_CreatedAt DEFAULT(GETDATE()),
        updated_at DATETIME NULL CONSTRAINT DF_BannerTargets_UpdatedAt DEFAULT(GETDATE())
    );
END;

IF OBJECT_ID('dbo.BannerPositionMaps', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BannerPositionMaps
    (
        banner_position_map_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        banner_id INT NOT NULL,
        banner_position_id INT NOT NULL,
        display_category_id INT NULL,
        priority INT NOT NULL CONSTRAINT DF_BannerPositionMaps_Priority DEFAULT(100),
        start_at DATETIME2 NULL,
        end_at DATETIME2 NULL,
        is_default BIT NOT NULL CONSTRAINT DF_BannerPositionMaps_IsDefault DEFAULT(0),
        is_active BIT NOT NULL CONSTRAINT DF_BannerPositionMaps_IsActive DEFAULT(1),
        created_at DATETIME NULL CONSTRAINT DF_BannerPositionMaps_CreatedAt DEFAULT(GETDATE()),
        updated_at DATETIME NULL CONSTRAINT DF_BannerPositionMaps_UpdatedAt DEFAULT(GETDATE())
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Banners_is_deleted_is_active' AND object_id = OBJECT_ID('dbo.Banners'))
    CREATE INDEX IX_Banners_is_deleted_is_active ON dbo.Banners(is_deleted, is_active);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BannerPositions_code' AND object_id = OBJECT_ID('dbo.BannerPositions'))
    CREATE UNIQUE INDEX IX_BannerPositions_code ON dbo.BannerPositions(code);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BannerTargets_banner_id' AND object_id = OBJECT_ID('dbo.BannerTargets'))
    CREATE UNIQUE INDEX IX_BannerTargets_banner_id ON dbo.BannerTargets(banner_id);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BannerTargets_TargetLookup' AND object_id = OBJECT_ID('dbo.BannerTargets'))
    CREATE INDEX IX_BannerTargets_TargetLookup ON dbo.BannerTargets(target_type, category_id, brand_id, product_id);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BannerPositionMaps_PositionScopePriority' AND object_id = OBJECT_ID('dbo.BannerPositionMaps'))
    CREATE INDEX IX_BannerPositionMaps_PositionScopePriority ON dbo.BannerPositionMaps(banner_position_id, display_category_id, is_active, is_default, priority);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_BannerTargets_Banners')
    ALTER TABLE dbo.BannerTargets ADD CONSTRAINT FK_BannerTargets_Banners FOREIGN KEY (banner_id) REFERENCES dbo.Banners(banner_id) ON DELETE CASCADE;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_BannerTargets_Categories')
    ALTER TABLE dbo.BannerTargets ADD CONSTRAINT FK_BannerTargets_Categories FOREIGN KEY (category_id) REFERENCES dbo.Category(category_id);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_BannerTargets_Brands')
    ALTER TABLE dbo.BannerTargets ADD CONSTRAINT FK_BannerTargets_Brands FOREIGN KEY (brand_id) REFERENCES dbo.Brand(BrandId);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_BannerTargets_Products')
    ALTER TABLE dbo.BannerTargets ADD CONSTRAINT FK_BannerTargets_Products FOREIGN KEY (product_id) REFERENCES dbo.Product(product_id);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_BannerPositionMaps_Banners')
    ALTER TABLE dbo.BannerPositionMaps ADD CONSTRAINT FK_BannerPositionMaps_Banners FOREIGN KEY (banner_id) REFERENCES dbo.Banners(banner_id) ON DELETE CASCADE;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_BannerPositionMaps_BannerPositions')
    ALTER TABLE dbo.BannerPositionMaps ADD CONSTRAINT FK_BannerPositionMaps_BannerPositions FOREIGN KEY (banner_position_id) REFERENCES dbo.BannerPositions(banner_position_id) ON DELETE CASCADE;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_BannerPositionMaps_Categories')
    ALTER TABLE dbo.BannerPositionMaps ADD CONSTRAINT FK_BannerPositionMaps_Categories FOREIGN KEY (display_category_id) REFERENCES dbo.Category(category_id);
