using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kinof.Api.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260911100000_AddProblemReportAdminComment")]
    public partial class AddProblemReportAdminComment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "admin_comment",
                table: "problem_reports",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "admin_comment",
                table: "problem_reports");
        }
    }
}
