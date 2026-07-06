using Microsoft.EntityFrameworkCore;
using V3RII.Application.Common.Security;
using V3RII.Domain.Common;
using V3RII.Domain.Entities;
using V3RII.Domain.Enums;

namespace V3RII.Infrastructure.Persistence;

public sealed class V3RiiDbContext(DbContextOptions<V3RiiDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<PermissionDefinition> PermissionDefinitions => Set<PermissionDefinition>();
    public DbSet<PermissionGroup> PermissionGroups => Set<PermissionGroup>();
    public DbSet<PermissionGroupPermissionDefinition> PermissionGroupPermissionDefinitions => Set<PermissionGroupPermissionDefinition>();
    public DbSet<UserPermissionGroup> UserPermissionGroups => Set<UserPermissionGroup>();
    public DbSet<KnowledgeArticle> KnowledgeArticles => Set<KnowledgeArticle>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<ChatAnalyticsEvent> ChatAnalyticsEvents => Set<ChatAnalyticsEvent>();
    public DbSet<MailOutboxItem> MailOutboxItems => Set<MailOutboxItem>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(x => x.NormalizedEmail).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.NormalizedEmail).HasMaxLength(256).IsRequired();
            entity.Property(x => x.FullName).HasMaxLength(180).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
        });

        modelBuilder.Entity<PermissionDefinition>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<PermissionGroup>(entity =>
        {
            entity.HasIndex(x => x.NormalizedName).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.NormalizedName).HasMaxLength(120).IsRequired();
        });

        modelBuilder.Entity<PermissionGroupPermissionDefinition>(entity =>
        {
            entity.HasKey(x => new { x.PermissionGroupId, x.PermissionDefinitionId });
            entity.HasOne(x => x.PermissionGroup).WithMany(x => x.Permissions).HasForeignKey(x => x.PermissionGroupId);
            entity.HasOne(x => x.PermissionDefinition).WithMany().HasForeignKey(x => x.PermissionDefinitionId);
        });

        modelBuilder.Entity<UserPermissionGroup>(entity =>
        {
            entity.HasKey(x => new { x.UserId, x.PermissionGroupId });
            entity.HasOne(x => x.User).WithMany(x => x.PermissionGroups).HasForeignKey(x => x.UserId);
            entity.HasOne(x => x.PermissionGroup).WithMany(x => x.Users).HasForeignKey(x => x.PermissionGroupId);
        });

        modelBuilder.Entity<KnowledgeArticle>(entity =>
        {
            entity.HasIndex(x => new { x.Product, x.IsPublished });
            entity.Property(x => x.Title).HasMaxLength(220).IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(700).IsRequired();
            entity.Property(x => x.ContentMarkdown).HasMaxLength(8000).IsRequired();
            entity.Property(x => x.Tags).HasMaxLength(700).IsRequired();
        });

        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.HasIndex(x => x.TicketNo).IsUnique();
            entity.HasIndex(x => new { x.Status, x.Product, x.CreatedAt });
            entity.Property(x => x.TicketNo).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Intent).HasMaxLength(120).IsRequired();
            entity.Property(x => x.CustomerName).HasMaxLength(180).IsRequired();
            entity.Property(x => x.CustomerEmail).HasMaxLength(256).IsRequired();
            entity.Property(x => x.CompanyName).HasMaxLength(180);
            entity.Property(x => x.Details).HasMaxLength(5000).IsRequired();
            entity.Property(x => x.TranscriptJson).HasMaxLength(12000);
            entity.Property(x => x.AssignedToEmail).HasMaxLength(256);
            entity.Property(x => x.HandoffReason).HasMaxLength(500);
            entity.Property(x => x.Source).HasMaxLength(80).IsRequired();
        });

        modelBuilder.Entity<ChatAnalyticsEvent>(entity =>
        {
            entity.HasIndex(x => new { x.EventType, x.CreatedAt });
            entity.HasIndex(x => new { x.Product, x.CreatedAt });
            entity.Property(x => x.EventType).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Intent).HasMaxLength(120);
            entity.Property(x => x.SessionId).HasMaxLength(120);
            entity.Property(x => x.MetadataJson).HasMaxLength(4000);
        });

        modelBuilder.Entity<MailOutboxItem>(entity =>
        {
            entity.HasIndex(x => new { x.SentAt, x.NextAttemptAt });
            entity.Property(x => x.To).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(220).IsRequired();
            entity.Property(x => x.BodyHtml).HasMaxLength(8000).IsRequired();
            entity.Property(x => x.LastError).HasMaxLength(1200);
        });

        Seed(modelBuilder);
    }

    private static void Seed(ModelBuilder modelBuilder)
    {
        var permissionId = 1L;
        var permissions = PermissionCodes.All
            .Select(code => new PermissionDefinition
            {
                Id = permissionId++,
                Code = code,
                Name = code,
                Description = "Seeded system permission",
                CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            })
            .ToArray();

        modelBuilder.Entity<PermissionDefinition>().HasData(permissions);
        modelBuilder.Entity<PermissionGroup>().HasData(new PermissionGroup
        {
            Id = 1,
            Name = "System Admin",
            NormalizedName = "SYSTEM ADMIN",
            IsSystem = true,
            CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
        });

        modelBuilder.Entity<PermissionGroupPermissionDefinition>().HasData(
            permissions.Select(x => new PermissionGroupPermissionDefinition
            {
                PermissionGroupId = 1,
                PermissionDefinitionId = x.Id
            }));

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            Email = "admin@v3rii.com",
            NormalizedEmail = "ADMIN@V3RII.COM",
            FullName = "V3RII Admin",
            PasswordHash = "$2a$11$5SVbkfIquQrVSkCTMo9iq.wbO.O2MejZMlChqiuxWwoLFC.rjPEeq",
            IsActive = true,
            CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
        });

        modelBuilder.Entity<UserPermissionGroup>().HasData(new UserPermissionGroup
        {
            UserId = 1,
            PermissionGroupId = 1
        });

        modelBuilder.Entity<KnowledgeArticle>().HasData(
            new KnowledgeArticle
            {
                Id = 1,
                Product = ProductKey.Crm,
                Title = "V3RII CRM",
                Summary = "Satış, teklif, müşteri 360, onay akışları, mail/WhatsApp ve ERP entegrasyonları için kurumsal CRM.",
                ContentMarkdown = "Müşteri kartları, mükerrer kayıt kontrolü, teklif-sipariş dönüşümü, PDF rapor tasarımı, Power BI, Outlook, WhatsApp ve Netsis/ERP entegrasyonları desteklenir.",
                Tags = "crm,satis,teklif,netsis,erp,whatsapp",
                IsPublished = true,
                CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new KnowledgeArticle
            {
                Id = 2,
                Product = ProductKey.Wms,
                Title = "V3RII WMS",
                Summary = "Depo kabul, lokasyon, barkod, sevkiyat, kalite kontrol, paketleme ve ERP servis operasyonlarını yönetir.",
                ContentMarkdown = "Mal kabul, depo giriş/çıkış, transfer, karantina, kalite kontrol, paketleme, yükleme, barkod, e-belge ve ERP entegrasyon süreçleri parametrik olarak yönetilebilir.",
                Tags = "wms,depo,barkod,sevkiyat,netsis,erp",
                IsPublished = true,
                CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new KnowledgeArticle
            {
                Id = 3,
                Product = ProductKey.B2B,
                Title = "V3RII B2B",
                Summary = "Bayi ve müşteri portalları için katalog, fiyat, stok görünürlüğü, teklif, sipariş, ödeme ve pazar yeri süreçlerini yönetir.",
                ContentMarkdown = "Şirket hesapları, alıcı yönetimi, katalog görünürlük kuralları, müşteri bazlı fiyat/stok kapsamları, satın alma onay kuralları, teklif-sipariş-ödeme akışları ve ERP/pazar yeri entegrasyonları desteklenir.",
                Tags = "b2b,bayi,katalog,fiyat,stok,pazar-yeri,erp",
                IsPublished = true,
                CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new KnowledgeArticle
            {
                Id = 4,
                Product = ProductKey.Uts,
                Title = "V3RII UTS",
                Summary = "ÜTS/UTS üretim, verme, alma, ters verme, ithalat, ihracat, imha ve operasyonel takip süreçlerini merkezi panelden yönetir.",
                ContentMarkdown = "UTS üretim listeleri, tüketiciye verme, alma, ters verme, ithalat/ihracat/imha operasyonları, cari ve stok referansları, rol/yetki altyapısı ve Hangfire izleme desteklenir.",
                Tags = "uts,uts-uretim,verme,alma,imha,ithalat,ihracat,yetki",
                IsPublished = true,
                CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            });
    }
}
