using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentBlazor.Migrations
{
    /// <inheritdoc />
    public partial class AddThreadStateToSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThreadStateJson",
                table: "ChatSessions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThreadStateJson",
                table: "ChatSessions");
        }
    }
}
