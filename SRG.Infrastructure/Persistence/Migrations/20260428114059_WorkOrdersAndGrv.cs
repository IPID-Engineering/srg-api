using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WorkOrdersAndGrv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrderedWorkId",
                table: "WorkEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrderedMaterialId",
                table: "MaterialUsages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkOrderId",
                table: "DailyReports",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GoodsReceivedVouchers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeliveryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceivedVouchers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceivedVouchers_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceivedVouchers_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Direction = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovements_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CrewId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubcontractorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    PlannedStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PlannedEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrders_Crews_CrewId",
                        column: x => x.CrewId,
                        principalTable: "Crews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrders_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrders_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrders_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrders_Users_SubcontractorId",
                        column: x => x.SubcontractorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceivedVoucherItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceivedVoucherId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceivedVoucherItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceivedVoucherItems_GoodsReceivedVouchers_GoodsReceiv~",
                        column: x => x.GoodsReceivedVoucherId,
                        principalTable: "GoodsReceivedVouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoodsReceivedVoucherItems_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderedMaterials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlannedQuantity = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderedMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderedMaterials_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderedMaterials_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderedWorks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PlannedQuantity = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderedWorks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderedWorks_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderedWorks_WorkTypes_WorkTypeId",
                        column: x => x.WorkTypeId,
                        principalTable: "WorkTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntries_OrderedWorkId",
                table: "WorkEntries",
                column: "OrderedWorkId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntries_WorkTypeId",
                table: "WorkEntries",
                column: "WorkTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUsages_MaterialId",
                table: "MaterialUsages",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUsages_OrderedMaterialId",
                table: "MaterialUsages",
                column: "OrderedMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyReports_WorkOrderId",
                table: "DailyReports",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceivedVoucherItems_GoodsReceivedVoucherId",
                table: "GoodsReceivedVoucherItems",
                column: "GoodsReceivedVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceivedVoucherItems_MaterialId",
                table: "GoodsReceivedVoucherItems",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceivedVouchers_CreatedById",
                table: "GoodsReceivedVouchers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceivedVouchers_Number",
                table: "GoodsReceivedVouchers",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceivedVouchers_WarehouseId",
                table: "GoodsReceivedVouchers",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderedMaterials_MaterialId",
                table: "OrderedMaterials",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderedMaterials_WorkOrderId",
                table: "OrderedMaterials",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderedWorks_WorkOrderId",
                table: "OrderedWorks",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderedWorks_WorkTypeId",
                table: "OrderedWorks",
                column: "WorkTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_CreatedById",
                table: "StockMovements",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_MaterialId",
                table: "StockMovements",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_SourceType_SourceId",
                table: "StockMovements",
                columns: new[] { "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_WarehouseId_CreatedAt",
                table: "StockMovements",
                columns: new[] { "WarehouseId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_CreatedById",
                table: "WorkOrders",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_CrewId",
                table: "WorkOrders",
                column: "CrewId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_Number",
                table: "WorkOrders",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_ProjectId",
                table: "WorkOrders",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_SectionId",
                table: "WorkOrders",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_SubcontractorId",
                table: "WorkOrders",
                column: "SubcontractorId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTypes_Code",
                table: "WorkTypes",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DailyReports_WorkOrders_WorkOrderId",
                table: "DailyReports",
                column: "WorkOrderId",
                principalTable: "WorkOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialUsages_Materials_MaterialId",
                table: "MaterialUsages",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialUsages_OrderedMaterials_OrderedMaterialId",
                table: "MaterialUsages",
                column: "OrderedMaterialId",
                principalTable: "OrderedMaterials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkEntries_OrderedWorks_OrderedWorkId",
                table: "WorkEntries",
                column: "OrderedWorkId",
                principalTable: "OrderedWorks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkEntries_WorkTypes_WorkTypeId",
                table: "WorkEntries",
                column: "WorkTypeId",
                principalTable: "WorkTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DailyReports_WorkOrders_WorkOrderId",
                table: "DailyReports");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialUsages_Materials_MaterialId",
                table: "MaterialUsages");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialUsages_OrderedMaterials_OrderedMaterialId",
                table: "MaterialUsages");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkEntries_OrderedWorks_OrderedWorkId",
                table: "WorkEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkEntries_WorkTypes_WorkTypeId",
                table: "WorkEntries");

            migrationBuilder.DropTable(
                name: "GoodsReceivedVoucherItems");

            migrationBuilder.DropTable(
                name: "OrderedMaterials");

            migrationBuilder.DropTable(
                name: "OrderedWorks");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "GoodsReceivedVouchers");

            migrationBuilder.DropTable(
                name: "WorkOrders");

            migrationBuilder.DropTable(
                name: "WorkTypes");

            migrationBuilder.DropIndex(
                name: "IX_WorkEntries_OrderedWorkId",
                table: "WorkEntries");

            migrationBuilder.DropIndex(
                name: "IX_WorkEntries_WorkTypeId",
                table: "WorkEntries");

            migrationBuilder.DropIndex(
                name: "IX_MaterialUsages_MaterialId",
                table: "MaterialUsages");

            migrationBuilder.DropIndex(
                name: "IX_MaterialUsages_OrderedMaterialId",
                table: "MaterialUsages");

            migrationBuilder.DropIndex(
                name: "IX_DailyReports_WorkOrderId",
                table: "DailyReports");

            migrationBuilder.DropColumn(
                name: "OrderedWorkId",
                table: "WorkEntries");

            migrationBuilder.DropColumn(
                name: "OrderedMaterialId",
                table: "MaterialUsages");

            migrationBuilder.DropColumn(
                name: "WorkOrderId",
                table: "DailyReports");
        }
    }
}
