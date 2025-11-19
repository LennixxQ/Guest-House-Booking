using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuestHouseBookingCore.Migrations
{
    /// <inheritdoc />
    public partial class AddLogCreatedModifiedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "LogTable",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "LogTable",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "LogTable");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "LogTable");
        }
    }
}
