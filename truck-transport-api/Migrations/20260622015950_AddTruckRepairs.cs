using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckTransportApi.Migrations
{
    /// <inheritdoc />
    public partial class AddTruckRepairs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Repairs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TruckNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Repairs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Repairs_Date",
                table: "Repairs",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Repairs_TruckNumber",
                table: "Repairs",
                column: "TruckNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Repairs");
        }
    }
}
