using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckTransportApi.Migrations
{
    /// <inheritdoc />
    public partial class AddRepairDriverName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                table: "Repairs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DriverName",
                table: "Repairs");
        }
    }
}
