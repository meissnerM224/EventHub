using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixParticipantRoleName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("8f2a1c40-1f3d-4c9a-9b7e-2a0d5c1e7a02"),
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "Participant", "PARTICIPANT" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("8f2a1c40-1f3d-4c9a-9b7e-2a0d5c1e7a02"),
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "Participants", "PARTICIPANTS" });
        }
    }
}
