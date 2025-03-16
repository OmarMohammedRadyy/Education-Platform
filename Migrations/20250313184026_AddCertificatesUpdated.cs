using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EducationPlatformN.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificatesUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Courses_CourseId",
                table: "Certificates");

            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Users_UserId",
                table: "Certificates");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Certificates",
                newName: "StudentId");

            migrationBuilder.RenameColumn(
                name: "IssuedAt",
                table: "Certificates",
                newName: "IssuedDate");

            migrationBuilder.RenameIndex(
                name: "IX_Certificates_UserId",
                table: "Certificates",
                newName: "IX_Certificates_StudentId");

            migrationBuilder.AlterColumn<decimal>(
                name: "Value",
                table: "CertificateConditions",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<int>(
                name: "CourseId",
                table: "CertificateConditions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "StudentProgress",
                columns: table => new
                {
                    ProgressId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    LessonId = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentProgress", x => x.ProgressId);
                    table.ForeignKey(
                        name: "FK_StudentProgress_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "CourseId");
                    table.ForeignKey(
                        name: "FK_StudentProgress_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "LessonId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentProgress_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CertificateConditions_CourseId",
                table: "CertificateConditions",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentProgress_CourseId",
                table: "StudentProgress",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentProgress_LessonId",
                table: "StudentProgress",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentProgress_StudentId",
                table: "StudentProgress",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_CertificateConditions_Courses_CourseId",
                table: "CertificateConditions",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "CourseId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Courses_CourseId",
                table: "Certificates",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "CourseId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Users_StudentId",
                table: "Certificates",
                column: "StudentId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CertificateConditions_Courses_CourseId",
                table: "CertificateConditions");

            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Courses_CourseId",
                table: "Certificates");

            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Users_StudentId",
                table: "Certificates");

            migrationBuilder.DropTable(
                name: "StudentProgress");

            migrationBuilder.DropIndex(
                name: "IX_CertificateConditions_CourseId",
                table: "CertificateConditions");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "CertificateConditions");

            migrationBuilder.RenameColumn(
                name: "StudentId",
                table: "Certificates",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "IssuedDate",
                table: "Certificates",
                newName: "IssuedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Certificates_StudentId",
                table: "Certificates",
                newName: "IX_Certificates_UserId");

            migrationBuilder.AlterColumn<decimal>(
                name: "Value",
                table: "CertificateConditions",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Courses_CourseId",
                table: "Certificates",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "CourseId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Users_UserId",
                table: "Certificates",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
