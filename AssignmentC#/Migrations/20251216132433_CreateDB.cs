using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssignmentC_.Migrations
{
    /// <inheritdoc />
    public partial class CreateDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payment_Promotion_PromotionId",
                table: "Payment");

            migrationBuilder.DropIndex(
                name: "IX_Payment_PromotionId",
                table: "Payment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Promotion",
                table: "Promotion");

            migrationBuilder.DropColumn(
                name: "PromotionId",
                table: "Payment");

            migrationBuilder.RenameTable(
                name: "Promotion",
                newName: "Promotions");

            migrationBuilder.AddColumn<DateOnly>(
                name: "CreatedTime",
                table: "Promotions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountValue",
                table: "Promotions",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "Promotions",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EligibilityMode",
                table: "Promotions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "Promotions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentId",
                table: "Promotions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                table: "Promotions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoucherCode",
                table: "Promotions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoucherType",
                table: "Promotions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "points",
                table: "Promotions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "Promotions",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Promotions",
                table: "Promotions",
                column: "PromotionId");

            migrationBuilder.CreateTable(
                name: "VoucherAssignments",
                columns: table => new
                {
                    AssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PromotionId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(5)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoucherAssignments", x => x.AssignmentId);
                    table.ForeignKey(
                        name: "FK_VoucherAssignments_Promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "Promotions",
                        principalColumn: "PromotionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VoucherAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VoucherConditions",
                columns: table => new
                {
                    ConditionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConditionType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MinAge = table.Column<int>(type: "int", nullable: true),
                    MaxAge = table.Column<int>(type: "int", nullable: true),
                    MinSpend = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsFirstPurchase = table.Column<bool>(type: "bit", nullable: true),
                    BirthMonth = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PromotionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoucherConditions", x => x.ConditionId);
                    table.ForeignKey(
                        name: "FK_VoucherConditions_Promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "Promotions",
                        principalColumn: "PromotionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_PaymentId",
                table: "Promotions",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_VoucherAssignments_PromotionId",
                table: "VoucherAssignments",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_VoucherAssignments_UserId",
                table: "VoucherAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VoucherConditions_PromotionId",
                table: "VoucherConditions",
                column: "PromotionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Promotions_Payment_PaymentId",
                table: "Promotions",
                column: "PaymentId",
                principalTable: "Payment",
                principalColumn: "PaymentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Promotions_Payment_PaymentId",
                table: "Promotions");

            migrationBuilder.DropTable(
                name: "VoucherAssignments");

            migrationBuilder.DropTable(
                name: "VoucherConditions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Promotions",
                table: "Promotions");

            migrationBuilder.DropIndex(
                name: "IX_Promotions_PaymentId",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "CreatedTime",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "DiscountValue",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "EligibilityMode",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "PaymentId",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "VoucherCode",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "VoucherType",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "points",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "Promotions");

            migrationBuilder.RenameTable(
                name: "Promotions",
                newName: "Promotion");

            migrationBuilder.AddColumn<int>(
                name: "PromotionId",
                table: "Payment",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Promotion",
                table: "Promotion",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_PromotionId",
                table: "Payment",
                column: "PromotionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payment_Promotion_PromotionId",
                table: "Payment",
                column: "PromotionId",
                principalTable: "Promotion",
                principalColumn: "PromotionId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
