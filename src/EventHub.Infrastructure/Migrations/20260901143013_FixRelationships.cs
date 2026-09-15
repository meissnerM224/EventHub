using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Events_EventId1",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Events_Categories_CategoryId1",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_CategoryId1",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_EventId1",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CategoryId1",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "EventId1",
                table: "Bookings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId1",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "EventId1",
                table: "Bookings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Events_CategoryId1",
                table: "Events",
                column: "CategoryId1");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_EventId1",
                table: "Bookings",
                column: "EventId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Events_EventId1",
                table: "Bookings",
                column: "EventId1",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Categories_CategoryId1",
                table: "Events",
                column: "CategoryId1",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
