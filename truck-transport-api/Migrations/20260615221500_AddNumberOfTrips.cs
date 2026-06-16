using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckTransportApi.Migrations
{
    public partial class AddNumberOfTrips : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NumberOfTrips",
                table: "Trips",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumberOfTrips",
                table: "Trips");
        }
    }
}
