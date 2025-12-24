using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BreastCancer.Migrations
{
    /// <inheritdoc />
    public partial class V10__AddRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Caregivers_AspNetUsers_Id",
                table: "Caregivers");

            migrationBuilder.DropForeignKey(
                name: "FK_Doctors_AspNetUsers_Id",
                table: "Doctors");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_AspNetUsers_Id",
                table: "Patients");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Patients",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Doctors",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Caregivers",
                newName: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_LicenseNumber",
                table: "Doctors",
                column: "LicenseNumber",
                unique: true,
                filter: "[LicenseNumber] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Caregivers_AspNetUsers_UserId",
                table: "Caregivers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_AspNetUsers_UserId",
                table: "Doctors",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_AspNetUsers_UserId",
                table: "Patients",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Caregivers_AspNetUsers_UserId",
                table: "Caregivers");

            migrationBuilder.DropForeignKey(
                name: "FK_Doctors_AspNetUsers_UserId",
                table: "Doctors");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_AspNetUsers_UserId",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Doctors_LicenseNumber",
                table: "Doctors");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Patients",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Doctors",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Caregivers",
                newName: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Caregivers_AspNetUsers_Id",
                table: "Caregivers",
                column: "Id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_AspNetUsers_Id",
                table: "Doctors",
                column: "Id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_AspNetUsers_Id",
                table: "Patients",
                column: "Id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
