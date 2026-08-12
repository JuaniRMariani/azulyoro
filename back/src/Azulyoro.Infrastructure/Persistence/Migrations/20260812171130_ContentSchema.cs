using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Azulyoro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ContentSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sources",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    rss_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    rate_limit_seconds = table.Column<int>(type: "integer", nullable: false),
                    keyword_filter = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    robots_ok = table.Column<bool>(type: "boolean", nullable: false),
                    last_fetched_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    etag = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_modified = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staging_articles",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    source_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    url_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    excerpt = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    published_at_source = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    scraped_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    category = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_articles", x => x.id);
                    table.ForeignKey(
                        name: "fk_staging_articles_sources_source_id",
                        column: x => x.source_id,
                        principalSchema: "app",
                        principalTable: "sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "articles",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    cover_image_url = table.Column<string>(type: "text", nullable: true),
                    source_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    source_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    author_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_members_only = table.Column<bool>(type: "boolean", nullable: false),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    staging_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_articles", x => x.id);
                    table.ForeignKey(
                        name: "fk_articles_staging_articles_staging_id",
                        column: x => x.staging_id,
                        principalSchema: "app",
                        principalTable: "staging_articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "article_tags",
                schema: "app",
                columns: table => new
                {
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_article_tags", x => new { x.article_id, x.tag_id });
                    table.ForeignKey(
                        name: "fk_article_tags_articles_article_id",
                        column: x => x.article_id,
                        principalSchema: "app",
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_article_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalSchema: "app",
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "article_translations",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    summary = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    body_html = table.Column<string>(type: "text", nullable: true),
                    meta_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    meta_description = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_article_translations", x => x.id);
                    table.ForeignKey(
                        name: "fk_article_translations_articles_article_id",
                        column: x => x.article_id,
                        principalSchema: "app",
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_article_tags_tag_id",
                schema: "app",
                table: "article_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "ix_article_translations_article_id_locale",
                schema: "app",
                table: "article_translations",
                columns: new[] { "article_id", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_articles_slug",
                schema: "app",
                table: "articles",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_articles_staging_id",
                schema: "app",
                table: "articles",
                column: "staging_id");

            migrationBuilder.CreateIndex(
                name: "ix_articles_status_published_at",
                schema: "app",
                table: "articles",
                columns: new[] { "status", "published_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sources_active",
                schema: "app",
                table: "sources",
                column: "active");

            migrationBuilder.CreateIndex(
                name: "ix_staging_articles_source_id",
                schema: "app",
                table: "staging_articles",
                column: "source_id");

            migrationBuilder.CreateIndex(
                name: "ix_staging_articles_status",
                schema: "app",
                table: "staging_articles",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_staging_articles_title_hash",
                schema: "app",
                table: "staging_articles",
                column: "title_hash");

            migrationBuilder.CreateIndex(
                name: "ix_staging_articles_url_hash",
                schema: "app",
                table: "staging_articles",
                column: "url_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tags_slug",
                schema: "app",
                table: "tags",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "article_tags",
                schema: "app");

            migrationBuilder.DropTable(
                name: "article_translations",
                schema: "app");

            migrationBuilder.DropTable(
                name: "tags",
                schema: "app");

            migrationBuilder.DropTable(
                name: "articles",
                schema: "app");

            migrationBuilder.DropTable(
                name: "staging_articles",
                schema: "app");

            migrationBuilder.DropTable(
                name: "sources",
                schema: "app");
        }
    }
}
