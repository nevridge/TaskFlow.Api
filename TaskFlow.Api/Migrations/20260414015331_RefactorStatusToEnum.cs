using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TaskFlow.Api.Migrations
{
    /// <inheritdoc />
    public partial class RefactorStatusToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_Statuses_StatusId",
                table: "TaskItems");

            migrationBuilder.DropTable(
                name: "Statuses");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_StatusId",
                table: "TaskItems");

            // Remap legacy status values to the new enum before renaming the column.
            // Old: 1=Todo, 2=In Progress, 3=Done
            // New: 0=Draft, 1=Todo, 2=Completed
            migrationBuilder.Sql("UPDATE \"TaskItems\" SET \"StatusId\" = 2 WHERE \"StatusId\" = 3");  // Done → Completed
            migrationBuilder.Sql("UPDATE \"TaskItems\" SET \"StatusId\" = 1 WHERE \"StatusId\" = 2");  // In Progress → Todo
            // StatusId = 1 (Todo) already matches Status.Todo = 1; no update needed.

            migrationBuilder.RenameColumn(
                name: "StatusId",
                table: "TaskItems",
                newName: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remap new enum values back to legacy status IDs before recreating the FK.
            // New: 0=Draft, 1=Todo, 2=Completed
            // Old: 1=Todo, 2=In Progress, 3=Done
            migrationBuilder.Sql("UPDATE \"TaskItems\" SET \"Status\" = 3 WHERE \"Status\" = 2");  // Completed → Done
            migrationBuilder.Sql("UPDATE \"TaskItems\" SET \"Status\" = 1 WHERE \"Status\" = 0");  // Draft → Todo (no legacy equivalent)
            // Status = 1 (Todo) already matches the legacy StatusId = 1; no update needed.

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "TaskItems",
                newName: "StatusId");

            migrationBuilder.CreateTable(
                name: "Statuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Statuses", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Statuses",
                columns: new[] { "Id", "CreatedDate", "Description", "Name", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Task is pending", "Todo", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Task is being worked on", "In Progress", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Task is completed", "Done", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_StatusId",
                table: "TaskItems",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Statuses_Name",
                table: "Statuses",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_Statuses_StatusId",
                table: "TaskItems",
                column: "StatusId",
                principalTable: "Statuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
