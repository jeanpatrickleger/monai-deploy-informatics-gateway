﻿using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Monai.Deploy.InformaticsGateway.Database.Migrations
{
    public partial class R1_Initialize : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DestinationApplicationEntities",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    AeTitle = table.Column<string>(type: "TEXT", nullable: false),
                    HostIp = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestinationApplicationEntities", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "FileStorageInfo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", nullable: false),
                    StorageRootPath = table.Column<string>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false),
                    Workflows = table.Column<string>(type: "TEXT", nullable: true),
                    Received = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true),
                    TryCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileStorageInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InferenceRequest",
                columns: table => new
                {
                    InferenceRequestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TransactionId = table.Column<string>(type: "TEXT", nullable: false),
                    Priority = table.Column<byte>(type: "INTEGER", nullable: false),
                    InputMetadata = table.Column<string>(type: "TEXT", nullable: true),
                    InputResources = table.Column<string>(type: "TEXT", nullable: true),
                    OutputResources = table.Column<string>(type: "TEXT", nullable: true),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StoragePath = table.Column<string>(type: "TEXT", nullable: false),
                    TryCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InferenceRequest", x => x.InferenceRequestId);
                });

            migrationBuilder.CreateTable(
                name: "MonaiApplicationEntities",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    AeTitle = table.Column<string>(type: "TEXT", nullable: false),
                    Grouping = table.Column<string>(type: "TEXT", nullable: false),
                    Workflows = table.Column<string>(type: "TEXT", nullable: true),
                    IgnoredSopClasses = table.Column<string>(type: "TEXT", nullable: true),
                    Timeout = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonaiApplicationEntities", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "SourceApplicationEntities",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    AeTitle = table.Column<string>(type: "TEXT", nullable: false),
                    HostIp = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceApplicationEntities", x => x.Name);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DestinationApplicationEntities");

            migrationBuilder.DropTable(
                name: "FileStorageInfo");

            migrationBuilder.DropTable(
                name: "InferenceRequest");

            migrationBuilder.DropTable(
                name: "MonaiApplicationEntities");

            migrationBuilder.DropTable(
                name: "SourceApplicationEntities");
        }
    }
}