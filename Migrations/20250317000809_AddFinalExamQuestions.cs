using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EducationPlatformN.Migrations
{
    public partial class AddFinalExamQuestions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // إنشاء جدول FinalExamQuestions
            migrationBuilder.CreateTable(
                name: "FinalExamQuestions",
                columns: table => new
                {
                    FinalExamQuestionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinalExamId = table.Column<int>(type: "int", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrectAnswer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuestionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OptionA = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OptionB = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OptionC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OptionD = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinalExamQuestions", x => x.FinalExamQuestionId);
                    table.ForeignKey(
                        name: "FK_FinalExamQuestions_FinalExams_FinalExamId",
                        column: x => x.FinalExamId,
                        principalTable: "FinalExams",
                        principalColumn: "FinalExamId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinalExamQuestions_FinalExamId",
                table: "FinalExamQuestions",
                column: "FinalExamId");

            // تعديلات أخرى
            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "Notifications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "FinalExams",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "AnswersJson",
                table: "FinalExamResults",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.DropForeignKey(
                name: "FK_LessonNotes_Users_UploadedByUserId",
                table: "LessonNotes");

            migrationBuilder.AddForeignKey(
                name: "FK_LessonNotes_Users_UploadedByUserId",
                table: "LessonNotes",
                column: "UploadedByUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinalExamQuestions");

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "FinalExams",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AnswersJson",
                table: "FinalExamResults",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.DropForeignKey(
                name: "FK_LessonNotes_Users_UploadedByUserId",
                table: "LessonNotes");

            migrationBuilder.AddForeignKey(
                name: "FK_LessonNotes_Users_UploadedByUserId",
                table: "LessonNotes",
                column: "UploadedByUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}