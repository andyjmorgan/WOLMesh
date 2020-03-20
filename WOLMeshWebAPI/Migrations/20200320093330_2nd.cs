using Microsoft.EntityFrameworkCore.Migrations;

namespace WOLMeshWebAPI.Migrations
{
    public partial class _2nd : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BroadcastAddress",
                table: "Machines",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IPAddress",
                table: "Machines",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MacAddress",
                table: "Machines",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IPAddress",
                table: "MachineNetworkDetails",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BroadcastAddress",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "IPAddress",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "MacAddress",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "IPAddress",
                table: "MachineNetworkDetails");
        }
    }
}
