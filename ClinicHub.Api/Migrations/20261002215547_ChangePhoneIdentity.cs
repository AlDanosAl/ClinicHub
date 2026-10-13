using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class ChangePhoneIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Phones_DeviceId",
                table: "Phones");

            migrationBuilder.DropColumn(
                name: "ApiKey",
                table: "Phones");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "Phones");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Phones");

            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveFailures",
                table: "SimCards",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastMessageAt",
                table: "SimCards",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastMessageHash",
                table: "SimCards",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PausedUntil",
                table: "SimCards",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Phones",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "Patients",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AutoRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Trigger = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TextTemplate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DaysOffset = table.Column<int>(type: "int", nullable: false),
                    SendTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastRunAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessageTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AutoRuleFires",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AutoRuleId = table.Column<int>(type: "int", nullable: false),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    FiredOnDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoRuleFires", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutoRuleFires_AutoRules_AutoRuleId",
                        column: x => x.AutoRuleId,
                        principalTable: "AutoRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AutoRuleFires_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Phones_PhoneNumber",
                table: "Phones",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AutoRuleFires_AutoRuleId_PatientId_FiredOnDate",
                table: "AutoRuleFires",
                columns: new[] { "AutoRuleId", "PatientId", "FiredOnDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AutoRuleFires_PatientId",
                table: "AutoRuleFires",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutoRuleFires");

            migrationBuilder.DropTable(
                name: "MessageTemplates");

            migrationBuilder.DropTable(
                name: "AutoRules");

            migrationBuilder.DropIndex(
                name: "IX_Phones_PhoneNumber",
                table: "Phones");

            migrationBuilder.DropColumn(
                name: "ConsecutiveFailures",
                table: "SimCards");

            migrationBuilder.DropColumn(
                name: "LastMessageAt",
                table: "SimCards");

            migrationBuilder.DropColumn(
                name: "LastMessageHash",
                table: "SimCards");

            migrationBuilder.DropColumn(
                name: "PausedUntil",
                table: "SimCards");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Phones");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Patients");

            migrationBuilder.AddColumn<string>(
                name: "ApiKey",
                table: "Phones",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "Phones",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Phones",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Phones_DeviceId",
                table: "Phones",
                column: "DeviceId",
                unique: true);
        }
    }
}
