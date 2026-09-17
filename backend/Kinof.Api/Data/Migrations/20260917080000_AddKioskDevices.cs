using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kinof.Api.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260917080000_AddKioskDevices")]
    public partial class AddKioskDevices : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "kiosk_devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    room_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    api_key = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    label = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    revoked_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    last_seen_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kiosk_devices", x => x.id);
                    table.ForeignKey(
                        name: "FK_kiosk_devices_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_kiosk_devices_api_key",
                table: "kiosk_devices",
                column: "api_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_kiosk_devices_room_id",
                table: "kiosk_devices",
                column: "room_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "kiosk_devices");
        }
    }
}
