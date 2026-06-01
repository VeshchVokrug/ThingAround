using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "availability_slots",
                columns: table => new
                {
                    listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    reserved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    price = table.Column<int>(type: "integer", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_availability_slots", x => new { x.listing_id, x.date });
                });

            migrationBuilder.CreateTable(
                name: "rental_listings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title_slug = table.Column<string>(type: "text", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_slug = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    images_urls = table.Column<List<string>>(type: "text[]", nullable: true),
                    owner_rating = table.Column<float>(type: "real", nullable: false),
                    city = table.Column<string>(type: "text", nullable: false),
                    default_price = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    contact_manager_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_person_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    contact_person_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    contact_socials_urls = table.Column<List<string>>(type: "text[]", nullable: true),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true, computedColumnSql: "to_tsvector('russian', coalesce(\"title\", '') || ' ' || coalesce(\"description\", ''))", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rental_listings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_availability_slots_date_is_available",
                table: "availability_slots",
                columns: new[] { "date", "is_available" })
                .Annotation("Npgsql:IndexInclude", new[] { "price" });

            migrationBuilder.CreateIndex(
                name: "ix_availability_slots_listing_id_date_is_available",
                table: "availability_slots",
                columns: new[] { "listing_id", "date", "is_available" });

            migrationBuilder.CreateIndex(
                name: "ix_rental_listings_city_filters",
                table: "rental_listings",
                columns: new[] { "city", "category_slug", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_rental_listings_filters",
                table: "rental_listings",
                columns: new[] { "category_slug", "is_active", "default_price" });

            migrationBuilder.CreateIndex(
                name: "ix_rental_listings_search_vector",
                table: "rental_listings",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "ix_rental_listings_title_slug",
                table: "rental_listings",
                column: "title_slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "availability_slots");

            migrationBuilder.DropTable(
                name: "rental_listings");
        }
    }
}
