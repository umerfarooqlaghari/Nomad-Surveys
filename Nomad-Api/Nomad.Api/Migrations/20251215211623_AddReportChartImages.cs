using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nomad.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReportChartImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportChartImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ClusterName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CompetencyName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportChartImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportChartImages_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportChartImages_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportChartImages_ImageType",
                table: "ReportChartImages",
                column: "ImageType");

            migrationBuilder.CreateIndex(
                name: "IX_ReportChartImages_SurveyId_ImageType_ClusterName_Competency~",
                table: "ReportChartImages",
                columns: new[] { "SurveyId", "ImageType", "ClusterName", "CompetencyName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportChartImages_SurveyId_TenantId",
                table: "ReportChartImages",
                columns: new[] { "SurveyId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportChartImages_TenantId",
                table: "ReportChartImages",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportChartImages");
        }
    }
}
