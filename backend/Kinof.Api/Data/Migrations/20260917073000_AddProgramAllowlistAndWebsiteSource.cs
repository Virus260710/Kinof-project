using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kinof.Api.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260917073000_AddProgramAllowlistAndWebsiteSource")]
    public partial class AddProgramAllowlistAndWebsiteSource : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "website_blacklist",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.CreateIndex(
                name: "IX_website_blacklist_source_category",
                table: "website_blacklist",
                columns: new[] { "source", "category" });

            migrationBuilder.CreateTable(
                name: "program_allowlist",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    process_name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    display_name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_allowlist", x => x.id);
                    table.ForeignKey(
                        name: "FK_program_allowlist_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_program_allowlist_created_by",
                table: "program_allowlist",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_program_allowlist_process_name",
                table: "program_allowlist",
                column: "process_name",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "program_allowlist");
            migrationBuilder.DropIndex(
                name: "IX_website_blacklist_source_category",
                table: "website_blacklist");
            migrationBuilder.DropColumn(name: "source", table: "website_blacklist");
        }
    }
}
