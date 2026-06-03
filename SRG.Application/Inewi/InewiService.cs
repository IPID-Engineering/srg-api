using SRG.Application.Persistence;
using SRG.Domain.Entities;

namespace SRG.Application.Inewi;

public interface IInewiService
{
    Task<List<InewiRecordResponse>> GetBySubcontractorAsync(Guid subcontractorId, CancellationToken cancellationToken = default);
    Task<List<InewiRecordResponse>> GetByDateRangeAsync(Guid subcontractorId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<ImportInewiResult> ImportAsync(Guid subcontractorId, Guid importedById, List<ImportInewiRecord> records, string? sourceFileName, CancellationToken cancellationToken = default);
}

public class InewiService(IInewiRepository inewiRepository) : IInewiService
{
    public async Task<List<InewiRecordResponse>> GetBySubcontractorAsync(Guid subcontractorId, CancellationToken cancellationToken = default)
    {
        var records = await inewiRepository.GetBySubcontractorAsync(subcontractorId, cancellationToken);
        return records.Select(ToResponse).ToList();
    }

    public async Task<List<InewiRecordResponse>> GetByDateRangeAsync(Guid subcontractorId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var records = await inewiRepository.GetByDateRangeAsync(subcontractorId, from, to, cancellationToken);
        return records.Select(ToResponse).ToList();
    }

    public async Task<ImportInewiResult> ImportAsync(Guid subcontractorId, Guid importedById, List<ImportInewiRecord> records, string? sourceFileName, CancellationToken cancellationToken = default)
    {
        var imported = 0;
        var updated = 0;
        var now = DateTime.UtcNow;

        foreach (var record in records)
        {
            var existing = await inewiRepository.GetByWorkerAndDateAsync(subcontractorId, record.WorkerName, record.Date, cancellationToken);

            if (existing != null)
            {
                if (existing.Hours != record.Hours)
                {
                    existing.Hours = record.Hours;
                    existing.SourceFileName = sourceFileName;
                    existing.ImportedAt = now;
                    existing.ImportedById = importedById;
                    updated++;
                }
            }
            else
            {
                var newRecord = new InewiRecord
                {
                    SubcontractorId = subcontractorId,
                    WorkerName = record.WorkerName,
                    Date = record.Date,
                    Hours = record.Hours,
                    SourceFileName = sourceFileName,
                    ImportedAt = now,
                    ImportedById = importedById
                };
                await inewiRepository.AddAsync(newRecord, cancellationToken);
                imported++;
            }
        }

        await inewiRepository.SaveChangesAsync(cancellationToken);

        return new ImportInewiResult(imported, updated, records.Count);
    }

    private static InewiRecordResponse ToResponse(InewiRecord record)
    {
        return new InewiRecordResponse(
            record.Id,
            record.SubcontractorId,
            record.WorkerName,
            record.Date,
            record.Hours,
            record.SourceFileName,
            record.ImportedAt);
    }
}

public record InewiRecordResponse(
    Guid Id,
    Guid SubcontractorId,
    string WorkerName,
    DateOnly Date,
    decimal Hours,
    string? SourceFileName,
    DateTime ImportedAt);

public record ImportInewiRecord(string WorkerName, DateOnly Date, decimal Hours);

public record ImportInewiRequest(List<ImportInewiRecord> Records, string? SourceFileName);

public record ImportInewiResult(int Imported, int Updated, int Total);
