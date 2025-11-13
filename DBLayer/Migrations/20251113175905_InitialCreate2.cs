using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBLayer.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsUsed",
                table: "tblResetToken",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.CreateTable(
                name: "ApplicationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MessageTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Level = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeStamp = table.Column<DateTime>(type: "datetime", nullable: true),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogEvent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequestPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ActionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Application = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tblApplicationFlag",
                columns: table => new
                {
                    FlagID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyID = table.Column<long>(type: "bigint", nullable: false),
                    FlagName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FlagValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "String"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PossibleValues = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DefaultValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ShowToUser = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsReadOnly = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    ModifiedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime", nullable: true),
                    ModuleNamespace = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblApplicationFlag", x => x.FlagID);
                    table.ForeignKey(
                        name: "FK_ApplicationFlag_Company",
                        column: x => x.CompanyID,
                        principalTable: "tblCompany",
                        principalColumn: "CompanyID");
                });

            migrationBuilder.CreateTable(
                name: "tblPasswordPolicy",
                columns: table => new
                {
                    PasswordPolicyID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyID = table.Column<long>(type: "bigint", nullable: false),
                    MinimumLength = table.Column<int>(type: "int", nullable: false, defaultValue: 12),
                    MaximumLength = table.Column<int>(type: "int", nullable: false, defaultValue: 128),
                    RequireUppercase = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RequireLowercase = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RequireDigit = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RequireSpecialCharacter = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    MinimumUniqueCharacters = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    ProhibitCommonPasswords = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ProhibitSequentialCharacters = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ProhibitRepeatingCharacters = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PasswordExpirationDays = table.Column<int>(type: "int", nullable: true),
                    PasswordHistoryCount = table.Column<int>(type: "int", nullable: true),
                    EnablePasswordExpiry = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MaxLoginAttempts = table.Column<int>(type: "int", nullable: true, defaultValue: 5),
                    LockoutDurationMinutes = table.Column<int>(type: "int", nullable: true, defaultValue: 30),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    ModifiedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblPasswordPolicy", x => x.PasswordPolicyID);
                    table.ForeignKey(
                        name: "FK_PasswordPolicy_Company",
                        column: x => x.CompanyID,
                        principalTable: "tblCompany",
                        principalColumn: "CompanyID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblApplicationFlag_CompanyID",
                table: "tblApplicationFlag",
                column: "CompanyID");

            migrationBuilder.CreateIndex(
                name: "IX_tblPasswordPolicy_CompanyID",
                table: "tblPasswordPolicy",
                column: "CompanyID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationLogs");

            migrationBuilder.DropTable(
                name: "tblApplicationFlag");

            migrationBuilder.DropTable(
                name: "tblPasswordPolicy");

            migrationBuilder.AlterColumn<bool>(
                name: "IsUsed",
                table: "tblResetToken",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");
        }
    }
}
