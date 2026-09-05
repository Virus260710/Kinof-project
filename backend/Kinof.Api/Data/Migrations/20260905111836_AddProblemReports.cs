using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kinof.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProblemReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "problem_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_problem_reports", x => x.id);
                    table.ForeignKey(
                        name: "FK_problem_reports_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "problem_report_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    problem_report_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    stored_file_name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    original_file_name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_problem_report_images", x => x.id);
                    table.ForeignKey(
                        name: "FK_problem_report_images_problem_reports_problem_report_id",
                        column: x => x.problem_report_id,
                        principalTable: "problem_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_problem_report_images_problem_report_id",
                table: "problem_report_images",
                column: "problem_report_id");

            migrationBuilder.CreateIndex(
                name: "IX_problem_reports_user_id_created_at",
                table: "problem_reports",
                columns: new[] { "user_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "problem_report_images");

            migrationBuilder.DropTable(
                name: "problem_reports");
        }
    }
}
