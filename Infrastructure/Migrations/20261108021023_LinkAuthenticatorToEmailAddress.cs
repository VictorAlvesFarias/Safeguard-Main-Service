using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class LinkAuthenticatorToEmailAddress : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmailAddressId",
                table: "Authenticator",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Authenticator_EmailAddressId",
                table: "Authenticator",
                column: "EmailAddressId");

            migrationBuilder.AddForeignKey(
                name: "FK_Authenticator_EmailAddress_EmailAddressId",
                table: "Authenticator",
                column: "EmailAddressId",
                principalTable: "EmailAddress",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Authenticator_EmailAddress_EmailAddressId",
                table: "Authenticator");

            migrationBuilder.DropIndex(
                name: "IX_Authenticator_EmailAddressId",
                table: "Authenticator");

            migrationBuilder.DropColumn(
                name: "EmailAddressId",
                table: "Authenticator");
        }
    }
}
