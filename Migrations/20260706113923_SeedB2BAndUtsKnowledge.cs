using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace V3RII.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedB2BAndUtsKnowledge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "KnowledgeArticles",
                columns: new[] { "Id", "ContentMarkdown", "CreatedAt", "IsDeleted", "IsPublished", "Product", "Summary", "Tags", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { 3L, "Şirket hesapları, alıcı yönetimi, katalog görünürlük kuralları, müşteri bazlı fiyat/stok kapsamları, satın alma onay kuralları, teklif-sipariş-ödeme akışları ve ERP/pazar yeri entegrasyonları desteklenir.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), false, true, 2, "Bayi ve müşteri portalları için katalog, fiyat, stok görünürlüğü, teklif, sipariş, ödeme ve pazar yeri süreçlerini yönetir.", "b2b,bayi,katalog,fiyat,stok,pazar-yeri,erp", "V3RII B2B", null },
                    { 4L, "UTS üretim listeleri, tüketiciye verme, alma, ters verme, ithalat/ihracat/imha operasyonları, cari ve stok referansları, rol/yetki altyapısı ve Hangfire izleme desteklenir.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), false, true, 4, "ÜTS/UTS üretim, verme, alma, ters verme, ithalat, ihracat, imha ve operasyonel takip süreçlerini merkezi panelden yönetir.", "uts,uts-uretim,verme,alma,imha,ithalat,ihracat,yetki", "V3RII UTS", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "KnowledgeArticles",
                keyColumn: "Id",
                keyValue: 3L);

            migrationBuilder.DeleteData(
                table: "KnowledgeArticles",
                keyColumn: "Id",
                keyValue: 4L);
        }
    }
}
