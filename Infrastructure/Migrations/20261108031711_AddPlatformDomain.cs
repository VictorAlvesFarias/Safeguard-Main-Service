using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddPlatformDomain : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlatformDomain",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlatformId = table.Column<int>(type: "integer", nullable: false),
                    Domain = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformDomain", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformDomain_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlatformDomain_Platform_PlatformId",
                        column: x => x.PlatformId,
                        principalTable: "Platform",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformDomain_PlatformId",
                table: "PlatformDomain",
                column: "PlatformId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformDomain_UserId_Domain",
                table: "PlatformDomain",
                columns: new[] { "UserId", "Domain" },
                unique: true);

            // Sites already learned from completed captures become linked domains (newest capture wins).
            migrationBuilder.Sql(@"
                INSERT INTO ""PlatformDomain"" (""UserId"", ""PlatformId"", ""Domain"", ""CreateDate"", ""UpdateDate"", ""Deleted"")
                SELECT DISTINCT ON (c.""UserId"", c.""Domain"") c.""UserId"", e.""PlatformId"", c.""Domain"", now(), now(), false
                FROM ""CapturedCredential"" c
                JOIN ""Email"" e ON e.""Id"" = c.""EmailId""
                WHERE e.""PlatformId"" IS NOT NULL AND c.""Domain"" <> ''
                ORDER BY c.""UserId"", c.""Domain"", c.""UpdateDate"" DESC;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlatformDomain");
        }
    }
}
