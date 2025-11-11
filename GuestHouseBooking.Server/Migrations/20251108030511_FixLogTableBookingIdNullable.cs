using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuestHouseBookingCore.Migrations
{
    /// <inheritdoc />
    public partial class FixLogTableBookingIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
        name: "BookingId",
        table: "LogTable",
        type: "int",
        nullable: true,
        oldClrType: typeof(int),
        oldType: "int");

            // Agar purana constraint hai to drop karo
            migrationBuilder.DropForeignKey(
                name: "FK_LogTable_Bookings_BookingId",
                table: "LogTable");

            migrationBuilder.AddForeignKey(
                name: "FK_LogTable_Bookings_BookingId",
                table: "LogTable",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "BookingId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "BookingId",
                table: "LogTable",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
