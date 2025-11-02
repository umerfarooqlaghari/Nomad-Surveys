using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nomad.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSubjectEvaluatorSurveys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubjectEvaluatorSurveys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectEvaluatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectEvaluatorSurveys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubjectEvaluatorSurveys_SubjectEvaluators_SubjectEvaluatorId",
                        column: x => x.SubjectEvaluatorId,
                        principalTable: "SubjectEvaluators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubjectEvaluatorSurveys_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubjectEvaluatorSurveys_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubjectEvaluatorSurveys_SubjectEvaluatorId_SurveyId",
                table: "SubjectEvaluatorSurveys",
                columns: new[] { "SubjectEvaluatorId", "SurveyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubjectEvaluatorSurveys_SurveyId",
                table: "SubjectEvaluatorSurveys",
                column: "SurveyId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectEvaluatorSurveys_TenantId",
                table: "SubjectEvaluatorSurveys",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubjectEvaluatorSurveys");
        }
    }
}
