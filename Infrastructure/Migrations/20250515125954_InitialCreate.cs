using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GoverningName = table.Column<string>(nullable: true),
                    HeadName = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    Shortname = table.Column<string>(nullable: true),
                    UNP = table.Column<string>(nullable: false),
                    OKPO = table.Column<string>(nullable: true),
                    OKYLP = table.Column<string>(nullable: true),
                    Region = table.Column<string>(nullable: true),
                    Address = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: true),
                    Supervisor = table.Column<string>(nullable: true),
                    ChiefAccountant = table.Column<string>(nullable: true),
                    PasswordHash = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    DeadlineType = table.Column<int>(nullable: false),
                    FilePath = table.Column<string>(nullable: true),
                    Type = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemRoles",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(nullable: false),
                    RoleNameRu = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubmissionDeadlines",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(nullable: true),
                    ReportTemplateId = table.Column<int>(nullable: false),
                    ReportId = table.Column<int>(nullable: true),
                    DeadlineType = table.Column<int>(nullable: false),
                    DeadlineDate = table.Column<DateTime>(nullable: false),
                    Period = table.Column<DateTime>(nullable: false),
                    FixedDay = table.Column<int>(nullable: true),
                    IsClosed = table.Column<bool>(nullable: false),
                    Status = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionDeadlines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionDeadlines_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubmissionDeadlines_ReportTemplates_ReportTemplateId",
                        column: x => x.ReportTemplateId,
                        principalTable: "ReportTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SummaryReports",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(nullable: true),
                    FilePath = table.Column<string>(nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    ReportTemplateId = table.Column<int>(nullable: false),
                    PeriodStart = table.Column<DateTime>(nullable: false),
                    PeriodEnd = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SummaryReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SummaryReports_ReportTemplates_ReportTemplateId",
                        column: x => x.ReportTemplateId,
                        principalTable: "ReportTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(nullable: true),
                    FullName = table.Column<string>(nullable: true),
                    Number = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: true),
                    Position = table.Column<string>(nullable: true),
                    AccessId = table.Column<int>(nullable: true),
                    PasswordHash = table.Column<string>(nullable: true),
                    RoleId = table.Column<int>(nullable: false),
                    BranchId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_SystemRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "SystemRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(nullable: false),
                    Message = table.Column<string>(nullable: true),
                    IsRead = table.Column<bool>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Posts",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostHeader = table.Column<string>(nullable: true),
                    PostText = table.Column<string>(nullable: true),
                    PosterId = table.Column<int>(nullable: false),
                    PostDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Posts_Users_PosterId",
                        column: x => x.PosterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(nullable: true),
                    TemplateId = table.Column<int>(nullable: false),
                    UploadDate = table.Column<DateTime>(nullable: false),
                    Period = table.Column<DateTime>(nullable: false),
                    UploadedById = table.Column<int>(nullable: true),
                    BranchId = table.Column<int>(nullable: true),
                    FilePath = table.Column<string>(nullable: true),
                    Type = table.Column<int>(nullable: false),
                    IsClosed = table.Column<bool>(nullable: false),
                    SummaryReportId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reports_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reports_SummaryReports_SummaryReportId",
                        column: x => x.SummaryReportId,
                        principalTable: "SummaryReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reports_ReportTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "ReportTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reports_Users_UploadedById",
                        column: x => x.UploadedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommentHistory",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportId = table.Column<int>(nullable: true),
                    DeadlineId = table.Column<int>(nullable: true),
                    Comment = table.Column<string>(nullable: true),
                    Status = table.Column<int>(nullable: false),
                    AuthorId = table.Column<int>(nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentHistory_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommentHistory_SubmissionDeadlines_DeadlineId",
                        column: x => x.DeadlineId,
                        principalTable: "SubmissionDeadlines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommentHistory_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Branches_Name",
                table: "Branches",
                column: "Name",
                unique: true,
                filter: "[Name] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CommentHistory_AuthorId",
                table: "CommentHistory",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentHistory_DeadlineId",
                table: "CommentHistory",
                column: "DeadlineId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentHistory_ReportId",
                table: "CommentHistory",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_PosterId",
                table: "Posts",
                column: "PosterId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_BranchId",
                table: "Reports",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_SummaryReportId",
                table: "Reports",
                column: "SummaryReportId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_TemplateId",
                table: "Reports",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_UploadedById",
                table: "Reports",
                column: "UploadedById");

            migrationBuilder.CreateIndex(
                name: "IX_ReportTemplates_Name",
                table: "ReportTemplates",
                column: "Name",
                unique: true,
                filter: "[Name] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionDeadlines_BranchId",
                table: "SubmissionDeadlines",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionDeadlines_ReportTemplateId",
                table: "SubmissionDeadlines",
                column: "ReportTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SummaryReports_ReportTemplateId",
                table: "SummaryReports",
                column: "ReportTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_BranchId",
                table: "Users",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserName",
                table: "Users",
                column: "UserName",
                unique: true,
                filter: "[UserName] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommentHistory");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Posts");

            migrationBuilder.DropTable(
                name: "SubmissionDeadlines");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "SummaryReports");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "ReportTemplates");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "SystemRoles");
        }
    }
}
