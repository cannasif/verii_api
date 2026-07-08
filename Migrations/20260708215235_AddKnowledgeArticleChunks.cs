using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace V3RII.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeArticleChunks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KnowledgeArticleChunks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KnowledgeArticleId = table.Column<long>(type: "bigint", nullable: false),
                    Product = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2400)", maxLength: 2400, nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: false),
                    ChunkIndex = table.Column<int>(type: "int", nullable: false),
                    TokenEstimate = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeArticleChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeArticleChunks_KnowledgeArticles_KnowledgeArticleId",
                        column: x => x.KnowledgeArticleId,
                        principalTable: "KnowledgeArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeArticleChunks_KnowledgeArticleId_ChunkIndex",
                table: "KnowledgeArticleChunks",
                columns: new[] { "KnowledgeArticleId", "ChunkIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeArticleChunks_Product_IsPublished",
                table: "KnowledgeArticleChunks",
                columns: new[] { "Product", "IsPublished" });

            migrationBuilder.Sql("""
                INSERT INTO KnowledgeArticleChunks
                    (KnowledgeArticleId, Product, Title, Content, Tags, ChunkIndex, TokenEstimate, IsPublished, CreatedAt, IsDeleted)
                SELECT
                    Id,
                    Product,
                    Title,
                    LEFT(CONCAT(Summary, CHAR(13), CHAR(10), CHAR(13), CHAR(10), ContentMarkdown), 2400),
                    Tags,
                    0,
                    CASE
                        WHEN LEN(CONCAT(Summary, ContentMarkdown)) < 4 THEN 1
                        ELSE CEILING(CAST(LEN(CONCAT(Summary, ContentMarkdown)) AS decimal(18, 2)) / 4)
                    END,
                    IsPublished,
                    SYSDATETIMEOFFSET(),
                    0
                FROM KnowledgeArticles
                WHERE LEN(CONCAT(Summary, ContentMarkdown)) > 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KnowledgeArticleChunks");
        }
    }
}
