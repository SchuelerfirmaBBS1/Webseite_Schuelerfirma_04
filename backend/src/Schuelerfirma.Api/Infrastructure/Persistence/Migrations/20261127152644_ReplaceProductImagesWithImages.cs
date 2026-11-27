using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schuelerfirma.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceProductImagesWithImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    ImageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    FileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContentType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.ImageId);
                    table.CheckConstraint("CK_Images_ExactlyOneOwner", "(`ProductId` IS NOT NULL) = 1");
                    table.ForeignKey(
                        name: "FK_Images_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ImageContents",
                columns: table => new
                {
                    ImageId = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<byte[]>(type: "mediumblob", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageContents", x => x.ImageId);
                    table.ForeignKey(
                        name: "FK_ImageContents_Images_ImageId",
                        column: x => x.ImageId,
                        principalTable: "Images",
                        principalColumn: "ImageId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Images_ProductId",
                table: "Images",
                column: "ProductId");

            // Keep existing product images (same ids), then remove the old tables.
            migrationBuilder.Sql(
                "INSERT INTO `Images` (`ImageId`, `ProductId`, `FileName`, `Description`, `ContentType`, `Length`, `SortOrder`, `IsPrimary`) " +
                "SELECT `ProductImageId`, `ProductId`, `FileName`, `Description`, `ContentType`, `Length`, `SortOrder`, `IsPrimary` FROM `ProductImages`;");
            migrationBuilder.Sql(
                "INSERT INTO `ImageContents` (`ImageId`, `Data`) SELECT `ProductImageId`, `Data` FROM `ProductImageContents`;");

            migrationBuilder.DropTable(
                name: "ProductImageContents");

            migrationBuilder.DropTable(
                name: "ProductImages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new
                {
                    ProductImageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ContentType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsPrimary = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImages", x => x.ProductImageId);
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProductImageContents",
                columns: table => new
                {
                    ProductImageId = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<byte[]>(type: "mediumblob", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImageContents", x => x.ProductImageId);
                    table.ForeignKey(
                        name: "FK_ProductImageContents_ProductImages_ProductImageId",
                        column: x => x.ProductImageId,
                        principalTable: "ProductImages",
                        principalColumn: "ProductImageId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId",
                table: "ProductImages",
                column: "ProductId");

            // Only product images exist when going back; keep them with their ids.
            migrationBuilder.Sql(
                "INSERT INTO `ProductImages` (`ProductImageId`, `ProductId`, `FileName`, `Description`, `ContentType`, `Length`, `SortOrder`, `IsPrimary`) " +
                "SELECT `ImageId`, `ProductId`, `FileName`, `Description`, `ContentType`, `Length`, `SortOrder`, `IsPrimary` FROM `Images` WHERE `ProductId` IS NOT NULL;");
            migrationBuilder.Sql(
                "INSERT INTO `ProductImageContents` (`ProductImageId`, `Data`) " +
                "SELECT c.`ImageId`, c.`Data` FROM `ImageContents` c JOIN `Images` i ON i.`ImageId` = c.`ImageId` WHERE i.`ProductId` IS NOT NULL;");

            migrationBuilder.DropTable(
                name: "ImageContents");

            migrationBuilder.DropTable(
                name: "Images");
        }
    }
}
