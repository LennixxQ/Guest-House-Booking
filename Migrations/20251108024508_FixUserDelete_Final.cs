using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuestHouseBookingCore.Migrations
{
    /// <inheritdoc />
    public partial class FixUserDelete_Final : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PURANA CONSTRAINT HATAO
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Users_UserId",
                table: "Bookings");

            // NAYA CONSTRAINT LAGAO → DELETE SAFE
            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Users_UserId",
                table: "Bookings",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
