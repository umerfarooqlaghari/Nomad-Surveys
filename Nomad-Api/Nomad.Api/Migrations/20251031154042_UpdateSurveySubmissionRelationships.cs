using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nomad.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSurveySubmissionRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SurveySubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectEvaluatorSurveyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseData = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveySubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveySubmissions_Evaluators_EvaluatorId",
                        column: x => x.EvaluatorId,
                        principalTable: "Evaluators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveySubmissions_SubjectEvaluatorSurveys_SubjectEvaluatorS~",
                        column: x => x.SubjectEvaluatorSurveyId,
                        principalTable: "SubjectEvaluatorSurveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SurveySubmissions_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveySubmissions_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveySubmissions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SurveySubmissions_CompletedAt",
                table: "SurveySubmissions",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SurveySubmissions_EvaluatorId",
                table: "SurveySubmissions",
                column: "EvaluatorId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveySubmissions_SubjectEvaluatorSurveyId_EvaluatorId",
                table: "SurveySubmissions",
                columns: new[] { "SubjectEvaluatorSurveyId", "EvaluatorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SurveySubmissions_SubjectId",
                table: "SurveySubmissions",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveySubmissions_SurveyId",
                table: "SurveySubmissions",
                column: "SurveyId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveySubmissions_TenantId_Status",
                table: "SurveySubmissions",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SurveySubmissions");
        }
    }
}
