using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Stock",
                table: "equipment_items",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Stock",
                table: "equipment_items");
        }
    }
}
