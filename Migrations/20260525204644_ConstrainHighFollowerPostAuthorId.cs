using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BreastCancer.Migrations
{
    /// <inheritdoc />
    public partial class ConstrainHighFollowerPostAuthorId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AuthorId",
                schema: "community",
                table: "HighFollowerPosts",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "40bef983-d5ac-4165-b7c7-f6847bd3112d");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2",
                column: "ConcurrencyStamp",
                value: "49b016f1-faec-4fb2-b1b5-bdf4887df424");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "3",
                column: "ConcurrencyStamp",
                value: "cce33309-bc1b-4146-99cc-4dd7e67e4ca3");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "4",
                column: "ConcurrencyStamp",
                value: "b8eb395a-ef35-43ea-9cd5-b5f45fe90bac");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AuthorId",
                schema: "community",
                table: "HighFollowerPosts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "66f47479-aaa2-4a3a-aa69-36d15da1ea89");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2",
                column: "ConcurrencyStamp",
                value: "17a5cad2-bb7b-4c44-918f-79322d1519e0");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "3",
                column: "ConcurrencyStamp",
                value: "a114b23d-2d00-4d52-bbe1-9fe3aa8b1cd1");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "4",
                column: "ConcurrencyStamp",
                value: "3596837a-89e3-41fa-9a02-41bc6fd66498");
        }
    }
}
