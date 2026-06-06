using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BreastCancer.Migrations
{
    /// <inheritdoc />
    public partial class PreventDuplicateReactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reactions_PostId",
                schema: "community",
                table: "Reactions");

            migrationBuilder.DropIndex(
                name: "IX_Follows_FollowingId",
                schema: "community",
                table: "Follows");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "fc628ff3-201e-448a-8cfb-c24740a90b38");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2",
                column: "ConcurrencyStamp",
                value: "302d1b74-b11e-4fcb-aa26-3fd3454a90db");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "3",
                column: "ConcurrencyStamp",
                value: "9e7cc21a-9c39-4b67-a895-af68974e2f0e");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "4",
                column: "ConcurrencyStamp",
                value: "afa4f131-7ee3-46ce-b34a-5b0b13ab879b");

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_PostId_UserId",
                schema: "community",
                table: "Reactions",
                columns: new[] { "PostId", "UserId" },
                unique: true);

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
                name: "IX_Reactions_PostId_UserId",
                schema: "community",
                table: "Reactions");

            migrationBuilder.DropIndex(
                name: "IX_Follows_FollowingId_CreatedAt",
                schema: "community",
                table: "Follows");

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
                name: "IX_Reactions_PostId",
                schema: "community",
                table: "Reactions",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_Follows_FollowingId",
                schema: "community",
                table: "Follows",
                column: "FollowingId");
        }
    }
}
