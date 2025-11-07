using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nomad.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsSelfEvaluationFromSurvey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSelfEvaluation",
                table: "Surveys");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSelfEvaluation",
                table: "Surveys",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
