using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LossLens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DefectCodes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefectCodes", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "ProductionLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionLines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shifts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stations_ProductionLines_LineId",
                        column: x => x.LineId,
                        principalTable: "ProductionLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HourlyRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Hour = table.Column<int>(type: "integer", nullable: false),
                    LineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlannedUnits = table.Column<int>(type: "integer", nullable: false),
                    GoodUnits = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HourlyRecords", x => x.Id);
                    table.CheckConstraint("CK_HourlyRecords_Hour", "\"Hour\" BETWEEN 0 AND 23");
                    table.CheckConstraint("CK_HourlyRecords_Units", "\"PlannedUnits\" >= 0 AND \"GoodUnits\" >= 0");
                    table.ForeignKey(
                        name: "FK_HourlyRecords_ProductionLines_LineId",
                        column: x => x.LineId,
                        principalTable: "ProductionLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HourlyRecords_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DefectEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Hour = table.Column<int>(type: "integer", nullable: false),
                    StationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefectCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefectEntries", x => x.Id);
                    table.CheckConstraint("CK_DefectEntries_Count", "\"Count\" >= 0");
                    table.CheckConstraint("CK_DefectEntries_Hour", "\"Hour\" BETWEEN 0 AND 23");
                    table.ForeignKey(
                        name: "FK_DefectEntries_DefectCodes_DefectCode",
                        column: x => x.DefectCode,
                        principalTable: "DefectCodes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefectEntries_Stations_StationId",
                        column: x => x.StationId,
                        principalTable: "Stations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TestRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Hour = table.Column<int>(type: "integer", nullable: false),
                    StationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestedUnits = table.Column<int>(type: "integer", nullable: false),
                    FirstPassUnits = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestRecords", x => x.Id);
                    table.CheckConstraint("CK_TestRecords_Hour", "\"Hour\" BETWEEN 0 AND 23");
                    table.CheckConstraint("CK_TestRecords_Units", "\"TestedUnits\" >= 0 AND \"FirstPassUnits\" >= 0 AND \"FirstPassUnits\" <= \"TestedUnits\"");
                    table.ForeignKey(
                        name: "FK_TestRecords_Stations_StationId",
                        column: x => x.StationId,
                        principalTable: "Stations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DefectEntries_Date_StationId_Hour_DefectCode",
                table: "DefectEntries",
                columns: new[] { "Date", "StationId", "Hour", "DefectCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DefectEntries_DefectCode",
                table: "DefectEntries",
                column: "DefectCode");

            migrationBuilder.CreateIndex(
                name: "IX_DefectEntries_StationId",
                table: "DefectEntries",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_HourlyRecords_Date_LineId_Hour",
                table: "HourlyRecords",
                columns: new[] { "Date", "LineId", "Hour" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HourlyRecords_LineId",
                table: "HourlyRecords",
                column: "LineId");

            migrationBuilder.CreateIndex(
                name: "IX_HourlyRecords_ShiftId",
                table: "HourlyRecords",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionLines_Name",
                table: "ProductionLines",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_Name",
                table: "Shifts",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stations_LineId_Type",
                table: "Stations",
                columns: new[] { "LineId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestRecords_Date_StationId_Hour",
                table: "TestRecords",
                columns: new[] { "Date", "StationId", "Hour" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestRecords_StationId",
                table: "TestRecords",
                column: "StationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DefectEntries");

            migrationBuilder.DropTable(
                name: "HourlyRecords");

            migrationBuilder.DropTable(
                name: "TestRecords");

            migrationBuilder.DropTable(
                name: "DefectCodes");

            migrationBuilder.DropTable(
                name: "Shifts");

            migrationBuilder.DropTable(
                name: "Stations");

            migrationBuilder.DropTable(
                name: "ProductionLines");
        }
    }
}
