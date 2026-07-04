using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckTransportApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAdditionalDriverToTrip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
                migrationBuilder.AddColumn<string>(
                    name: "AdditionalDriverName",
                    table: "Trips",
                    type: "nvarchar(max)",
                    nullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
                migrationBuilder.DropColumn(
                    name: "AdditionalDriverName",
                    table: "Trips");

        }
    }
}
