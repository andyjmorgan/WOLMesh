using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WOLMeshWebAPI.Migrations
{
    public partial class _1st : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MachineNetworkDetails",
                columns: table => new
                {
                    internalkey = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceID = table.Column<string>(nullable: true),
                    NetworkID = table.Column<int>(nullable: false),
                    MacAddress = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineNetworkDetails", x => x.internalkey);
                });

            migrationBuilder.CreateTable(
                name: "Machines",
                columns: table => new
                {
                    ID = table.Column<string>(nullable: false),
                    HostName = table.Column<string>(nullable: true),
                    CurrentUser = table.Column<string>(nullable: true),
                    DomainName = table.Column<string>(nullable: true),
                    WindowsVersion = table.Column<string>(nullable: true),
                    LastHeardFrom = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Machines", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Networks",
                columns: table => new
                {
                    NetworkID = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SubnetMask = table.Column<string>(nullable: true),
                    BroadcastAddress = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Networks", x => x.NetworkID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MachineNetworkDetails");

            migrationBuilder.DropTable(
                name: "Machines");

            migrationBuilder.DropTable(
                name: "Networks");
        }
    }
}
