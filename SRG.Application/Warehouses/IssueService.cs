using System.ComponentModel.DataAnnotations;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using QRCoder;
using SRG.Application.Audit;
using SRG.Application.Persistence;
using SRG.Domain.Entities;
using SRG.Domain.Enums;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace SRG.Application.Warehouses;

public class IssueService(
    IWarehouseRepository warehouse,
    IWorkOrderRepository workOrderRepository,
    IConstructionRepository constructionRepository,
    IAuditService auditService) : IIssueService
{
    public async Task<List<IssueResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var issues = await warehouse.GetIssuesAsync(cancellationToken);
        return issues.Select(ToResponse).ToList();
    }

    public async Task<List<IssueResponse>> GetByWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default)
    {
        var issues = await warehouse.GetIssuesByWorkOrderAsync(workOrderId, cancellationToken);
        return issues.Select(ToResponse).ToList();
    }

    public async Task<IssueResponse> CreateIssueAsync(
        CreateIssueRequest request,
        Guid createdById,
        CancellationToken cancellationToken = default)
    {
        var workOrder = await workOrderRepository.GetWorkOrderByIdAsync(request.WorkOrderId, cancellationToken)
            ?? throw new KeyNotFoundException("Work order was not found.");

        var crewOwnerId = workOrder.CrewId ?? workOrder.SubcontractorCrewId;
        if (!crewOwnerId.HasValue)
        {
            throw new ValidationException("Work order must have an assigned crew to create an issue.");
        }

        var main = await warehouse.GetMainWarehouseAsync(cancellationToken)
            ?? throw new InvalidOperationException("Main warehouse is missing.");

        var destination = await warehouse.GetSubWarehouseByOwnerAsync(crewOwnerId.Value, cancellationToken);
        if (destination == null)
        {
            var crewName = workOrder.Crew?.Name ?? workOrder.SubcontractorCrew?.Name ?? "Brygada";
            destination = new Warehouse
            {
                Name = crewName,
                Type = WarehouseType.Sub,
                OwnerId = crewOwnerId.Value,
            };
            await warehouse.AddWarehouseAsync(destination, cancellationToken);
            await warehouse.SaveChangesAsync(cancellationToken);
        }

        var nextNumber = await warehouse.GetNextIssueNumberAsync(cancellationToken);
        var issue = new Issue
        {
            Number = $"W{nextNumber:D4}",
            WorkOrderId = workOrder.Id,
            FromWarehouseId = main.Id,
            ToWarehouseId = destination.Id,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow,
            Status = IssueStatus.Draft,
        };

        await warehouse.AddIssueAsync(issue, cancellationToken);
        await warehouse.SaveChangesAsync(cancellationToken);

        issue.WorkOrder = workOrder;

        await auditService.LogActionAsync(createdById, "CREATE_ISSUE", "Issue", issue.Id, new
        {
            issue.WorkOrderId,
            issue.FromWarehouseId,
            issue.ToWarehouseId,
            issue.Status,
        }, cancellationToken);

        return ToResponse(issue);
    }

    public async Task<IssueResponse> AddItemAsync(
        Guid issueId,
        AddIssueItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var issue = await GetDraftIssueAsync(issueId, cancellationToken);
        _ = await warehouse.GetMaterialByIdAsync(request.MaterialId, cancellationToken)
            ?? throw new KeyNotFoundException("Material was not found.");

        if (request.Quantity <= 0)
        {
            throw new ValidationException("Quantity must be greater than zero.");
        }

        await warehouse.AddIssueItemAsync(new IssueItem
        {
            IssueId = issue.Id,
            MaterialId = request.MaterialId,
            Quantity = request.Quantity,
        }, cancellationToken);
        await warehouse.SaveChangesAsync(cancellationToken);

        return ToResponse(await GetIssueAsync(issueId, cancellationToken));
    }

    public async Task<IssueResponse> ConfirmIssueAsync(Guid issueId, ConfirmIssueRequest request, CancellationToken cancellationToken = default)
    {
        var confirmed = await GetIssueAsync(issueId, cancellationToken);

        await warehouse.ExecuteInTransactionAsync(async () =>
        {
            var issue = await GetDraftIssueAsync(issueId, cancellationToken);

            if (issue.Items.Count == 0)
            {
                throw new ValidationException("Cannot confirm Issue without items.");
            }

            foreach (var item in issue.Items)
            {
                await StockService.DecreaseStockAsync(warehouse, issue.FromWarehouseId, item.MaterialId, item.Quantity, StockMovementSourceType.Issue, issue.Id, issue.CreatedById, cancellationToken);
                await StockService.IncreaseStockAsync(warehouse, issue.ToWarehouseId, item.MaterialId, item.Quantity, StockMovementSourceType.Issue, issue.Id, issue.CreatedById, cancellationToken);
            }

            issue.Status = IssueStatus.Confirmed;
            issue.ConfirmedAt = DateTime.UtcNow;
            issue.VerificationCode = GenerateVerificationCode(issue.Id, issue.Number);
            
            if (request.ReceivedByWorkerId.HasValue)
            {
                var worker = await constructionRepository.GetWorkerByIdAsync(request.ReceivedByWorkerId.Value, cancellationToken);
                if (worker != null)
                {
                    issue.ReceivedByWorkerId = worker.Id;
                    issue.ReceivedByName = $"{worker.FirstName} {worker.LastName}";
                }
            }
            else if (request.ReceivedBySubcontractorWorkerId.HasValue)
            {
                var worker = await constructionRepository.GetSubcontractorWorkerByIdAsync(request.ReceivedBySubcontractorWorkerId.Value, cancellationToken);
                if (worker != null)
                {
                    issue.ReceivedBySubcontractorWorkerId = worker.Id;
                    issue.ReceivedByName = $"{worker.FirstName} {worker.LastName}";
                }
            }
            
            await warehouse.SaveChangesAsync(cancellationToken);
            confirmed = issue;
        }, cancellationToken);
        await auditService.LogActionAsync(confirmed.CreatedById, "CONFIRM_ISSUE", "Issue", confirmed.Id, new
        {
            confirmed.FromWarehouseId,
            confirmed.ToWarehouseId,
            confirmed.ReceivedByName,
            Items = confirmed.Items.Select(item => new { item.MaterialId, item.Quantity }),
        }, cancellationToken);

        return ToResponse(confirmed);
    }

    public async Task<List<IssueWorkerOption>> GetWorkersForIssueAsync(Guid issueId, CancellationToken cancellationToken = default)
    {
        var issue = await GetIssueAsync(issueId, cancellationToken);
        var workOrder = await workOrderRepository.GetWorkOrderByIdAsync(issue.WorkOrderId, cancellationToken)
            ?? throw new KeyNotFoundException("Work order was not found.");

        var result = new List<IssueWorkerOption>();

        if (workOrder.CrewId.HasValue)
        {
            var workers = await constructionRepository.GetWorkerByCrewAsync(workOrder.CrewId.Value, cancellationToken);
            result.AddRange(workers.Select(w => new IssueWorkerOption(w.Id, $"{w.FirstName} {w.LastName}", false)));
        }
        else if (workOrder.SubcontractorCrewId.HasValue)
        {
            var workers = await constructionRepository.GetSubcontractorWorkersByCrewAsync(workOrder.SubcontractorCrewId.Value, cancellationToken);
            result.AddRange(workers.Select(w => new IssueWorkerOption(w.Id, $"{w.FirstName} {w.LastName}", true)));
        }

        return result;
    }

    public async Task<IssueVerificationResponse> VerifyIssueAsync(Guid issueId, string code, CancellationToken cancellationToken = default)
    {
        var issue = await warehouse.GetIssueByIdAsync(issueId, cancellationToken);
        
        if (issue == null)
        {
            return new IssueVerificationResponse(
                false, null, null, null, null, null, null, 0,
                "Dokument o podanym identyfikatorze nie istnieje w systemie.");
        }
        
        if (issue.Status != IssueStatus.Confirmed)
        {
            return new IssueVerificationResponse(
                false, issue.Number, null, null, null, null, null, 0,
                "Dokument nie został potwierdzony.");
        }
        
        var expectedCode = GenerateVerificationCode(issue.Id, issue.Number);
        if (!string.Equals(code, expectedCode, StringComparison.OrdinalIgnoreCase))
        {
            return new IssueVerificationResponse(
                false, null, null, null, null, null, null, 0,
                "Nieprawidłowy kod weryfikacyjny. Dokument może być sfałszowany.");
        }
        
        var workOrder = await workOrderRepository.GetWorkOrderByIdAsync(issue.WorkOrderId, cancellationToken);
        var crewName = workOrder?.Crew?.Name ?? workOrder?.SubcontractorCrew?.Name;
        var projectName = workOrder?.Project?.Name;
        var issuedByName = issue.CreatedBy != null ? $"{issue.CreatedBy.FirstName} {issue.CreatedBy.LastName}" : null;
        
        return new IssueVerificationResponse(
            true,
            issue.Number,
            projectName,
            crewName,
            issuedByName,
            issue.ReceivedByName,
            issue.ConfirmedAt,
            issue.Items.Count,
            "Dokument jest autentyczny i został zweryfikowany pomyślnie.");
    }

    public async Task<byte[]> GenerateProtocolAsync(Guid issueId, CancellationToken cancellationToken = default)
    {
        var issue = await GetIssueAsync(issueId, cancellationToken);
        
        if (issue.Status != IssueStatus.Confirmed)
        {
            throw new ValidationException("Protokół można wygenerować tylko dla potwierdzonych wydań.");
        }

        var workOrder = await workOrderRepository.GetWorkOrderByIdAsync(issue.WorkOrderId, cancellationToken);
        var crewName = workOrder?.Crew?.Name ?? workOrder?.SubcontractorCrew?.Name ?? "—";
        var projectName = workOrder?.Project?.Name ?? "—";
        var issuedByName = issue.CreatedBy != null ? $"{issue.CreatedBy.FirstName} {issue.CreatedBy.LastName}" : "—";
        var issueDate = (issue.ConfirmedAt ?? issue.CreatedAt).ToLocalTime();
        
        var verificationUrl = $"https://srg.bmscope.com/verify/{issueId}?code={issue.VerificationCode}";

        using var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();

            body.AppendChild(CreateParagraph("PROTOKÓŁ PRZEKAZANIA MATERIAŁÓW", true, 24, JustificationValues.Center));
            body.AppendChild(CreateParagraph($"Nr {issue.Number}", true, 14, JustificationValues.Center));
            body.AppendChild(CreateParagraph("", false, 10));
            
            body.AppendChild(CreateParagraph(
                "Niniejszy dokument potwierdza przekazanie materiałów budowlanych z magazynu głównego " +
                "do magazynu brygady wykonawczej. Strona wydająca oświadcza, że wymienione materiały " +
                "zostały wydane w ilościach i stanie zgodnym z poniższym zestawieniem. " +
                "Strona odbierająca potwierdza odbiór materiałów bez zastrzeżeń.",
                false, 10, JustificationValues.Both));
            body.AppendChild(CreateParagraph("", false, 8));

            var infoTable = new Table();
            infoTable.AppendChild(new TableProperties(
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" },
                    new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" },
                    new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" },
                    new RightBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" }
                )
            ));
            infoTable.AppendChild(CreateInfoRow("Data i godzina wydania:", $"{issueDate:dd.MM.yyyy, HH:mm}"));
            infoTable.AppendChild(CreateInfoRow("Numer zlecenia:", workOrder?.Number ?? "—"));
            infoTable.AppendChild(CreateInfoRow("Projekt:", projectName));
            infoTable.AppendChild(CreateInfoRow("Brygada odbierająca:", crewName));
            body.AppendChild(infoTable);
            
            body.AppendChild(CreateParagraph("", false, 10));
            body.AppendChild(CreateParagraph("WYKAZ PRZEKAZANYCH MATERIAŁÓW", true, 12, JustificationValues.Left));
            body.AppendChild(CreateParagraph("", false, 6));

            var table = new Table();
            table.AppendChild(new TableProperties(
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 6 },
                    new BottomBorder { Val = BorderValues.Single, Size = 6 },
                    new LeftBorder { Val = BorderValues.Single, Size = 6 },
                    new RightBorder { Val = BorderValues.Single, Size = 6 },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
                ),
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }
            ));

            var headerRow = new TableRow();
            headerRow.AppendChild(CreateTableCell("Lp.", true, "400", true));
            headerRow.AppendChild(CreateTableCell("Nazwa materiału", true, "3200", true));
            headerRow.AppendChild(CreateTableCell("Ilość", true, "700", true));
            headerRow.AppendChild(CreateTableCell("J.m.", true, "700", true));
            table.AppendChild(headerRow);

            var idx = 1;
            foreach (var item in issue.Items)
            {
                var row = new TableRow();
                row.AppendChild(CreateTableCell($"{idx}.", false, "400"));
                row.AppendChild(CreateTableCell(item.Material?.Name ?? "—", false, "3200"));
                row.AppendChild(CreateTableCell(item.Quantity.ToString("N2"), false, "700"));
                row.AppendChild(CreateTableCell(item.Material?.Unit ?? "szt.", false, "700"));
                table.AppendChild(row);
                idx++;
            }

            body.AppendChild(table);
            body.AppendChild(CreateParagraph("", false, 14));

            var signatureTable = new Table();
            signatureTable.AppendChild(new TableProperties(
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }
            ));

            var sigRow = new TableRow();
            
            var leftCell = new TableCell();
            leftCell.AppendChild(new TableCellProperties(new TableCellWidth { Width = "2500", Type = TableWidthUnitValues.Pct }));
            leftCell.AppendChild(CreateParagraphForCell("ODBIERAJĄCY", true, JustificationValues.Left));
            leftCell.AppendChild(CreateParagraphForCell("", false));
            leftCell.AppendChild(CreateParagraphForCell(issue.ReceivedByName ?? "—", false));
            leftCell.AppendChild(CreateParagraphForCell("", false));
            leftCell.AppendChild(CreateParagraphForCell("", false));
            leftCell.AppendChild(CreateParagraphForCell("........................................", false));
            leftCell.AppendChild(CreateParagraphForCell("(data i czytelny podpis)", false, JustificationValues.Left, 8));
            sigRow.AppendChild(leftCell);

            var rightCell = new TableCell();
            rightCell.AppendChild(new TableCellProperties(new TableCellWidth { Width = "2500", Type = TableWidthUnitValues.Pct }));
            rightCell.AppendChild(CreateParagraphForCell("WYDAJĄCY", true, JustificationValues.Right));
            rightCell.AppendChild(CreateParagraphForCell("", false));
            rightCell.AppendChild(CreateParagraphForCell(issuedByName, false, JustificationValues.Right));
            rightCell.AppendChild(CreateParagraphForCell("", false));
            rightCell.AppendChild(CreateParagraphForCell("", false));
            rightCell.AppendChild(CreateParagraphForCell("........................................", false, JustificationValues.Right));
            rightCell.AppendChild(CreateParagraphForCell("(data i czytelny podpis)", false, JustificationValues.Right, 8));
            sigRow.AppendChild(rightCell);

            signatureTable.AppendChild(sigRow);
            body.AppendChild(signatureTable);

            body.AppendChild(CreateParagraph("", false, 20));
            
            var qrBytes = GenerateQrCode(verificationUrl);
            var imagePart = mainPart.AddImagePart(ImagePartType.Png);
            using (var qrStream = new MemoryStream(qrBytes))
            {
                imagePart.FeedData(qrStream);
            }

            var footerTable = new Table();
            footerTable.AppendChild(new TableProperties(
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }
            ));
            
            var footerRow = new TableRow();
            
            var qrCell = new TableCell();
            qrCell.AppendChild(new TableCellProperties(new TableCellWidth { Width = "1000", Type = TableWidthUnitValues.Pct }));
            qrCell.AppendChild(CreateImageParagraph(mainPart.GetIdOfPart(imagePart), 500000, 500000));
            footerRow.AppendChild(qrCell);
            
            var textCell = new TableCell();
            textCell.AppendChild(new TableCellProperties(
                new TableCellWidth { Width = "4000", Type = TableWidthUnitValues.Pct },
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
            ));
            textCell.AppendChild(CreateParagraphForCell("Zeskanuj kod QR aby zweryfikować autentyczność dokumentu", false, JustificationValues.Left, 8));
            textCell.AppendChild(CreateParagraphForCell($"ID: {issueId:N}", false, JustificationValues.Left, 7));
            footerRow.AppendChild(textCell);
            
            footerTable.AppendChild(footerRow);
            body.AppendChild(footerTable);

            mainPart.Document.AppendChild(body);
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }
    
    private static byte[] GenerateQrCode(string content)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(3);
    }
    
    private static string GenerateVerificationCode(Guid issueId, string issueNumber)
    {
        var secret = "SRG-VERIFICATION-SECRET-2024";
        var data = $"{issueId}:{issueNumber}:{secret}";
        var hashBytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hashBytes)[..16].ToLowerInvariant();
    }
    
    private static TableRow CreateInfoRow(string label, string value)
    {
        var row = new TableRow();
        
        var labelCell = new TableCell();
        labelCell.AppendChild(new TableCellProperties(
            new TableCellWidth { Width = "1500", Type = TableWidthUnitValues.Pct },
            new Shading { Val = ShadingPatternValues.Clear, Fill = "F5F5F5" }
        ));
        labelCell.AppendChild(CreateParagraphForCell(label, true, JustificationValues.Left, 10));
        row.AppendChild(labelCell);
        
        var valueCell = new TableCell();
        valueCell.AppendChild(new TableCellProperties(new TableCellWidth { Width = "3500", Type = TableWidthUnitValues.Pct }));
        valueCell.AppendChild(CreateParagraphForCell(value, false, JustificationValues.Left, 10));
        row.AppendChild(valueCell);
        
        return row;
    }
    
    private static Paragraph CreateImageParagraph(string relationshipId, long width, long height)
    {
        var element = new Drawing(
            new DW.Inline(
                new DW.Extent { Cx = width, Cy = height },
                new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                new DW.DocProperties { Id = 1U, Name = "QR Code" },
                new DW.NonVisualGraphicFrameDrawingProperties(
                    new A.GraphicFrameLocks { NoChangeAspect = true }),
                new A.Graphic(
                    new A.GraphicData(
                        new PIC.Picture(
                            new PIC.NonVisualPictureProperties(
                                new PIC.NonVisualDrawingProperties { Id = 0U, Name = "qr.png" },
                                new PIC.NonVisualPictureDrawingProperties()),
                            new PIC.BlipFill(
                                new A.Blip { Embed = relationshipId },
                                new A.Stretch(new A.FillRectangle())),
                            new PIC.ShapeProperties(
                                new A.Transform2D(
                                    new A.Offset { X = 0L, Y = 0L },
                                    new A.Extents { Cx = width, Cy = height }),
                                new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })))
                    { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
            ) { DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U });

        return new Paragraph(new Run(element));
    }

    private static Paragraph CreateParagraph(string text, bool bold, int fontSize, JustificationValues? justification = null)
    {
        var run = new Run();
        var runProperties = new RunProperties();
        runProperties.AppendChild(new FontSize { Val = (fontSize * 2).ToString() });
        if (bold) runProperties.AppendChild(new Bold());
        run.AppendChild(runProperties);
        run.AppendChild(new Text(text));

        var paragraph = new Paragraph();
        if (justification.HasValue)
        {
            paragraph.AppendChild(new ParagraphProperties(new Justification { Val = justification.Value }));
        }
        paragraph.AppendChild(run);
        return paragraph;
    }

    private static Paragraph CreateParagraphForCell(string text, bool bold, JustificationValues? justification = null, int fontSize = 10)
    {
        var run = new Run();
        var runProperties = new RunProperties();
        runProperties.AppendChild(new FontSize { Val = (fontSize * 2).ToString() });
        if (bold) runProperties.AppendChild(new Bold());
        run.AppendChild(runProperties);
        run.AppendChild(new Text(text));

        var paragraph = new Paragraph();
        paragraph.AppendChild(new ParagraphProperties(new Justification { Val = justification ?? JustificationValues.Left }));
        paragraph.AppendChild(run);
        return paragraph;
    }

    private static TableCell CreateTableCell(string text, bool bold, string width, bool isHeader = false)
    {
        var cell = new TableCell();
        var cellProps = new TableCellProperties(
            new TableCellWidth { Width = width, Type = TableWidthUnitValues.Dxa },
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
        );
        if (isHeader)
        {
            cellProps.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Fill = "E8E8E8" });
        }
        cell.AppendChild(cellProps);

        var run = new Run();
        var runProperties = new RunProperties();
        runProperties.AppendChild(new FontSize { Val = "18" });
        if (bold) runProperties.AppendChild(new Bold());
        run.AppendChild(runProperties);
        run.AppendChild(new Text(text));

        var paragraph = new Paragraph();
        if (isHeader)
        {
            paragraph.AppendChild(new ParagraphProperties(new Justification { Val = JustificationValues.Center }));
        }
        paragraph.AppendChild(run);
        cell.AppendChild(paragraph);
        return cell;
    }

    private async Task<Issue> GetDraftIssueAsync(Guid id, CancellationToken cancellationToken)
    {
        var issue = await GetIssueAsync(id, cancellationToken);

        if (issue.Status != IssueStatus.Draft)
        {
            throw new ValidationException("Only Draft Issue can be changed.");
        }

        return issue;
    }

    private async Task<Issue> GetIssueAsync(Guid id, CancellationToken cancellationToken)
    {
        return await warehouse.GetIssueByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Issue was not found.");
    }

    private static IssueResponse ToResponse(Issue issue)
    {
        var toWarehouseName = issue.ToWarehouse?.Name;
        if (issue.ToWarehouse?.Type == WarehouseType.Sub)
        {
            var crewName = issue.WorkOrder?.Crew?.Name ?? issue.WorkOrder?.SubcontractorCrew?.Name;
            toWarehouseName = crewName != null ? $"Magazyn brygady: {crewName}" : "Magazyn brygady";
        }

        var createdByName = issue.CreatedBy != null 
            ? $"{issue.CreatedBy.FirstName} {issue.CreatedBy.LastName}" 
            : null;
        
        return new IssueResponse(
            issue.Id,
            issue.Number,
            issue.WorkOrderId,
            issue.WorkOrder?.Number ?? string.Empty,
            issue.FromWarehouseId,
            issue.ToWarehouseId,
            toWarehouseName,
            issue.CreatedById,
            createdByName,
            issue.CreatedAt,
            issue.ConfirmedAt,
            issue.Status,
            issue.ReceivedByName,
            issue.VerificationCode,
            issue.Items.Select(item => new IssueItemResponse(
                item.Id,
                item.IssueId,
                item.MaterialId,
                item.Material?.Name,
                item.Material?.Unit,
                item.Quantity)).ToList());
    }
}
