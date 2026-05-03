using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BreastCancer.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientDiagnosis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatientDiagnoses",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AgeAtDiagnosis = table.Column<int>(type: "int", nullable: false),
                    CancerType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CancerTypeDetailed = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TumorStage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NeoplasmHistologicGrade = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Her2Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Chemotherapy = table.Column<bool>(type: "bit", nullable: false),
                    HormoneTherapy = table.Column<bool>(type: "bit", nullable: false),
                    RadioTherapy = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientDiagnoses", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_PatientDiagnoses_Patients_UserId",
                        column: x => x.UserId,
                        principalTable: "Patients",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "60b0b93c-145f-4c48-868e-91b4ee464c6e");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2",
                column: "ConcurrencyStamp",
                value: "23cfef64-42d8-4ce8-886b-e9b3feb05a9d");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "3",
                column: "ConcurrencyStamp",
                value: "5f7d13ee-3dd5-4449-bf32-f8ed926ebd2f");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "4",
                column: "ConcurrencyStamp",
                value: "f61befd0-11b0-4192-ad27-9f8c259b7d62");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientDiagnoses");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: null);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2",
                column: "ConcurrencyStamp",
                value: null);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "3",
                column: "ConcurrencyStamp",
                value: null);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "4",
                column: "ConcurrencyStamp",
                value: null);
        }
    }
}
