using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nomad.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeIdToSubjectsAndEvaluators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmployeeId",
                table: "Subjects",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeId",
                table: "Evaluators",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // Update existing records with generated EmployeeIds using CTE
            migrationBuilder.Sql(@"
                WITH numbered_subjects AS (
                    SELECT ""Id"", 'SUB' || LPAD(ROW_NUMBER() OVER (PARTITION BY ""TenantId"" ORDER BY ""CreatedAt"")::text, 6, '0') as new_employee_id
                    FROM ""Subjects""
                    WHERE ""EmployeeId"" = ''
                )
                UPDATE ""Subjects""
                SET ""EmployeeId"" = numbered_subjects.new_employee_id
                FROM numbered_subjects
                WHERE ""Subjects"".""Id"" = numbered_subjects.""Id"";
            ");

            migrationBuilder.Sql(@"
                WITH numbered_evaluators AS (
                    SELECT ""Id"", 'EVL' || LPAD(ROW_NUMBER() OVER (PARTITION BY ""TenantId"" ORDER BY ""CreatedAt"")::text, 6, '0') as new_employee_id
                    FROM ""Evaluators""
                    WHERE ""EmployeeId"" = ''
                )
                UPDATE ""Evaluators""
                SET ""EmployeeId"" = numbered_evaluators.new_employee_id
                FROM numbered_evaluators
                WHERE ""Evaluators"".""Id"" = numbered_evaluators.""Id"";
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_EmployeeId_TenantId",
                table: "Subjects",
                columns: new[] { "EmployeeId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Evaluators_EmployeeId_TenantId",
                table: "Evaluators",
                columns: new[] { "EmployeeId", "TenantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subjects_EmployeeId_TenantId",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Evaluators_EmployeeId_TenantId",
                table: "Evaluators");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "Evaluators");
        }
    }
}
