using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusAstralis.Migrations.NexusStars
{
    /// <inheritdoc />
    public partial class Comments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserNick = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConstellationId = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_constellations_ConstellationId",
                        column: x => x.ConstellationId,
                        principalTable: "constellations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            //migrationBuilder.CreateTable(
            //    name: "ConstellationsStars",
            //    columns: table => new
            //    {
            //        constellation_id = table.Column<int>(type: "int", nullable: false),
            //        star_id = table.Column<int>(type: "int", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_ConstellationsStars", x => new { x.constellation_id, x.star_id });
            //    });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ConstellationId",
                table: "Comments",
                column: "ConstellationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Comments");

            //migrationBuilder.DropTable(
            //    name: "ConstellationsStars");
        }
    }
}
