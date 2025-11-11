using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuestHouseBookingCore.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                    name: "IsDeleted",
                    table: "Users",
                    nullable: false,
                    defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Users",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn("IsDeleted", "Users");
            migrationBuilder.DropColumn("DeletedAt", "Users");
        }
    }
}
