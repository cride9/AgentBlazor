using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentBlazor.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentContextIdToSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AgentContextId",
                table: "ChatSessions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgentContextId",
                table: "ChatSessions");
        }
    }
}
