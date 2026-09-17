using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kinof.Api.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260917043000_AddBehaviorPenalties")]
    public partial class AddBehaviorPenalties : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "behavior_penalties",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    points = table.Column<int>(type: "INTEGER", nullable: false),
                    reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    source = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    source_key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_behavior_penalties", x => x.id);
                    table.ForeignKey(
                        name: "FK_behavior_penalties_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_behavior_penalties_source_key",
                table: "behavior_penalties",
                column: "source_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_behavior_penalties_user_id_created_at",
                table: "behavior_penalties",
                columns: new[] { "user_id", "created_at" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "behavior_penalties");
        }
    }
}
