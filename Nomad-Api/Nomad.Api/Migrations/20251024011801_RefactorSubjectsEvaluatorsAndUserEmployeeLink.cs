using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nomad.Api.Migrations
{
    /// <inheritdoc />
    public partial class RefactorSubjectsEvaluatorsAndUserEmployeeLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Evaluators_EvaluatorId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Subjects_SubjectId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_Email_TenantId",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Evaluators_EvaluatorEmail_TenantId",
                table: "Evaluators");

            migrationBuilder.DropIndex(
                name: "IX_Employees_EvaluatorId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_SubjectId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "BusinessUnit",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "Designation",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "Grade",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "Metadata1",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "Metadata2",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "Tenure",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "BusinessUnit",
                table: "Evaluators");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "Evaluators");

            migrationBuilder.DropColumn(
                name: "Designation",
                table: "Evaluators");

            migrationBuilder.DropColumn(
                name: "EvaluatorEmail",
                table: "Evaluators");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Evaluators");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Evaluators");

            migrationBuilder.DropColumn(
                name: "Grade",
                table: "Evaluators");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Evaluators");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Evaluators");

            migrationBuilder.DropColumn(
                name: "Metadata1",
                table: "Evaluators");

            migrationBuilder.DropColumn(
                name: "Metadata2",
                table: "Evaluators");

            migrationBuilder.DropColumn(
                name: "Tenure",
                table: "Evaluators");

            migrationBuilder.DropColumn(
                name: "EvaluatorId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "Employees");

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Designation",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EmployeeId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Grade",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata1",
                table: "Users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata2",
                table: "Users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Tenure",
                table: "Users",
                type: "integer",
                nullable: true);

            // First, delete all existing data from Subjects and Evaluators tables
            // since we're changing the EmployeeId from string to Guid FK
            migrationBuilder.Sql("DELETE FROM \"SubjectEvaluators\";");
            migrationBuilder.Sql("DELETE FROM \"Subjects\";");
            migrationBuilder.Sql("DELETE FROM \"Evaluators\";");

            // Drop the old EmployeeId column (string)
            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "Evaluators");

            // Add the new EmployeeId column (Guid FK)
            migrationBuilder.AddColumn<Guid>(
                name: "EmployeeId",
                table: "Subjects",
                type: "uuid",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "EmployeeId",
                table: "Evaluators",
                type: "uuid",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployeeId",
                table: "Users",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_EmployeeId",
                table: "Subjects",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Evaluators_EmployeeId",
                table: "Evaluators",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Evaluators_Employees_EmployeeId",
                table: "Evaluators",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Employees_EmployeeId",
                table: "Subjects",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Employees_EmployeeId",
                table: "Users",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Evaluators_Employees_EmployeeId",
                table: "Evaluators");

            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Employees_EmployeeId",
                table: "Subjects");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Employees_EmployeeId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_EmployeeId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_EmployeeId",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Evaluators_EmployeeId",
                table: "Evaluators");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Designation",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Grade",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Metadata1",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Metadata2",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Tenure",
                table: "Users");

            // Drop the Guid EmployeeId column
            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "Evaluators");

            // Add back the string EmployeeId column
            migrationBuilder.AddColumn<string>(
                name: "EmployeeId",
                table: "Subjects",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeId",
                table: "Evaluators",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "BusinessUnit",
                table: "Subjects",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "Subjects",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Designation",
                table: "Subjects",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Subjects",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Subjects",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Subjects",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Grade",
                table: "Subjects",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Subjects",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Subjects",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata1",
                table: "Subjects",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata2",
                table: "Subjects",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Tenure",
                table: "Subjects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessUnit",
                table: "Evaluators",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "Evaluators",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Designation",
                table: "Evaluators",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvaluatorEmail",
                table: "Evaluators",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Evaluators",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Evaluators",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Grade",
                table: "Evaluators",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Evaluators",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Evaluators",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata1",
                table: "Evaluators",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata2",
                table: "Evaluators",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Tenure",
                table: "Evaluators",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EvaluatorId",
                table: "Employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubjectId",
                table: "Employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_Email_TenantId",
                table: "Subjects",
                columns: new[] { "Email", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Evaluators_EvaluatorEmail_TenantId",
                table: "Evaluators",
                columns: new[] { "EvaluatorEmail", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EvaluatorId",
                table: "Employees",
                column: "EvaluatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_SubjectId",
                table: "Employees",
                column: "SubjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Evaluators_EvaluatorId",
                table: "Employees",
                column: "EvaluatorId",
                principalTable: "Evaluators",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Subjects_SubjectId",
                table: "Employees",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
