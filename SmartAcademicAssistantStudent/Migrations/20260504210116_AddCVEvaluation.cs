using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartAcademicAssistantStudent.Migrations
{
    /// <inheritdoc />
    public partial class AddCVEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CVEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExtractedText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GPA = table.Column<double>(type: "float", nullable: false),
                    ExperienceYears = table.Column<int>(type: "int", nullable: false),
                    SkillsCount = table.Column<int>(type: "int", nullable: false),
                    HasGitHub = table.Column<bool>(type: "bit", nullable: false),
                    CertificationsCount = table.Column<int>(type: "int", nullable: false),
                    ProjectsCount = table.Column<int>(type: "int", nullable: false),
                    HasInternship = table.Column<bool>(type: "bit", nullable: false),
                    EnglishLevel = table.Column<int>(type: "int", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AcceptanceProbability = table.Column<float>(type: "real", nullable: false),
                    Strengths = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Weaknesses = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CVEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CVEvaluations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CVEvaluations_UserId",
                table: "CVEvaluations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CVEvaluations");
        }
    }
}
