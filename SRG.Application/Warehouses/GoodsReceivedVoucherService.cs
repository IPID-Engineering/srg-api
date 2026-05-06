using System.ComponentModel.DataAnnotations;
using SRG.Application.Audit;
using SRG.Application.Persistence;
using SRG.Domain.Entities;
using SRG.Domain.Enums;

namespace SRG.Application.Warehouses;

public class GoodsReceivedVoucherService(
    IWarehouseRepository warehouse,
    IAuditService auditService) : IGoodsReceivedVoucherService
{
    public async Task<GoodsReceivedVoucherResponse> CreateAsync(
        CreateGoodsReceivedVoucherRequest request,
        Guid createdById,
        CancellationToken cancellationToken = default)
    {
        var number = RequiredText(request.Number, "Number");
        if (await warehouse.GetGoodsReceivedVoucherByNumberAsync(number, cancellationToken) is not null)
        {
            throw new ValidationException("GRV number must be unique.");
        }

        var main = await warehouse.GetMainWarehouseAsync(cancellationToken)
            ?? throw new InvalidOperationException("Main warehouse is missing.");
        var grv = new GoodsReceivedVoucher
        {
            Number = number,
            WarehouseId = main.Id,
            CreatedById = createdById,
            SupplierName = OptionalText(request.SupplierName),
            DeliveryDate = request.DeliveryDate,
            Status = GoodsReceivedVoucherStatus.Draft,
            CreatedAt = DateTime.UtcNow,
        };

        await warehouse.AddGoodsReceivedVoucherAsync(grv, cancellationToken);
        await warehouse.SaveChangesAsync(cancellationToken);
        await auditService.LogActionAsync(createdById, "CREATE_GRV", "GoodsReceivedVoucher", grv.Id, new
        {
            grv.Number,
            grv.WarehouseId,
            grv.SupplierName,
            grv.DeliveryDate,
        }, cancellationToken);

        return ToResponse(grv);
    }

    public async Task<List<GoodsReceivedVoucherResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var grvs = await warehouse.GetGoodsReceivedVouchersAsync(cancellationToken);
        return grvs.Select(ToResponse).ToList();
    }

    public async Task<GoodsReceivedVoucherResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return ToResponse(await GetGrvAsync(id, cancellationToken));
    }

    public async Task<GoodsReceivedVoucherResponse> AddItemAsync(
        Guid id,
        AddGoodsReceivedVoucherItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var grv = await GetDraftGrvAsync(id, cancellationToken);
        _ = await warehouse.GetMaterialByIdAsync(request.MaterialId, cancellationToken)
            ?? throw new KeyNotFoundException("Material was not found.");

        if (request.Quantity <= 0)
        {
            throw new ValidationException("Quantity must be greater than zero.");
        }

        await warehouse.AddGoodsReceivedVoucherItemAsync(new GoodsReceivedVoucherItem
        {
            GoodsReceivedVoucherId = grv.Id,
            MaterialId = request.MaterialId,
            LineNumber = request.LineNumber,
            PartNumber = string.IsNullOrWhiteSpace(request.PartNumber) ? null : request.PartNumber.Trim(),
            VendorPartNumber = string.IsNullOrWhiteSpace(request.VendorPartNumber) ? null : request.VendorPartNumber.Trim(),
            Quantity = request.Quantity,
            Unit = RequiredText(request.Unit, "Unit"),
            UnitPrice = request.UnitPrice,
            ExtendedPrice = request.ExtendedPrice,
            Status = string.IsNullOrWhiteSpace(request.Status) ? null : request.Status.Trim(),
        }, cancellationToken);
        await warehouse.SaveChangesAsync(cancellationToken);
        return ToResponse(await GetGrvAsync(id, cancellationToken));
    }

    public async Task<GoodsReceivedVoucherResponse> ConfirmAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var confirmed = await GetGrvAsync(id, cancellationToken);
        await warehouse.ExecuteInTransactionAsync(async () =>
        {
            var grv = await GetDraftGrvAsync(id, cancellationToken);
            if (grv.Items.Count == 0)
            {
                throw new ValidationException("Cannot confirm GRV without items.");
            }

            foreach (var item in grv.Items)
            {
                await StockService.IncreaseStockAsync(
                    warehouse,
                    grv.WarehouseId,
                    item.MaterialId,
                    item.Quantity,
                    StockMovementSourceType.GRV,
                    grv.Id,
                    userId,
                    cancellationToken);
            }

            grv.Status = GoodsReceivedVoucherStatus.Confirmed;
            await warehouse.SaveChangesAsync(cancellationToken);
            confirmed = grv;
        }, cancellationToken);

        await auditService.LogActionAsync(userId, "CONFIRM_GRV", "GoodsReceivedVoucher", confirmed.Id, new
        {
            confirmed.Number,
            confirmed.WarehouseId,
            Items = confirmed.Items.Select(item => new { item.MaterialId, item.Quantity }),
        }, cancellationToken);

        return ToResponse(confirmed);
    }

    public async Task<GoodsReceivedVoucherResponse> CancelAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var grv = await GetDraftGrvAsync(id, cancellationToken);
        grv.Status = GoodsReceivedVoucherStatus.Cancelled;
        await warehouse.SaveChangesAsync(cancellationToken);
        await auditService.LogActionAsync(userId, "CANCEL_GRV", "GoodsReceivedVoucher", grv.Id, new
        {
            grv.Number,
            grv.Status,
        }, cancellationToken);
        return ToResponse(grv);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var grv = await GetGrvAsync(id, cancellationToken);
        var grvNumber = grv.Number;
        var itemCount = grv.Items.Count;

        await warehouse.ExecuteInTransactionAsync(async () =>
        {
            if (grv.Status == GoodsReceivedVoucherStatus.Confirmed)
            {
                foreach (var item in grv.Items)
                {
                    await StockService.DecreaseStockAsync(
                        warehouse,
                        grv.WarehouseId,
                        item.MaterialId,
                        item.Quantity,
                        StockMovementSourceType.ManualAdjustment,
                        grv.Id,
                        userId,
                        cancellationToken);
                }
            }
            
            await warehouse.RemoveStockMovementsBySourceAsync(grv.Id, cancellationToken);
            warehouse.RemoveGoodsReceivedVoucher(grv);
            await warehouse.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        await auditService.LogActionAsync(userId, "DELETE_GRV", "GoodsReceivedVoucher", id, new
        {
            Number = grvNumber,
            ItemCount = itemCount,
        }, cancellationToken);
    }

    public async Task<GoodsReceivedVoucherResponse> ImportAsync(ImportGrvRequest request, Guid createdById, CancellationToken cancellationToken = default)
    {
        var number = RequiredText(request.Number, "Number");
        if (await warehouse.GetGoodsReceivedVoucherByNumberAsync(number, cancellationToken) is not null)
        {
            throw new ValidationException("GRV number must be unique.");
        }

        if (request.Items == null || request.Items.Count == 0)
        {
            throw new ValidationException("GRV must have at least one item.");
        }

        var main = await warehouse.GetMainWarehouseAsync(cancellationToken)
            ?? throw new InvalidOperationException("Main warehouse is missing.");

        var importCategory = await warehouse.GetOrCreateImportCategoryAsync(cancellationToken);
        var allMaterials = await warehouse.GetMaterialsAsync(cancellationToken);

        GoodsReceivedVoucher confirmed = null!;
        await warehouse.ExecuteInTransactionAsync(async () =>
        {
            var grv = new GoodsReceivedVoucher
            {
                Number = number,
                WarehouseId = main.Id,
                CreatedById = createdById,
                SupplierName = OptionalText(request.SupplierName),
                DeliveryDate = request.DeliveryDate,
                Status = GoodsReceivedVoucherStatus.Draft,
                CreatedAt = DateTime.UtcNow,
            };

            await warehouse.AddGoodsReceivedVoucherAsync(grv, cancellationToken);
            await warehouse.SaveChangesAsync(cancellationToken);

            var allCategories = await warehouse.GetCategoriesAsync(cancellationToken);

            foreach (var item in request.Items)
            {
                if (item.Quantity <= 0) continue;
                var materialName = RequiredText(item.MaterialName, "MaterialName");

                var material = allMaterials.FirstOrDefault(m =>
                    m.Name.Equals(materialName, StringComparison.OrdinalIgnoreCase));

                if (material == null)
                {
                    var categoryId = importCategory.Id;
                    
                    var partNumber = OptionalText(item.PartNumber);
                    if (!string.IsNullOrEmpty(partNumber) && partNumber.Length >= 9)
                    {
                        var digitsOnly = new string(partNumber.Where(char.IsDigit).ToArray());
                        if (digitsOnly.Length >= 9)
                        {
                            var familyCode = digitsOnly.Substring(2, 3);
                            var subFamilyCode = digitsOnly.Substring(5, 4);
                            
                            var matchedCategory = allCategories.FirstOrDefault(c => 
                                c.FamilyCode == familyCode && c.SubFamilyCode == subFamilyCode);
                            if (matchedCategory != null)
                            {
                                categoryId = matchedCategory.Id;
                            }
                            else
                            {
                                var familyCategory = allCategories.FirstOrDefault(c => 
                                    c.FamilyCode == familyCode && c.SubFamilyCode == null);
                                if (familyCategory != null)
                                {
                                    categoryId = familyCategory.Id;
                                }
                            }
                        }
                    }

                    material = new Material
                    {
                        Name = materialName,
                        CategoryId = categoryId,
                        Unit = OptionalText(item.Unit) ?? "szt.",
                        CreatedAt = DateTime.UtcNow,
                    };
                    await warehouse.AddMaterialAsync(material, cancellationToken);
                    await warehouse.SaveChangesAsync(cancellationToken);
                    allMaterials.Add(material);
                }

                await warehouse.AddGoodsReceivedVoucherItemAsync(new GoodsReceivedVoucherItem
                {
                    GoodsReceivedVoucherId = grv.Id,
                    MaterialId = material.Id,
                    LineNumber = item.LineNumber,
                    PartNumber = OptionalText(item.PartNumber),
                    VendorPartNumber = OptionalText(item.VendorPartNumber),
                    Quantity = item.Quantity,
                    Unit = OptionalText(item.Unit) ?? material.Unit,
                    UnitPrice = item.UnitPrice,
                    ExtendedPrice = item.ExtendedPrice,
                    Status = OptionalText(item.Status),
                }, cancellationToken);

                await StockService.IncreaseStockAsync(
                    warehouse,
                    grv.WarehouseId,
                    material.Id,
                    item.Quantity,
                    StockMovementSourceType.GRV,
                    grv.Id,
                    createdById,
                    cancellationToken);
            }

            grv.Status = GoodsReceivedVoucherStatus.Confirmed;
            await warehouse.SaveChangesAsync(cancellationToken);
            confirmed = await GetGrvAsync(grv.Id, cancellationToken);
        }, cancellationToken);

        await auditService.LogActionAsync(createdById, "IMPORT_GRV", "GoodsReceivedVoucher", confirmed.Id, new
        {
            confirmed.Number,
            confirmed.WarehouseId,
            ItemCount = confirmed.Items.Count,
        }, cancellationToken);

        return ToResponse(confirmed);
    }

    private async Task<GoodsReceivedVoucher> GetDraftGrvAsync(Guid id, CancellationToken cancellationToken)
    {
        var grv = await GetGrvAsync(id, cancellationToken);
        if (grv.Status != GoodsReceivedVoucherStatus.Draft)
        {
            throw new ValidationException("Only Draft GRV can be changed.");
        }

        return grv;
    }

    private async Task<GoodsReceivedVoucher> GetGrvAsync(Guid id, CancellationToken cancellationToken)
    {
        return await warehouse.GetGoodsReceivedVoucherByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("GRV was not found.");
    }

    private static GoodsReceivedVoucherResponse ToResponse(GoodsReceivedVoucher grv)
    {
        return new GoodsReceivedVoucherResponse(
            grv.Id,
            grv.Number,
            grv.WarehouseId,
            grv.CreatedById,
            grv.SupplierName,
            grv.DeliveryDate,
            grv.Status,
            grv.CreatedAt,
            grv.Items.Select(item => new GoodsReceivedVoucherItemResponse(
                item.Id,
                item.GoodsReceivedVoucherId,
                item.MaterialId,
                item.Material?.Name,
                item.LineNumber,
                item.PartNumber,
                item.VendorPartNumber,
                item.Quantity,
                item.Unit,
                item.UnitPrice,
                item.ExtendedPrice,
                item.Status)).ToList());
    }

    private static string RequiredText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static string? OptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
