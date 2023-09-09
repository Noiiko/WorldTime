﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using WorldTime.Data;

#nullable disable

namespace WorldTime.Data.Migrations
{
    [DbContext(typeof(BotDatabaseContext))]
    [Migration("20230909184554_AddEphemeral")]
    partial class AddEphemeral
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("WorldTime.Data.GuildConfiguration", b =>
                {
                    b.Property<decimal>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<bool>("EphemeralConfirm")
                        .HasColumnType("boolean")
                        .HasColumnName("ephemeral_confirm");

                    b.Property<bool>("Use12HourTime")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(false)
                        .HasColumnName("use12hour_time");

                    b.HasKey("GuildId")
                        .HasName("pk_guild_settings");

                    b.ToTable("guild_settings", (string)null);
                });

            modelBuilder.Entity("WorldTime.Data.UserEntry", b =>
                {
                    b.Property<long>("GuildId")
                        .HasColumnType("bigint")
                        .HasColumnName("guild_id");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint")
                        .HasColumnName("user_id");

                    b.Property<string>("TimeZone")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("zone");

                    b.HasKey("GuildId", "UserId")
                        .HasName("userdata_pkey");

                    b.ToTable("userdata", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}
