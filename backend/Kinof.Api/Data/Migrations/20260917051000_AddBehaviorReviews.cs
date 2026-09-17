using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kinof.Api.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260917051000_AddBehaviorReviews")]
    public partial class AddBehaviorReviews : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "behavior_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    display_name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    room_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    seat_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    room_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    seat_label = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    kind = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    target = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    activity = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    queue_key = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    occurrence_count = table.Column<int>(type: "INTEGER", nullable: false),
                    first_seen_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_seen_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    reviewed_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_behavior_reviews", x => x.id);
                    table.ForeignKey(
                        name: "FK_behavior_reviews_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_behavior_reviews_users_reviewed_by",
                        column: x => x.reviewed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_behavior_reviews_status",
                table: "behavior_reviews",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_behavior_reviews_queue_key",
                table: "behavior_reviews",
                column: "queue_key",
                unique: true,
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_behavior_reviews_user_id",
                table: "behavior_reviews",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_behavior_reviews_reviewed_by",
                table: "behavior_reviews",
                column: "reviewed_by");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "behavior_reviews");
        }
    }
}
