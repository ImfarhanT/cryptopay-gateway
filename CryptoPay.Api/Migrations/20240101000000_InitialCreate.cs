using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoPay.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Merchants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApiKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WebhookUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WebhookSecret = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Merchants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WalletAddresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Network = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAssigned = table.Column<bool>(type: "bit", nullable: false),
                    PaymentIntentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletAddresses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentIntents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderRef = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FiatCurrency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FiatAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CryptoCurrency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Network = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomerEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReturnUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PayAddress = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CryptoAmount = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    TxHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Confirmations = table.Column<int>(type: "int", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastWebhookStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastWebhookSentAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentIntents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentIntents_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_ApiKey",
                table: "Merchants",
                column: "ApiKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_OrderRef",
                table: "PaymentIntents",
                column: "OrderRef");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_PayAddress",
                table: "PaymentIntents",
                column: "PayAddress");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_Status",
                table: "PaymentIntents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_MerchantId",
                table: "PaymentIntents",
                column: "MerchantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentIntents");

            migrationBuilder.DropTable(
                name: "WalletAddresses");

            migrationBuilder.DropTable(
                name: "Merchants");
        }
    }
}
