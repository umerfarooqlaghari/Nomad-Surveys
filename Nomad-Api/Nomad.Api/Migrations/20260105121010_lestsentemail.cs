using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nomad.Api.Migrations
{
    /// <inheritdoc />
    public partial class lestsentemail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastReminderSentAt",
                table: "SubjectEvaluatorSurveys",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReminderSentAt",
                table: "SubjectEvaluatorSurveys");
        }
    }
}
