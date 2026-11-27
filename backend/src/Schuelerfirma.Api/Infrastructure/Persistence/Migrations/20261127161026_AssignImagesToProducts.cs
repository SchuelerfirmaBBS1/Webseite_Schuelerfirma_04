using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schuelerfirma.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Images no longer point to their owner: products point to their images through <c>ProductImages</c>, which also
    /// holds position and main-image flag. Existing images keep their ids and assignments, in both directions.
    /// </summary>
    public partial class AssignImagesToProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new
                {
                    ImageId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImages", x => x.ImageId);
                    table.ForeignKey(
                        name: "FK_ProductImages_Images_ImageId",
                        column: x => x.ImageId,
                        principalTable: "Images",
                        principalColumn: "ImageId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId",
                table: "ProductImages",
                column: "ProductId");

            // Move owner, position and main-image flag from the image into the assignment.
            migrationBuilder.Sql(
                "INSERT INTO `ProductImages` (`ImageId`, `ProductId`, `SortOrder`, `IsPrimary`) " +
                "SELECT `ImageId`, `ProductId`, `SortOrder`, `IsPrimary` FROM `Images` WHERE `ProductId` IS NOT NULL;");

            migrationBuilder.DropForeignKey(
                name: "FK_Images_Products_ProductId",
                table: "Images");

            migrationBuilder.DropIndex(
                name: "IX_Images_ProductId",
                table: "Images");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Images_ExactlyOneOwner",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "IsPrimary",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Images");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrimary",
                table: "Images",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "Images",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Images",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Move the assignment back into the image. An image without assignment cannot exist in the old model.
            migrationBuilder.Sql(
                "UPDATE `Images` i JOIN `ProductImages` p ON p.`ImageId` = i.`ImageId` " +
                "SET i.`ProductId` = p.`ProductId`, i.`SortOrder` = p.`SortOrder`, i.`IsPrimary` = p.`IsPrimary`;");
            migrationBuilder.Sql("DELETE FROM `Images` WHERE `ProductId` IS NULL;");

            migrationBuilder.DropTable(
                name: "ProductImages");

            migrationBuilder.CreateIndex(
                name: "IX_Images_ProductId",
                table: "Images",
                column: "ProductId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Images_ExactlyOneOwner",
                table: "Images",
                sql: "(`ProductId` IS NOT NULL) = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_Images_Products_ProductId",
                table: "Images",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
