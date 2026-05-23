using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityProfileService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddedPhoneNumberinProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "phone_number",
                table: "profiles",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "phone_number",
                table: "profiles");
        }
    }
}
