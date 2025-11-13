using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblRefreshToken",
                columns: table => new
                {
                    RefreshTokenId = table.Column<long>(type: "bigint", nullable: false),
                    UserID = table.Column<long>(type: "bigint", nullable: false),
                    Token = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    DeviceInfo = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    RevokedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    RevokedByIp = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    ReplacedByToken = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblRefreshToken", x => x.RefreshTokenId);
                    table.ForeignKey(
                        name: "FK_RefreshToken_User",
                        column: x => x.UserID,
                        principalTable: "tblUsers",
                        principalColumn: "Userid");
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblRefreshToken_Token",
                table: "tblRefreshToken",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblRefreshToken_UserID",
                table: "tblRefreshToken",
                column: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblRefreshToken");
        }
    }
}
