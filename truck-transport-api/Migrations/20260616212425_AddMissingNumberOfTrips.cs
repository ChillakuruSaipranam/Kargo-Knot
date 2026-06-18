using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckTransportApi.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingNumberOfTrips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Trips', 'NumberOfTrips') IS NULL
BEGIN
    ALTER TABLE [Trips] ADD [NumberOfTrips] int NOT NULL CONSTRAINT DF_Trips_NumberOfTrips DEFAULT(1);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Trips', 'NumberOfTrips') IS NOT NULL
BEGIN
    ALTER TABLE [Trips] DROP CONSTRAINT IF EXISTS DF_Trips_NumberOfTrips;
    ALTER TABLE [Trips] DROP COLUMN [NumberOfTrips];
END
");
        }
    }
}
