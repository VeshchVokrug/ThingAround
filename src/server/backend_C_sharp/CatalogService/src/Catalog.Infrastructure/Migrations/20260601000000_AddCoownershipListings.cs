using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddCoownershipListings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "coownership_listings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title_slug = table.Column<string>(type: "text", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    catalog_listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_slug = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    images_urls = table.Column<List<string>>(type: "text[]", nullable: true),
                    city = table.Column<string>(type: "text", nullable: false),
                    share_price = table.Column<int>(type: "integer", nullable: false),
                    total_shares = table.Column<int>(type: "integer", nullable: false),
                    available_shares = table.Column<int>(type: "integer", nullable: false),
                    funding_deadline = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_coownership_listings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_coownership_listings_city_filters",
                table: "coownership_listings",
                columns: new[] { "city", "category_slug", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_coownership_listings_owner_filters",
                table: "coownership_listings",
                columns: new[] { "owner_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_coownership_listings_title_slug",
                table: "coownership_listings",
                column: "title_slug",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "coownership_listings");
        }
    }
}