using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetGroomingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddPetTypeAndName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PetName",
                table: "Appointments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PetType",
                table: "Appointments",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PetName",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "PetType",
                table: "Appointments");
        }
    }
}
