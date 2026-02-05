using System;
using CryptoPay.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoPay.Api.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("CryptoPay.Api.Models.Merchant", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ApiKey")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("WebhookSecret")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("WebhookUrl")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("ApiKey")
                        .IsUnique();

                    b.ToTable("Merchants");
                });

            modelBuilder.Entity("CryptoPay.Api.Models.PaymentIntent", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<int?>("Confirmations")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("CryptoAmount")
                        .IsRequired()
                        .HasColumnType("decimal(18,8)");

                    b.Property<string>("CryptoCurrency")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CustomerEmail")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("FiatAmount")
                        .IsRequired()
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("FiatCurrency")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("LastWebhookSentAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("LastWebhookStatus")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("MerchantId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Network")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("OrderRef")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTime?>("PaidAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("PayAddress")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("ReturnUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<string>("TxHash")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("MerchantId");

                    b.HasIndex("OrderRef");

                    b.HasIndex("PayAddress");

                    b.HasIndex("Status");

                    b.ToTable("PaymentIntents");
                });

            modelBuilder.Entity("CryptoPay.Api.Models.WalletAddress", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsAssigned")
                        .HasColumnType("bit");

                    b.Property<string>("Network")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid?>("PaymentIntentId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.ToTable("WalletAddresses");
                });

            modelBuilder.Entity("CryptoPay.Api.Models.PaymentIntent", b =>
                {
                    b.HasOne("CryptoPay.Api.Models.Merchant", "Merchant")
                        .WithMany("PaymentIntents")
                        .HasForeignKey("MerchantId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Merchant");
                });

            modelBuilder.Entity("CryptoPay.Api.Models.Merchant", b =>
                {
                    b.Navigation("PaymentIntents");
                });
#pragma warning restore 612, 618
        }
    }
}
