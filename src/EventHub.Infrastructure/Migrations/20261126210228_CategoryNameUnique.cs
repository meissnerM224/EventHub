using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CategoryNameUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);
            // Seed data was inserted with explicit Ids 1–3, which does not move the
            // identity sequence. Without this, the first new category gets Id 1 → duplicate key.
            migrationBuilder.Sql("""
                                 SELECT setval(
                                 pg_get_serial_sequence('"Categories"', 'Id'),
                                 (SELECT MAX("Id") FROM "Categories")); 
                                 """);
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_Name",
                table: "Categories");
        }
    }
}
