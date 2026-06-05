using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tech_Store.Models;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<Attribute> Attributes { get; set; }

    public virtual DbSet<AttributeValue> AttributeValues { get; set; }

    public virtual DbSet<Banner> Banners { get; set; }

    public virtual DbSet<BannerPosition> BannerPositions { get; set; }

    public virtual DbSet<BannerPositionMap> BannerPositionMaps { get; set; }

    public virtual DbSet<BannerTarget> BannerTargets { get; set; }
    public virtual DbSet<Brand> Brands { get; set; }
    public virtual DbSet<BrandCategory> BrandCategories { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<Gallery> Galleries { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<PendingSePayCheckout> PendingSePayCheckouts { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }
    public virtual DbSet<Specs> Species { get; set; }
    public virtual DbSet<SpecValue> SpecValues { get; set;}
    public virtual DbSet<InventoryTransactions> InventoryTransactions { get; set; }

    public virtual DbSet<InventoryTransactionsDetail> InventoryTransactionsDetail { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Setting> Settings { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserProductEvent> UserProductEvents { get; set; }

    public virtual DbSet<UserNotification> UserNotifications { get; set; }

    public virtual DbSet<VariantAttribute> VariantAttributes { get; set; }

    public virtual DbSet<VarientProduct> VarientProducts { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    public virtual DbSet<Wishlist> Wishlists { get; set; }


    public override int SaveChanges()
    {
        ApplyTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTimestamps()
    {
        var now = DateTime.Now;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Added && entry.State != EntityState.Modified)
            {
                continue;
            }

            var createdAt = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAt");
            var updatedAt = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");

            if (entry.State == EntityState.Added)
            {
                if (createdAt != null && createdAt.CurrentValue == null)
                {
                    createdAt.CurrentValue = now;
                }

                if (updatedAt != null && updatedAt.CurrentValue == null)
                {
                    updatedAt.CurrentValue = now;
                }
            }
            else
            {
                if (createdAt != null)
                {
                    createdAt.IsModified = false;
                }

                if (updatedAt != null)
                {
                    updatedAt.CurrentValue = now;
                }
            }
        }
    }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        static void ConfigureTimestamps<TEntity>(EntityTypeBuilder<TEntity> entity)
            where TEntity : class
        {
            entity.Property<DateTime?>("CreatedAt")
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property<DateTime?>("UpdatedAt")
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
        }

        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.AddressId).HasName("PK__Address__CAA247C87ED1B05A");

            entity.ToTable("Address");
            ConfigureTimestamps(entity);

            entity.Property(e => e.AddressId).HasColumnName("address_id");
            entity.Property(e => e.AddressLine)
                .HasMaxLength(100)
                .HasColumnName("address_line");
            entity.Property(e => e.District)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("district");
            entity.Property(e => e.Province)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("province");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Ward)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ward");

            entity.HasOne(d => d.User).WithMany(p => p.Addresses)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Address_User");
        });

        modelBuilder.Entity<Attribute>(entity =>
        {
            entity.ToTable("Attribute");
            ConfigureTimestamps(entity);

            entity.Property(e => e.AttributeId).HasColumnName("attributeId");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.IsActive).HasColumnName("isActive");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.SortOrder).HasColumnName("sortOrder");
        });

        modelBuilder.Entity<AttributeValue>(entity =>
        {
            entity.ToTable("AttributeValue");
            ConfigureTimestamps(entity);

            entity.Property(e => e.AttributeId).HasColumnName("attributeId");
            entity.Property(e => e.Value)
                .HasMaxLength(250)
                .HasColumnName("value");

            entity.HasOne(d => d.Attribute).WithMany(p => p.AttributeValues)
                .HasForeignKey(d => d.AttributeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttributeValue_Attribute");
        });

        modelBuilder.Entity<Banner>(entity =>
        {
            entity.ToTable("Banners");
            ConfigureTimestamps(entity);

            entity.HasIndex(e => new { e.IsDeleted, e.IsActive });

            entity.Property(e => e.BannerId).HasColumnName("banner_id");
            entity.Property(e => e.AltText)
                .HasMaxLength(255)
                .HasColumnName("alt_text");
            entity.Property(e => e.DesktopImageUrl)
                .HasMaxLength(500)
                .HasColumnName("desktop_image_url");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.MobileImageUrl)
                .HasMaxLength(500)
                .HasColumnName("mobile_image_url");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.Notes).HasColumnName("notes");

            entity.HasData(
                new Banner
                {
                    BannerId = 1001,
                    Name = "Banner mặc định hero 1",
                    DesktopImageUrl = "https://images.macrumors.com/t/H0wescJV-Z32v37P2JxpOBsFX8c=/2500x0/filters:no_upscale()/article-new/2024/11/iPhone-17-Pro-Dual-Tone-Rectangle-Feature-1.jpg",
                    AltText = "Khuyến mãi điện thoại nổi bật",
                    IsActive = true,
                    IsDeleted = false
                },
                new Banner
                {
                    BannerId = 1002,
                    Name = "Banner mặc định hero 2",
                    DesktopImageUrl = "https://i.gadgets360cdn.com/large/samsung_galaxy_s25_ultra_technizoconcept_inline_1731133022562.jpg",
                    AltText = "Khuyến mãi Samsung nổi bật",
                    IsActive = true,
                    IsDeleted = false
                },
                new Banner
                {
                    BannerId = 1003,
                    Name = "Banner mặc định hero 3",
                    DesktopImageUrl = "https://cdn.mos.cms.futurecdn.net/hyKSYAeHLcrRtExozn7EBA-1200-80.jpg",
                    AltText = "Khuyến mãi flagship mới",
                    IsActive = true,
                    IsDeleted = false
                },
                new Banner
                {
                    BannerId = 1004,
                    Name = "Banner mặc định promo 1",
                    DesktopImageUrl = "/Client/asset/img/R.png",
                    AltText = "Khuyến mãi iPhone",
                    IsActive = true,
                    IsDeleted = false
                },
                new Banner
                {
                    BannerId = 1005,
                    Name = "Banner mặc định promo 2",
                    DesktopImageUrl = "/Client/asset/img/right-banner-14-10.webp",
                    AltText = "Khuyến mãi Samsung",
                    IsActive = true,
                    IsDeleted = false
                },
                new Banner
                {
                    BannerId = 1101,
                    Name = "Category hero điện thoại",
                    DesktopImageUrl = "https://images.macrumors.com/t/H0wescJV-Z32v37P2JxpOBsFX8c=/2500x0/filters:no_upscale()/article-new/2024/11/iPhone-17-Pro-Dual-Tone-Rectangle-Feature-1.jpg",
                    AltText = "Banner điện thoại nổi bật",
                    IsActive = true,
                    IsDeleted = false
                },
                new Banner
                {
                    BannerId = 1102,
                    Name = "Category hero laptop",
                    DesktopImageUrl = "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?auto=format&fit=crop&w=1600&q=80",
                    AltText = "Banner laptop hiệu năng cao",
                    IsActive = true,
                    IsDeleted = false
                },
                new Banner
                {
                    BannerId = 1103,
                    Name = "Category hero tablet",
                    DesktopImageUrl = "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?auto=format&fit=crop&w=1600&q=80",
                    AltText = "Banner máy tính bảng học tập và giải trí",
                    IsActive = true,
                    IsDeleted = false
                },
                new Banner
                {
                    BannerId = 1104,
                    Name = "Category hero phụ kiện",
                    DesktopImageUrl = "https://images.unsplash.com/photo-1585338447937-7082f8fc763d?auto=format&fit=crop&w=1600&q=80",
                    AltText = "Banner phụ kiện công nghệ",
                    IsActive = true,
                    IsDeleted = false
                },
                new Banner
                {
                    BannerId = 1105,
                    Name = "Brand hero Apple",
                    DesktopImageUrl = "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?auto=format&fit=crop&w=1600&q=80",
                    AltText = "Banner Apple",
                    IsActive = true,
                    IsDeleted = false
                },
                new Banner
                {
                    BannerId = 1106,
                    Name = "Brand hero Samsung",
                    DesktopImageUrl = "https://images.unsplash.com/photo-1610945265064-0e34e5519bbf?auto=format&fit=crop&w=1600&q=80",
                    AltText = "Banner Samsung",
                    IsActive = true,
                    IsDeleted = false
                },
                new Banner
                {
                    BannerId = 1107,
                    Name = "Brand hero ASUS",
                    DesktopImageUrl = "https://images.unsplash.com/photo-1593642702744-d377ab507dc8?auto=format&fit=crop&w=1600&q=80",
                    AltText = "Banner ASUS",
                    IsActive = true,
                    IsDeleted = false
                },
                new Banner
                {
                    BannerId = 1108,
                    Name = "Brand hero Lenovo",
                    DesktopImageUrl = "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?auto=format&fit=crop&w=1600&q=80",
                    AltText = "Banner Lenovo",
                    IsActive = true,
                    IsDeleted = false
                });
        });

        modelBuilder.Entity<BannerPosition>(entity =>
        {
            entity.ToTable("BannerPositions");
            ConfigureTimestamps(entity);

            entity.HasIndex(e => e.Code).IsUnique();

            entity.Property(e => e.BannerPositionId).HasColumnName("banner_position_id");
            entity.Property(e => e.Code)
                .HasMaxLength(120)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");

            entity.HasData(
                new BannerPosition { BannerPositionId = 1, Code = "home-hero-main", Name = "Hero chính trang chủ", Description = "Carousel lớn giữa trang chủ", IsActive = true },
                new BannerPosition { BannerPositionId = 2, Code = "home-hero-promo", Name = "Promo phụ trang chủ", Description = "Hai banner phụ bên phải hero trang chủ", IsActive = true },
                new BannerPosition { BannerPositionId = 3, Code = "category-hero", Name = "Hero danh mục", Description = "Banner đầu trang danh mục", IsActive = true },
                new BannerPosition { BannerPositionId = 4, Code = "brand-hero", Name = "Hero thương hiệu", Description = "Banner đầu trang thương hiệu", IsActive = true });
        });

        modelBuilder.Entity<BannerPositionMap>(entity =>
        {
            entity.ToTable("BannerPositionMaps");
            ConfigureTimestamps(entity);

            entity.HasIndex(e => new { e.BannerPositionId, e.DisplayCategoryId, e.DisplayBrandId, e.IsActive, e.IsDefault, e.Priority })
                .HasDatabaseName("IX_BannerPositionMaps_PositionScopePriority");

            entity.Property(e => e.BannerPositionMapId).HasColumnName("banner_position_map_id");
            entity.Property(e => e.BannerId).HasColumnName("banner_id");
            entity.Property(e => e.BannerPositionId).HasColumnName("banner_position_id");
            entity.Property(e => e.DisplayCategoryId).HasColumnName("display_category_id");
            entity.Property(e => e.DisplayBrandId).HasColumnName("display_brand_id");
            entity.Property(e => e.EndAt).HasColumnName("end_at");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.IsDefault).HasColumnName("is_default");
            entity.Property(e => e.Priority).HasColumnName("priority");
            entity.Property(e => e.StartAt).HasColumnName("start_at");

            entity.HasOne(d => d.Banner).WithMany(p => p.BannerPositionMaps)
                .HasForeignKey(d => d.BannerId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_BannerPositionMaps_Banners");

            entity.HasOne(d => d.BannerPosition).WithMany(p => p.BannerPositionMaps)
                .HasForeignKey(d => d.BannerPositionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_BannerPositionMaps_BannerPositions");

            entity.HasOne(d => d.DisplayCategory).WithMany()
                .HasForeignKey(d => d.DisplayCategoryId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_BannerPositionMaps_Categories");

            entity.HasOne(d => d.DisplayBrand).WithMany()
                .HasForeignKey(d => d.DisplayBrandId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_BannerPositionMaps_Brands");

            entity.HasData(
                new BannerPositionMap { BannerPositionMapId = 3001, BannerId = 1001, BannerPositionId = 1, Priority = 300, IsDefault = true, IsActive = true },
                new BannerPositionMap { BannerPositionMapId = 3002, BannerId = 1002, BannerPositionId = 1, Priority = 200, IsDefault = true, IsActive = true },
                new BannerPositionMap { BannerPositionMapId = 3003, BannerId = 1003, BannerPositionId = 1, Priority = 100, IsDefault = true, IsActive = true },
                new BannerPositionMap { BannerPositionMapId = 3004, BannerId = 1004, BannerPositionId = 2, Priority = 200, IsDefault = true, IsActive = true },
                new BannerPositionMap { BannerPositionMapId = 3005, BannerId = 1005, BannerPositionId = 2, Priority = 100, IsDefault = true, IsActive = true },
                new BannerPositionMap { BannerPositionMapId = 3006, BannerId = 1001, BannerPositionId = 3, Priority = 100, IsDefault = true, IsActive = true },
                new BannerPositionMap { BannerPositionMapId = 3007, BannerId = 1002, BannerPositionId = 4, Priority = 100, IsDefault = true, IsActive = true },
                new BannerPositionMap { BannerPositionMapId = 3101, BannerId = 1101, BannerPositionId = 3, DisplayCategoryId = 1, Priority = 500, IsDefault = false, IsActive = true },
                new BannerPositionMap { BannerPositionMapId = 3102, BannerId = 1102, BannerPositionId = 3, DisplayCategoryId = 2, Priority = 500, IsDefault = false, IsActive = true },
                new BannerPositionMap { BannerPositionMapId = 3103, BannerId = 1103, BannerPositionId = 3, DisplayCategoryId = 3, Priority = 500, IsDefault = false, IsActive = true },
                new BannerPositionMap { BannerPositionMapId = 3104, BannerId = 1104, BannerPositionId = 3, DisplayCategoryId = 5, Priority = 500, IsDefault = false, IsActive = true },
                new BannerPositionMap { BannerPositionMapId = 3105, BannerId = 1105, BannerPositionId = 4, DisplayBrandId = 4, Priority = 500, IsDefault = false, IsActive = true },
                new BannerPositionMap { BannerPositionMapId = 3106, BannerId = 1106, BannerPositionId = 4, DisplayBrandId = 2, Priority = 500, IsDefault = false, IsActive = true },
                new BannerPositionMap { BannerPositionMapId = 3107, BannerId = 1107, BannerPositionId = 4, DisplayBrandId = 354036, Priority = 500, IsDefault = false, IsActive = true },
                new BannerPositionMap { BannerPositionMapId = 3108, BannerId = 1108, BannerPositionId = 4, DisplayBrandId = 9, Priority = 500, IsDefault = false, IsActive = true });
        });

        modelBuilder.Entity<BannerTarget>(entity =>
        {
            entity.ToTable("BannerTargets");
            ConfigureTimestamps(entity);

            entity.HasIndex(e => e.BannerId).IsUnique();
            entity.HasIndex(e => new { e.TargetType, e.CategoryId, e.BrandId, e.ProductId })
                .HasDatabaseName("IX_BannerTargets_TargetLookup");

            entity.Property(e => e.BannerTargetId).HasColumnName("banner_target_id");
            entity.Property(e => e.BannerId).HasColumnName("banner_id");
            entity.Property(e => e.BrandId).HasColumnName("brand_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.ExternalUrl)
                .HasMaxLength(500)
                .HasColumnName("external_url");
            entity.Property(e => e.OpenInNewTab).HasColumnName("open_in_new_tab");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.TargetType)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("target_type");

            entity.HasOne(d => d.Banner).WithOne(p => p.BannerTarget)
                .HasForeignKey<BannerTarget>(d => d.BannerId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_BannerTargets_Banners");

            entity.HasOne(d => d.Brand).WithMany()
                .HasForeignKey(d => d.BrandId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_BannerTargets_Brands");

            entity.HasOne(d => d.Category).WithMany()
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_BannerTargets_Categories");

            entity.HasOne(d => d.Product).WithMany()
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_BannerTargets_Products");

            entity.HasData(
                new BannerTarget { BannerTargetId = 2001, BannerId = 1001, TargetType = "url", ExternalUrl = "/Search?q=iphone", OpenInNewTab = false },
                new BannerTarget { BannerTargetId = 2002, BannerId = 1002, TargetType = "url", ExternalUrl = "/Search?q=samsung", OpenInNewTab = false },
                new BannerTarget { BannerTargetId = 2003, BannerId = 1003, TargetType = "url", ExternalUrl = "/Search?q=flagship", OpenInNewTab = false },
                new BannerTarget { BannerTargetId = 2004, BannerId = 1004, TargetType = "url", ExternalUrl = "/Search?q=iphone", OpenInNewTab = false },
                new BannerTarget { BannerTargetId = 2005, BannerId = 1005, TargetType = "url", ExternalUrl = "/Search?q=samsung", OpenInNewTab = false },
                new BannerTarget { BannerTargetId = 2101, BannerId = 1101, TargetType = "category", CategoryId = 1, OpenInNewTab = false },
                new BannerTarget { BannerTargetId = 2102, BannerId = 1102, TargetType = "category", CategoryId = 2, OpenInNewTab = false },
                new BannerTarget { BannerTargetId = 2103, BannerId = 1103, TargetType = "category", CategoryId = 3, OpenInNewTab = false },
                new BannerTarget { BannerTargetId = 2104, BannerId = 1104, TargetType = "category", CategoryId = 5, OpenInNewTab = false },
                new BannerTarget { BannerTargetId = 2105, BannerId = 1105, TargetType = "brand", BrandId = 4, OpenInNewTab = false },
                new BannerTarget { BannerTargetId = 2106, BannerId = 1106, TargetType = "brand", BrandId = 2, OpenInNewTab = false },
                new BannerTarget { BannerTargetId = 2107, BannerId = 1107, TargetType = "brand", BrandId = 354036, OpenInNewTab = false },
                new BannerTarget { BannerTargetId = 2108, BannerId = 1108, TargetType = "brand", BrandId = 9, OpenInNewTab = false });
        });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.ToTable("Brand");
            ConfigureTimestamps(entity);

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Image)
                .HasMaxLength(255)
                .HasColumnName("image");
            entity.Property(e => e.Name)
                .HasMaxLength(155)
                .HasColumnName("name");
            entity.Property(e => e.SortOrder)
                .HasDefaultValue(0)
                .HasColumnName("sort_order");

            entity.HasOne(d => d.Category).WithMany(p => p.Brands)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_Brand_Category");
        });

        modelBuilder.Entity<BrandCategory>(entity =>
        {
            entity.ToTable("BrandCategory");
            entity.HasKey(e => new { e.BrandId, e.CategoryId });

            entity.Property(e => e.BrandId).HasColumnName("brand_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");

            entity.HasOne(d => d.Brand)
                .WithMany(p => p.BrandCategories)
                .HasForeignKey(d => d.BrandId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_BrandCategory_Brand");

            entity.HasOne(d => d.Category)
                .WithMany(p => p.BrandCategories)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_BrandCategory_Category");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK__Cart__2EF52A27EFE9AF54");

            entity.ToTable("Cart");

            entity.Property(e => e.CartId).HasColumnName("cart_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Carts)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Cart__user_id__5535A963");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.CartItemId).HasName("PK__CartItem__5D9A6C6EEDADB402");

            entity.ToTable("CartItem");
            ConfigureTimestamps(entity);

            entity.Property(e => e.CartItemId).HasColumnName("cart_item_id");
            entity.Property(e => e.CartId).HasColumnName("cart_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.VarientProductId).HasColumnName("varientProduct_id");

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .HasConstraintName("FK_CartItem_Cart");

            entity.HasOne(d => d.Product).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CartItem_Product");

            entity.HasOne(d => d.VarientProduct).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.VarientProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CartItem_VarientProduct");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Category__D54EE9B4A1BB5889");

            entity.ToTable("Category");
            ConfigureTimestamps(entity);

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EngTitle)
                .HasMaxLength(50)
                .HasColumnName("eng_title");
            entity.Property(e => e.Image)
                .HasMaxLength(255)
                .HasColumnName("image");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Visible).HasColumnName("visible");
            entity.Property(e => e.VisibleOnCategoryPage)
                .HasDefaultValue(1)
                .HasColumnName("visible_on_category_page");
            entity.Property(e => e.VisibleOnOtherPages)
                .HasDefaultValue(1)
                .HasColumnName("visible_on_other_pages");
            entity.Property(e => e.SortOrder)
                .HasDefaultValue(0)
                .HasColumnName("sort_order");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.ToTable("Comment");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Product).WithMany(p => p.Comments)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Comment_Product");

            entity.HasOne(d => d.User).WithMany(p => p.Comments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Comment_User");
        });

        modelBuilder.Entity<Gallery>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK_image");

            entity.ToTable("Gallery");
            ConfigureTimestamps(entity);

            entity.Property(e => e.ImageId).HasColumnName("image_id");
            entity.Property(e => e.Path)
                .HasMaxLength(500)
                .HasColumnName("path");
            entity.Property(e => e.ProductId).HasColumnName("product_id");

            entity.HasOne(d => d.Product).WithMany(p => p.Galleries)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_image_Product");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E326CAE267F");

            entity.Property(e => e.NotificationId).HasColumnName("notificationID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("createdAt");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.RedirectUrl).HasColumnName("redirectUrl");
            entity.Property(e => e.Title)
                .HasMaxLength(100)
                .HasColumnName("title");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Order__46596229037B3931");

            entity.ToTable("Order");
            ConfigureTimestamps(entity);

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.DeductAmount)
                .HasColumnType("decimal(18 ,2)")
                .HasColumnName("deduct_amount");
            entity.Property(e => e.DiscountAmount)
                .HasColumnType("decimal(18 ,2)")
                .HasColumnName("discount_amount");
            entity.Property(e => e.IsReviewed)
                .HasDefaultValue(false)
                .HasColumnName("is_reviewed");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("order_date");
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("order_status");
            entity.Property(e => e.OriginAmount)
                .HasColumnType("decimal(18 ,2)")
                .HasColumnName("origin_amount");
            entity.Property(e => e.ShippingAddressId).HasColumnName("shipping_address_id");
            entity.Property(e => e.ShippingAmount)
                .HasColumnType("decimal(18 ,2)")
                .HasColumnName("shipping_amount");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18 ,2)")
                .HasColumnName("total_amount");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.ShippingAddress).WithMany(p => p.Orders)
                .HasForeignKey(d => d.ShippingAddressId)
                .HasConstraintName("FK__Order__shipping___5DCAEF64");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Order__user_id__5CD6CB2B");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PK__OrderIte__3764B6BCEECC8333");

            entity.ToTable("OrderItem");
            ConfigureTimestamps(entity);

            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18 ,2)")
                .HasColumnName("price");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.VarientProductId).HasColumnName("varient_product_id");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OrderItem__order__6C190EBB");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OrderItem__produ__619B8048");

            entity.HasOne(d => d.VarientProduct).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.VarientProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItem_VarientProduct");
        });

        modelBuilder.Entity<PendingSePayCheckout>(entity =>
        {
            entity.HasKey(e => e.PendingSePayCheckoutId);

            entity.ToTable("PendingSePayCheckout");
            ConfigureTimestamps(entity);

            entity.HasIndex(e => e.PaymentContent).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.OrderId);

            entity.Property(e => e.PendingSePayCheckoutId).HasColumnName("pending_sepay_checkout_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(20,2)")
                .HasColumnName("amount");
            entity.Property(e => e.PaymentContent)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnType("varchar(100)")
                .HasColumnName("payment_content");
            entity.Property(e => e.CheckoutPayload)
                .IsRequired()
                .HasColumnName("checkout_payload");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnType("varchar(50)")
                .HasColumnName("payment_status");
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnType("varchar(50)")
                .HasColumnName("order_status");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PaidAt)
                .HasColumnType("datetime")
                .HasColumnName("paid_at");
            entity.Property(e => e.GatewayPayload).HasColumnName("gateway_payload");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Payment__ED1FC9EAAA6E7EEE");

            entity.ToTable("Payment");
            ConfigureTimestamps(entity);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountNumber)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("account_number");
            entity.Property(e => e.Accumulated)
                .HasColumnType("decimal(20,2)")
                .HasColumnName("accumulated");
            entity.Property(e => e.AmountIn)
                .HasColumnType("decimal(20,2)")
                .HasColumnName("amount_in");
            entity.Property(e => e.AmountOut)
                .HasColumnType("decimal(20,2)")
                .HasColumnName("amount_out");
            entity.Property(e => e.Body)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("body");
            entity.Property(e => e.Code)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.Gateway)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("gateway");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("payment_status");
            entity.Property(e => e.ReferenceNumber)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("reference_number");
            entity.Property(e => e.SubAccount)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("sub_account");
            entity.Property(e => e.TransactionContent)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("transaction_content");
            entity.Property(e => e.TransactionDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("transaction_date");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__Payment__order_i__6E01572D");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Product__47027DF5954989E6");

            entity.ToTable("Product");

            entity.HasIndex(e => e.Sku, "UQ__Product__CA1ECF0D40802956").IsUnique();

            entity.Property(e => e.ProductId).HasColumnName("product_id")
                                              .UseIdentityColumn(100000,1);
            entity.Property(e=>e.ProductSysId).HasColumnName("product_sys_id");
            entity.Property(e => e.BrandId).HasColumnName("brandId");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Color)
                .HasMaxLength(155)
                .HasColumnName("color");
            entity.Property(e => e.CostPrice)
                .HasColumnType("decimal(18 ,2)")
                .HasColumnName("costPrice");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DiscountAmount)
                .HasColumnType("decimal(18 ,2)")
                .HasColumnName("discountAmount");
            entity.Property(e => e.DiscountPercentage).HasColumnName("discountPercentage");
            entity.Property(e => e.Image)
                .HasMaxLength(500)
                .HasColumnName("image");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.OriginalPrice)
                .HasColumnType("decimal(18 ,2)")
                .HasColumnName("originalPrice");
            entity.Property(e => e.SellPrice)
                .HasColumnType("decimal(18 ,2)")
                .HasColumnName("sellPrice");
            entity.Property(e => e.Sku)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("sku");
            entity.Property(e => e.Slug)
                .HasMaxLength(255)
                .HasColumnName("slug");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.IsShippingFee)
                .HasDefaultValue(true)
                .HasColumnName("is_shipping_fee");
            entity.Property(e => e.Stock).HasColumnName("stock");
            entity.Property(e => e.SortOrder).HasColumnName("sortOrder");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UrlYoutube)
                .HasMaxLength(255)
                .HasColumnName("urlYoutube");
            entity.Property(e => e.Visible).HasColumnName("visible");
            entity.Property(e => e.WarrantyPeriod)
                .HasMaxLength(50)
                .HasColumnName("warrantyPeriod");
            entity.Property(e => e.Weight).HasColumnName("weight");

            entity.HasOne(d => d.Brand).WithMany(p => p.Products)
                .HasForeignKey(d => d.BrandId)
                .HasConstraintName("FK_Product_Brand");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_Product_Category");


            //Đánh Index
            entity.HasIndex(p => p.Sku).IsUnique();
            entity.HasIndex(p=>p.Slug).IsUnique();

            //Index filtering
            entity.HasIndex(p => p.CategoryId);
            entity.HasIndex(p => p.BrandId);

            //Index SortOrder
            entity.HasIndex(p => p.SortOrder);
            entity.HasIndex(p => p.SellPrice);
        });

        modelBuilder.Entity<InventoryTransactions>(entity =>
        {
            entity.ToTable("InventoryTransactions");

            entity.Property(e => e.InventoryTransId).HasColumnName("inventory_transactions_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.ProductId).HasColumnName("productId");
            entity.Property(e => e.SupplierId).HasColumnName("supplierId");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");
            entity.Property(e => e.UserId).HasColumnName("userId");

            entity.HasOne(d => d.Product).WithMany(p => p.InventoryTransactions)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductHistory_Product");

            entity.HasOne(d => d.Supplier).WithMany(p => p.InventoryTransactions)
                .HasForeignKey(d => d.SupplierId)
                .HasConstraintName("FK_InventoryTransactions_Supplier");

            entity.HasOne(d => d.User).WithMany(p => p.InventoryTransactions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductHistory_User");
        });

        modelBuilder.Entity<InventoryTransactionsDetail>(entity =>
        {
            entity.HasKey(e => e.InventoryTransDetailId);

            entity.ToTable("InventoryTransactionsDetail");
            ConfigureTimestamps(entity);

            entity.Property(e => e.InventoryTransDetailId).HasColumnName("inventoryTrans_detail_id");
            entity.Property(e => e.InventoryTransId).HasColumnName("historyId");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.VarientId).HasColumnName("varientId");

            entity.HasOne(d => d.InventoryTransactions).WithMany(p => p.InventoryTransactionsDetail)
                .HasForeignKey(d => d.InventoryTransId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductHistoryDetail_ProductHistory");

            entity.HasOne(d => d.Varient).WithMany(p => p.ProductHistoryDetails)
                .HasForeignKey(d => d.VarientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductHistoryDetail_VarientProduct");
        });
        modelBuilder.Entity<Specs>(entity =>
        {
            entity.HasKey(e => e.SpecId);
            entity.ToTable("Specs");
            ConfigureTimestamps(entity);

            entity.Property(e => e.SpecId).HasColumnName("spec_id");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
            entity.Property(e => e.Code)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.GroupName)
                .HasMaxLength(150)
                .HasColumnName("group_name");
            entity.Property(e => e.Unit)
                .HasMaxLength(50)
                .HasColumnName("unit");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.InputType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("input_type");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.IsFilterable).HasColumnName("is_filterable");
            entity.Property(e => e.IsVisibleOnProductPage).HasColumnName("is_visible_on_product_page");

            entity.HasIndex(e => e.Code).IsUnique();
        });

        modelBuilder.Entity<SpecValue>(entity =>
        {
            entity.HasKey(e=>e.SpecValueId);
            entity.ToTable("SpecValue");
            ConfigureTimestamps(entity);

            entity.Property(e => e.SpecValueId).HasColumnName("specValue_id");
            entity.Property(e => e.SpecId).HasColumnName("spec_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Value)
                .HasMaxLength(500)
                .HasColumnName("value");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");

            entity.HasOne(d => d.Specs).WithMany(p => p.SpecValues)
               .HasForeignKey(d => d.SpecId)
               .OnDelete(DeleteBehavior.ClientSetNull)
               .HasConstraintName("FK_SpecValue_Specs");

            entity.HasOne(d => d.Product).WithMany(p => p.SpecValues)
                         .HasForeignKey(d => d.ProductId)
                         .OnDelete(DeleteBehavior.ClientSetNull)
                         .HasConstraintName("FK_SpecValue_Product");

            entity.HasIndex(e => new { e.ProductId, e.SpecId }).IsUnique();
        });
        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__Review__60883D9045574F64");

            entity.ToTable("Review");
            ConfigureTimestamps(entity);

            entity.Property(e => e.ReviewId).HasColumnName("review_id");
            entity.Property(e => e.Comment)
                .HasColumnName("comment");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.ReviewDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("review_date");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.VarientId).HasColumnName("varient_id");

            entity.HasOne(d => d.Product).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Review__product___6E01572D");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Review__user_id__6EF57B66");

            entity.HasOne(d => d.Varient).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.VarientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Review_VarientProduct");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__760965CC5FED20CB");

            entity.ToTable("Role");

            entity.HasIndex(e => e.RoleName, "UQ__Role__783254B1B29B582A").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<Setting>(entity =>
        {
            entity.ToTable("Setting");
            ConfigureTimestamps(entity);

            entity.Property(e => e.SettingId).HasColumnName("settingId");
            entity.Property(e => e.DataType)
                .HasMaxLength(50)
                .HasColumnName("dataType");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Group)
                .HasMaxLength(50)
                .HasColumnName("group");
            entity.Property(e => e.Key)
                .HasMaxLength(50)
                .HasColumnName("key");
            entity.Property(e => e.Value).HasColumnName("value");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("Supplier");
            ConfigureTimestamps(entity);

            entity.HasIndex(e => e.Code).IsUnique();

            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
            entity.Property(e => e.Address)
                .HasMaxLength(500)
                .HasColumnName("address");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.ContactName)
                .HasMaxLength(155)
                .HasColumnName("contact_name");
            entity.Property(e => e.Email)
                .HasMaxLength(155)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone_number");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__B9BE370F7C090B8B");

            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "UQ__User__AB6E6164402BDE8F").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.CreatedVerify)
                .HasColumnType("datetime")
                .HasColumnName("created_verify");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .HasColumnName("first_name");
            entity.Property(e => e.Img)
                .HasMaxLength(500)
                .HasColumnName("img");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsVerified).HasColumnName("is_verified");
            entity.Property(e => e.LastLogin)
                .HasColumnType("datetime")
                .HasColumnName("last_login");
            entity.Property(e => e.LastLoginDevice)
                .HasMaxLength(255)
                .HasColumnName("last_login_device");
            entity.Property(e => e.LastLoginIp)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("last_login_ip");
            entity.Property(e => e.LastRequestAt)
                .HasColumnType("datetime")
                .HasColumnName("last_request_at");
            entity.Property(e => e.LastRequestDevice)
                .HasMaxLength(255)
                .HasColumnName("last_request_device");
            entity.Property(e => e.LastRequestIp)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("last_request_ip");
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .HasColumnName("last_name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password_hash");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("phone_number");
            entity.Property(e => e.VerificationCode)
                .HasMaxLength(6)
                .IsFixedLength()
                .HasColumnName("verificationCode");

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UserRole__role_i__412EB0B6"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UserRole__user_i__403A8C7D"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId").HasName("PK__UserRole__6EDEA153475DB758");
                        j.ToTable("UserRole");
                        j.IndexerProperty<int>("UserId").HasColumnName("user_id");
                        j.IndexerProperty<int>("RoleId").HasColumnName("role_id");
                    });
        });

        modelBuilder.Entity<UserProductEvent>(entity =>
        {
            entity.ToTable("UserProductEvent");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.UserId, e.ProductId })
                .IsUnique()
                .HasFilter("[user_id] IS NOT NULL")
                .HasDatabaseName("IX_UserProductEvent_UserId_ProductId");
            entity.HasIndex(e => new { e.SessionId, e.ProductId })
                .IsUnique()
                .HasFilter("[session_id] IS NOT NULL")
                .HasDatabaseName("IX_UserProductEvent_SessionId_ProductId");
            entity.HasIndex(e => e.LastInteractedAt)
                .HasDatabaseName("IX_UserProductEvent_LastInteractedAt");
            entity.HasIndex(e => new { e.ProductId, e.LastInteractedAt })
                .HasDatabaseName("IX_UserProductEvent_ProductId_LastInteractedAt");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.SessionId)
                .HasMaxLength(100)
                .HasColumnName("session_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ViewCount)
                .HasDefaultValue(0)
                .HasColumnName("view_count");
            entity.Property(e => e.AddToCartCount)
                .HasDefaultValue(0)
                .HasColumnName("add_to_cart_count");
            entity.Property(e => e.WishlistCount)
                .HasDefaultValue(0)
                .HasColumnName("wishlist_count");
            entity.Property(e => e.PurchaseCount)
                .HasDefaultValue(0)
                .HasColumnName("purchase_count");
            entity.Property(e => e.InteractionScore)
                .HasColumnType("float")
                .HasDefaultValue(0d)
                .HasColumnName("interaction_score");
            entity.Property(e => e.LastInteractedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime2")
                .HasColumnName("last_interacted_at");

            entity.HasOne(d => d.Product).WithMany(p => p.UserProductEvents)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_UserProductEvent_Product");

            entity.HasOne(d => d.User).WithMany(p => p.UserProductEvents)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_UserProductEvent_User");
        });

        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.Property(e => e.UserNotificationId).HasColumnName("user_notificationID");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("isRead");
            entity.Property(e => e.NotificationId).HasColumnName("notificationID");
            entity.Property(e => e.ReadAt)
                .HasColumnType("datetime")
                .HasColumnName("readAt");
            entity.Property(e => e.UserId).HasColumnName("userID");

            entity.HasOne(d => d.Notification).WithMany(p => p.UserNotifications)
                .HasForeignKey(d => d.NotificationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserNotifications_Notifications");

            entity.HasOne(d => d.User).WithMany(p => p.UserNotifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserNotifications_User");
        });

        modelBuilder.Entity<VariantAttribute>(entity =>
        {
            entity.ToTable("VariantAttribute");
            ConfigureTimestamps(entity);

            entity.Property(e => e.VariantAttributeId).HasColumnName("variantAttributeId");
            entity.Property(e => e.AttributeValueId).HasColumnName("attribute_ValueId");
            entity.Property(e => e.ProductVariantId).HasColumnName("product_VariantId");

            entity.HasOne(d => d.AttributeValue).WithMany(p => p.VariantAttributes)
                .HasForeignKey(d => d.AttributeValueId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VariantAttribute_AttributeValue");

            entity.HasOne(d => d.ProductVariant).WithMany(p => p.VariantAttributes)
                .HasForeignKey(d => d.ProductVariantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VariantAttribute_VarientProduct");
        });

        modelBuilder.Entity<VarientProduct>(entity =>
        {
            entity.HasKey(e => e.VarientId);

            entity.ToTable("VarientProduct");

            entity.Property(e => e.VarientId).HasColumnName("varientId");
            entity.Property(e => e.Attributes)
                .HasMaxLength(255)
                .HasColumnName("attributes");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18 ,2)")
                .HasColumnName("price");
            entity.Property(e => e.ProductId).HasColumnName("productId");
            entity.Property(e => e.ImageUrl).HasColumnName("imageUrl");
            entity.Property(e => e.Sku)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("sku");
            entity.Property(e => e.Stock).HasColumnName("stock");

            entity.HasOne(d => d.Product).WithMany(p => p.VarientProducts)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_VarientProduct_Product");

            entity.HasIndex(e => e.Sku, "UQ__VarientP__CA1ECF0D1E3D1C2E").IsUnique();
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.ToTable("Voucher");
            ConfigureTimestamps(entity);

            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(155);
            entity.Property(e => e.Promotion)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasKey(e => e.WishlistId).HasName("PK__Wishlist__6151514E10A01334");

            entity.ToTable("Wishlist");

            entity.Property(e => e.WishlistId).HasColumnName("wishlist_id");
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("added_date");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Product).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__Wishlist__produc__6A30C649");

            entity.HasOne(d => d.User).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Wishlist__user_i__693CA210");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
