using ClinicHub.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicHub.Api.Migrations
{
    /// <summary>Ключ устройства для подключения SmsAgent по QR-коду (колонка Phones.AgentToken).</summary>
    [DbContext(typeof(ClinicHubDbContext))]
    [Migration("20261008000000_AddPhoneAgentToken")]
    public partial class AddPhoneAgentToken : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgentToken",
                table: "Phones",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgentToken",
                table: "Phones");
        }
    }
}
