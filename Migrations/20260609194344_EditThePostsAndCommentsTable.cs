using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BreastCancer.Migrations
{
    /// <inheritdoc />
    public partial class EditThePostsAndCommentsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Follows_FollowingId",
                schema: "community",
                table: "Follows");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "community",
                table: "Comments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "f3260f47-0bdd-4c46-b69d-71d7a9c8bbf4");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2",
                column: "ConcurrencyStamp",
                value: "0b1053f3-ae92-46af-a338-2bfb2b19322b");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "3",
                column: "ConcurrencyStamp",
                value: "0c0c9e5c-fa42-4048-ab44-5ecb267bb5b7");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "4",
                column: "ConcurrencyStamp",
                value: "42ed7c5c-4c9e-4168-bb83-9ea53db20203");

            migrationBuilder.CreateIndex(
                name: "IX_Follows_FollowingId_CreatedAt",
                schema: "community",
                table: "Follows",
                columns: new[] { "FollowingId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Follows_FollowingId_CreatedAt",
                schema: "community",
                table: "Follows");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "community",
                table: "Comments");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "38b77973-9234-4384-b1c0-aea7e9ae6a77");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2",
                column: "ConcurrencyStamp",
                value: "aa42989d-c18a-4f8e-a90d-a5cf256eef31");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "3",
                column: "ConcurrencyStamp",
                value: "523e514f-6135-4425-82eb-f48049a36b03");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "4",
                column: "ConcurrencyStamp",
                value: "e3fb2cc0-a6de-41f8-82f3-9c72220b5b9e");

            migrationBuilder.CreateIndex(
                name: "IX_Follows_FollowingId",
                schema: "community",
                table: "Follows",
                column: "FollowingId");
        }
    }
}
