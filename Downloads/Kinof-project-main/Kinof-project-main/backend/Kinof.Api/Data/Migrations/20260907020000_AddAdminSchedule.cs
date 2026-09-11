using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kinof.Api.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260907020000_AddAdminSchedule")]
    public partial class AddAdminSchedule : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "job_title",
                table: "users",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "academic_year",
                table: "schedules",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "instructor_name",
                table: "schedules",
                type: "TEXT",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "schedules",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "section",
                table: "schedules",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_schedules_room_id_day_of_week_is_active",
                table: "schedules",
                columns: new[] { "room_id", "day_of_week", "is_active" });

            migrationBuilder.CreateTable(
                name: "admin_audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    action = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    target_type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    target_id = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    detail = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_admin_audit_logs_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "schedule_enrollment_pending",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    schedule_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    student_id = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_enrollment_pending", x => x.id);
                    table.ForeignKey(
                        name: "FK_schedule_enrollment_pending_schedules_schedule_id",
                        column: x => x.schedule_id,
                        principalTable: "schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_logs_action",
                table: "admin_audit_logs",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_logs_created_at",
                table: "admin_audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_logs_actor_user_id",
                table: "admin_audit_logs",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_enrollment_pending_student_id",
                table: "schedule_enrollment_pending",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_enrollment_pending_schedule_id_student_id",
                table: "schedule_enrollment_pending",
                columns: new[] { "schedule_id", "student_id" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "admin_audit_logs");
            migrationBuilder.DropTable(name: "schedule_enrollment_pending");
            migrationBuilder.DropIndex(
                name: "IX_schedules_room_id_day_of_week_is_active",
                table: "schedules");
            migrationBuilder.DropColumn(name: "job_title", table: "users");
            migrationBuilder.DropColumn(name: "academic_year", table: "schedules");
            migrationBuilder.DropColumn(name: "instructor_name", table: "schedules");
            migrationBuilder.DropColumn(name: "is_active", table: "schedules");
            migrationBuilder.DropColumn(name: "section", table: "schedules");
        }
    }
}
