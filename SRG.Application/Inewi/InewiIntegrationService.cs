using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using SRG.Application.Persistence;
using SRG.Domain.Entities;

namespace SRG.Application.Inewi;

public interface IInewiIntegrationService
{
    Task<InewiIntegrationStatusResponse> GetIntegrationStatusAsync(Guid subcontractorId, CancellationToken cancellationToken = default);
    Task<InewiIntegrationStatusResponse> ConfigureIntegrationAsync(Guid subcontractorId, Guid userId, ConfigureInewiIntegrationRequest request, CancellationToken cancellationToken = default);
    Task<InewiSyncResult> SyncDataAsync(Guid subcontractorId, Guid userId, InewiSyncRequest request, CancellationToken cancellationToken = default);
    Task DisableIntegrationAsync(Guid subcontractorId, CancellationToken cancellationToken = default);
    Task<InewiEmployeesListResponse> GetInewiEmployeesAsync(Guid subcontractorId, CancellationToken cancellationToken = default);
    Task MapWorkerToInewiEmployeeAsync(Guid workerId, string? inewiEmployeeId, CancellationToken cancellationToken = default);
}

public class InewiIntegrationService(
    IInewiRepository inewiRepository,
    IConstructionRepository constructionRepository,
    IInewiApiClient inewiApiClient,
    ILogger<InewiIntegrationService> logger) : IInewiIntegrationService
{
    private static readonly byte[] EncryptionKey = Encoding.UTF8.GetBytes("SRG-Inewi-Key-32-chars-long-key!");
    
    public async Task<InewiIntegrationStatusResponse> GetIntegrationStatusAsync(Guid subcontractorId, CancellationToken cancellationToken = default)
    {
        var settings = await inewiRepository.GetIntegrationSettingsAsync(subcontractorId, cancellationToken);
        
        if (settings == null)
        {
            return new InewiIntegrationStatusResponse(
                IsConfigured: false,
                IsEnabled: false,
                Email: null,
                LastSyncAt: null,
                LastError: null,
                TokenExpiresAt: null
            );
        }

        return new InewiIntegrationStatusResponse(
            IsConfigured: true,
            IsEnabled: settings.IsEnabled,
            Email: settings.Email,
            LastSyncAt: settings.LastSyncAt,
            LastError: settings.LastError,
            TokenExpiresAt: settings.TokenExpiresAt
        );
    }

    public async Task<InewiIntegrationStatusResponse> ConfigureIntegrationAsync(
        Guid subcontractorId, 
        Guid userId,
        ConfigureInewiIntegrationRequest request, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ValidationException("Email jest wymagany.");
        }
        
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationException("Hasło jest wymagane.");
        }

        // Authenticate with inewi API to validate credentials
        var tokenResponse = await inewiApiClient.AuthenticateAsync(request.Email, request.Password, cancellationToken);
        
        // Calculate token expiration (24h from now based on typical JWT)
        var tokenExpiresAt = DateTime.UtcNow.AddHours(24);

        var existingSettings = await inewiRepository.GetIntegrationSettingsAsync(subcontractorId, cancellationToken);

        if (existingSettings != null)
        {
            existingSettings.Email = request.Email;
            existingSettings.EncryptedPassword = EncryptPassword(request.Password);
            existingSettings.AccessToken = tokenResponse.AccessToken;
            existingSettings.TokenExpiresAt = tokenExpiresAt;
            existingSettings.IsEnabled = true;
            existingSettings.LastError = null;
            existingSettings.UpdatedAt = DateTime.UtcNow;
            
            await inewiRepository.UpdateIntegrationSettingsAsync(existingSettings, cancellationToken);
        }
        else
        {
            var settings = new InewiIntegrationSettings
            {
                SubcontractorId = subcontractorId,
                Email = request.Email,
                EncryptedPassword = EncryptPassword(request.Password),
                AccessToken = tokenResponse.AccessToken,
                TokenExpiresAt = tokenExpiresAt,
                IsEnabled = true,
                ConfiguredById = userId
            };
            
            await inewiRepository.AddIntegrationSettingsAsync(settings, cancellationToken);
        }

        await inewiRepository.SaveChangesAsync(cancellationToken);

        return new InewiIntegrationStatusResponse(
            IsConfigured: true,
            IsEnabled: true,
            Email: request.Email,
            LastSyncAt: null,
            LastError: null,
            TokenExpiresAt: tokenExpiresAt
        );
    }

    public async Task<InewiSyncResult> SyncDataAsync(
        Guid subcontractorId, 
        Guid userId, 
        InewiSyncRequest request, 
        CancellationToken cancellationToken = default)
    {
        var settings = await inewiRepository.GetIntegrationSettingsAsync(subcontractorId, cancellationToken);
        
        if (settings == null)
        {
            throw new ValidationException("Integracja z inewi nie jest skonfigurowana.");
        }
        
        if (!settings.IsEnabled)
        {
            throw new ValidationException("Integracja z inewi jest wyłączona.");
        }

        // Get all workers for this subcontractor with mapped inewi IDs
        var workers = await constructionRepository.GetSubcontractorWorkersAsync(subcontractorId, cancellationToken);
        var mappedWorkers = workers.Where(w => !string.IsNullOrEmpty(w.InewiEmployeeId)).ToList();
        
        if (mappedWorkers.Count == 0)
        {
            throw new ValidationException("Brak pracowników z przypisanym ID inewi. Przypisz pracowników w ustawieniach integracji.");
        }

        var inewiEmployeeIds = mappedWorkers.Select(w => w.InewiEmployeeId!).ToList();
        
        // Build mapping: inewiEmployeeId -> our worker name
        var inewiIdToWorkerName = mappedWorkers.ToDictionary(
            w => w.InewiEmployeeId!, 
            w => $"{w.FirstName} {w.LastName}"
        );

        try
        {
            // Check if token is expired and refresh if needed
            var accessToken = settings.AccessToken;
            if (string.IsNullOrEmpty(accessToken) || settings.TokenExpiresAt < DateTime.UtcNow.AddMinutes(5))
            {
                accessToken = await RefreshTokenAsync(settings, cancellationToken);
            }

            // Get organization session to map inewi employee names to IDs
            var orgSession = await inewiApiClient.GetOrganizationSessionAsync(accessToken!, cancellationToken);
            
            // Build mapping: inewi employee full name (normalized) -> inewi employee ID
            var inewiNameToId = orgSession.Employees.ToDictionary(
                e => NormalizeName(e.FullName),
                e => e.Id,
                StringComparer.OrdinalIgnoreCase
            );

            // Get export URL from inewi
            logger.LogInformation("Calling inewi export for {Count} employees, from {From} to {To}", inewiEmployeeIds.Count, request.From, request.To);
            var exportResult = await inewiApiClient.ExportDataAsync(accessToken!, inewiEmployeeIds, request.From, request.To, cancellationToken);
            logger.LogInformation("Export URL received: {Url}", exportResult.Url);

            // Download the ZIP file
            var zipData = await inewiApiClient.DownloadExportFileAsync(exportResult.Url, cancellationToken);
            logger.LogInformation("Downloaded ZIP file, size: {Size} bytes", zipData.Length);

            // Parse the ZIP and extract records using both mappings
            var records = ParseExportZip(zipData, inewiNameToId, inewiIdToWorkerName, logger);
            logger.LogInformation("Parsed {Count} records from ZIP", records.Count);

            // Import records
            var imported = 0;
            var updated = 0;
            var now = DateTime.UtcNow;

            foreach (var record in records)
            {
                var existing = await inewiRepository.GetByWorkerAndDateAsync(
                    subcontractorId, 
                    record.WorkerName, 
                    record.Date, 
                    cancellationToken);

                if (existing != null)
                {
                    if (existing.Hours != record.Hours)
                    {
                        existing.Hours = record.Hours;
                        existing.SourceFileName = "inewi-export";
                        existing.ImportedAt = now;
                        existing.ImportedById = userId;
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
                        SourceFileName = "inewi-export",
                        ImportedAt = now,
                        ImportedById = userId
                    };
                    await inewiRepository.AddAsync(newRecord, cancellationToken);
                    imported++;
                }
            }

            // Update sync status
            settings.LastSyncAt = now;
            settings.LastError = null;
            await inewiRepository.UpdateIntegrationSettingsAsync(settings, cancellationToken);
            await inewiRepository.SaveChangesAsync(cancellationToken);

            return new InewiSyncResult(imported, updated, records.Count, null);
        }
        catch (InewiTokenExpiredException)
        {
            // Try to refresh token and retry once
            try
            {
                var newToken = await RefreshTokenAsync(settings, cancellationToken);
                return await SyncDataWithTokenAsync(subcontractorId, userId, newToken, inewiEmployeeIds, inewiIdToWorkerName, request, settings, cancellationToken);
            }
            catch (Exception retryEx)
            {
                settings.LastError = retryEx.Message;
                await inewiRepository.UpdateIntegrationSettingsAsync(settings, cancellationToken);
                await inewiRepository.SaveChangesAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            settings.LastError = ex.Message;
            await inewiRepository.UpdateIntegrationSettingsAsync(settings, cancellationToken);
            await inewiRepository.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private async Task<InewiSyncResult> SyncDataWithTokenAsync(
        Guid subcontractorId,
        Guid userId,
        string accessToken,
        List<string> inewiEmployeeIds,
        Dictionary<string, string> inewiIdToWorkerName,
        InewiSyncRequest request,
        InewiIntegrationSettings settings,
        CancellationToken cancellationToken)
    {
        // Get organization session to map inewi employee names to IDs
        var orgSession = await inewiApiClient.GetOrganizationSessionAsync(accessToken, cancellationToken);
        var inewiNameToId = orgSession.Employees.ToDictionary(
            e => NormalizeName(e.FullName),
            e => e.Id,
            StringComparer.OrdinalIgnoreCase
        );

        var exportResult = await inewiApiClient.ExportDataAsync(accessToken, inewiEmployeeIds, request.From, request.To, cancellationToken);
        var zipData = await inewiApiClient.DownloadExportFileAsync(exportResult.Url, cancellationToken);
        var records = ParseExportZip(zipData, inewiNameToId, inewiIdToWorkerName, logger);

        var imported = 0;
        var updated = 0;
        var now = DateTime.UtcNow;

        foreach (var record in records)
        {
            var existing = await inewiRepository.GetByWorkerAndDateAsync(
                subcontractorId, 
                record.WorkerName, 
                record.Date, 
                cancellationToken);

            if (existing != null)
            {
                if (existing.Hours != record.Hours)
                {
                    existing.Hours = record.Hours;
                    existing.SourceFileName = "inewi-export";
                    existing.ImportedAt = now;
                    existing.ImportedById = userId;
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
                    SourceFileName = "inewi-export",
                    ImportedAt = now,
                    ImportedById = userId
                };
                await inewiRepository.AddAsync(newRecord, cancellationToken);
                imported++;
            }
        }

        settings.LastSyncAt = now;
        settings.LastError = null;
        await inewiRepository.UpdateIntegrationSettingsAsync(settings, cancellationToken);
        await inewiRepository.SaveChangesAsync(cancellationToken);

        return new InewiSyncResult(imported, updated, records.Count, null);
    }

    private static List<ParsedInewiRecord> ParseExportZip(
        byte[] zipData, 
        Dictionary<string, string> inewiNameToId,
        Dictionary<string, string> inewiIdToWorkerName,
        ILogger logger)
    {
        var records = new List<ParsedInewiRecord>();

        using var zipStream = new MemoryStream(zipData);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        // Log all files in the ZIP
        logger.LogInformation("ZIP contains {Count} entries:", archive.Entries.Count);
        foreach (var e in archive.Entries)
        {
            logger.LogInformation("  - {Name} ({Size} bytes)", e.FullName, e.Length);
        }
        
        // Log mappings
        logger.LogInformation("inewiNameToId mappings ({Count}):", inewiNameToId.Count);
        foreach (var kvp in inewiNameToId)
        {
            logger.LogInformation("  - '{Name}' -> {Id}", kvp.Key, kvp.Value);
        }
        
        logger.LogInformation("inewiIdToWorkerName mappings ({Count}):", inewiIdToWorkerName.Count);
        foreach (var kvp in inewiIdToWorkerName)
        {
            logger.LogInformation("  - {Id} -> '{Name}'", kvp.Key, kvp.Value);
        }

        foreach (var entry in archive.Entries)
        {
            if (!entry.FullName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                continue;
            
            if (entry.FullName.StartsWith("__MACOSX", StringComparison.OrdinalIgnoreCase))
                continue;
            
            if (entry.FullName.Contains('/') && !entry.FullName.Contains(".csv"))
                continue;

            var fileName = Path.GetFileNameWithoutExtension(entry.FullName);
            logger.LogInformation("Processing file: {FileName}", fileName);
            
            string? workerName = null;
            
            // Try multiple filename patterns
            var match = System.Text.RegularExpressions.Regex.Match(
                fileName, 
                @"Ewidencja_czasu_pracy-[\d.]+-[\d.]+-(.+)$"
            );
            
            string? inewiEmployeeName = null;
            
            if (match.Success)
            {
                inewiEmployeeName = match.Groups[1].Value.Replace("_", " ").Trim();
                logger.LogInformation("  Extracted employee name from pattern: '{Name}'", inewiEmployeeName);
            }
            else
            {
                inewiEmployeeName = fileName.Replace("_", " ").Trim();
                logger.LogInformation("  Using filename as name: '{Name}'", inewiEmployeeName);
            }
            
            if (!string.IsNullOrEmpty(inewiEmployeeName))
            {
                var normalizedName = NormalizeName(inewiEmployeeName);
                logger.LogInformation("  Normalized name: '{Name}'", normalizedName);
                
                if (inewiNameToId.TryGetValue(normalizedName, out var inewiEmployeeId))
                {
                    logger.LogInformation("  Found exact match in inewiNameToId: {Id}", inewiEmployeeId);
                    if (inewiIdToWorkerName.TryGetValue(inewiEmployeeId, out var mappedWorkerName))
                    {
                        workerName = mappedWorkerName;
                        logger.LogInformation("  Mapped to worker: '{WorkerName}'", workerName);
                    }
                }
                
                if (workerName == null)
                {
                    logger.LogInformation("  Trying fuzzy matching...");
                    foreach (var kvp in inewiNameToId)
                    {
                        var inewiName = kvp.Key;
                        var inewiId = kvp.Value;
                        
                        if (NamesMatch(normalizedName, inewiName))
                        {
                            logger.LogInformation("  Fuzzy match found: '{InewiName}' -> {Id}", inewiName, inewiId);
                            if (inewiIdToWorkerName.TryGetValue(inewiId, out var mappedWorkerName))
                            {
                                workerName = mappedWorkerName;
                                logger.LogInformation("  Mapped to worker: '{WorkerName}'", workerName);
                                break;
                            }
                        }
                    }
                }
                
                if (workerName == null)
                {
                    logger.LogInformation("  Trying direct worker name matching...");
                    foreach (var kvp in inewiIdToWorkerName)
                    {
                        if (NamesMatch(normalizedName, NormalizeName(kvp.Value)))
                        {
                            workerName = kvp.Value;
                            logger.LogInformation("  Direct match found: '{WorkerName}'", workerName);
                            break;
                        }
                    }
                }
            }

            if (workerName == null)
            {
                logger.LogWarning("  SKIPPING file - no worker name match found for: {FileName}", fileName);
                continue;
            }

            logger.LogInformation("  Matched to worker: '{WorkerName}', reading CSV...", workerName);

            using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream, Encoding.UTF8);

            var lines = new List<string>();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (!string.IsNullOrWhiteSpace(line))
                    lines.Add(line);
            }
            
            logger.LogInformation("  CSV has {Count} lines (including header)", lines.Count);

            var parsedCount = 0;
            foreach (var line in lines.Skip(1))
            {
                var values = ParseCsvLine(line);
                if (values.Count < 2)
                    continue;

                var dateStr = values[0].Trim('"', ' ');
                
                if (dateStr.Equals("Data", StringComparison.OrdinalIgnoreCase) ||
                    dateStr.Equals("Suma", StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(dateStr))
                {
                    continue;
                }
                    
                if (!TryParseDate(dateStr, out var date))
                    continue;

                var hoursStr = values[1].Trim('"', ' ').Replace(",", ".");
                if (string.IsNullOrWhiteSpace(hoursStr))
                    continue;
                    
                if (!decimal.TryParse(hoursStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var hours))
                    continue;

                if (hours > 0)
                {
                    records.Add(new ParsedInewiRecord(workerName, date, hours));
                    parsedCount++;
                }
            }
            logger.LogInformation("  Parsed {Count} records from this file", parsedCount);
        }

        return records;
    }
    
    private static bool NamesMatch(string name1, string name2)
    {
        if (name1.Equals(name2, StringComparison.OrdinalIgnoreCase))
            return true;
        
        var parts1 = name1.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var parts2 = name2.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts1.Length == parts2.Length && parts1.Length > 0)
        {
            var sorted1 = parts1.OrderBy(p => p).ToArray();
            var sorted2 = parts2.OrderBy(p => p).ToArray();
            if (sorted1.SequenceEqual(sorted2))
                return true;
        }
        
        if (name1.Contains(name2, StringComparison.OrdinalIgnoreCase) ||
            name2.Contains(name1, StringComparison.OrdinalIgnoreCase))
            return true;
        
        return false;
    }
    
    private static string NormalizeName(string name)
    {
        return System.Text.RegularExpressions.Regex.Replace(name.Trim(), @"\s+", " ");
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var current = new StringBuilder();

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else if (c == ';' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());

        return result;
    }

    private static bool TryParseDate(string dateStr, out DateOnly date)
    {
        string[] formats = ["yyyy-MM-dd", "dd.MM.yyyy", "dd/MM/yyyy", "MM/dd/yyyy", "dd-MM-yyyy"];
        
        foreach (var format in formats)
        {
            if (DateOnly.TryParseExact(dateStr, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return true;
            }
        }

        date = default;
        return false;
    }

    private record ParsedInewiRecord(string WorkerName, DateOnly Date, decimal Hours);

    public async Task DisableIntegrationAsync(Guid subcontractorId, CancellationToken cancellationToken = default)
    {
        var settings = await inewiRepository.GetIntegrationSettingsAsync(subcontractorId, cancellationToken);
        
        if (settings == null)
        {
            return;
        }

        settings.IsEnabled = false;
        settings.AccessToken = null;
        settings.TokenExpiresAt = null;
        
        await inewiRepository.UpdateIntegrationSettingsAsync(settings, cancellationToken);
        await inewiRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<InewiEmployeesListResponse> GetInewiEmployeesAsync(Guid subcontractorId, CancellationToken cancellationToken = default)
    {
        var settings = await inewiRepository.GetIntegrationSettingsAsync(subcontractorId, cancellationToken);
        
        if (settings == null || !settings.IsEnabled)
        {
            throw new ValidationException("Integracja z inewi nie jest skonfigurowana lub wyłączona.");
        }

        // Refresh token if needed
        var accessToken = settings.AccessToken;
        if (string.IsNullOrEmpty(accessToken) || settings.TokenExpiresAt < DateTime.UtcNow.AddMinutes(5))
        {
            accessToken = await RefreshTokenAsync(settings, cancellationToken);
        }

        // Get organization session from inewi
        var session = await inewiApiClient.GetOrganizationSessionAsync(accessToken!, cancellationToken);

        // Cleanup: Clear INEWI mappings for orphaned workers (those without a crew)
        var orphanedCleared = await constructionRepository.ClearInewiMappingsForOrphanedWorkersAsync(subcontractorId, cancellationToken);
        if (orphanedCleared > 0)
        {
            logger.LogInformation("Cleared INEWI mappings for {Count} orphaned workers (no crew) for subcontractor {SubcontractorId}", 
                orphanedCleared, subcontractorId);
        }

        // Get all workers for this subcontractor (only those with a crew)
        var workers = await constructionRepository.GetSubcontractorWorkersAsync(subcontractorId, cancellationToken);
        
        // Build set of valid INEWI employee IDs from the session
        var validInewiEmployeeIds = session.Employees.Select(e => e.Id).ToHashSet();

        // Cleanup: Clear InewiEmployeeId for workers whose INEWI employee no longer exists
        var hasChanges = false;
        foreach (var worker in workers.Where(w => !string.IsNullOrEmpty(w.InewiEmployeeId)))
        {
            if (!validInewiEmployeeIds.Contains(worker.InewiEmployeeId!))
            {
                logger.LogInformation(
                    "Clearing invalid INEWI mapping for worker {WorkerId} ({WorkerName}): INEWI employee {InewiId} no longer exists",
                    worker.Id, $"{worker.FirstName} {worker.LastName}", worker.InewiEmployeeId);
                worker.InewiEmployeeId = null;
                hasChanges = true;
            }
        }

        // Auto-map workers by name matching
        foreach (var worker in workers.Where(w => string.IsNullOrEmpty(w.InewiEmployeeId)))
        {
            var workerFullName = NormalizeName($"{worker.FirstName} {worker.LastName}");
            
            var matchingEmployee = session.Employees.FirstOrDefault(e => 
                NormalizeName(e.FullName).Equals(workerFullName, StringComparison.OrdinalIgnoreCase));
            
            if (matchingEmployee == null)
            {
                var reversedName = NormalizeName($"{worker.LastName} {worker.FirstName}");
                matchingEmployee = session.Employees.FirstOrDefault(e => 
                    NormalizeName(e.FullName).Equals(reversedName, StringComparison.OrdinalIgnoreCase));
            }
            
            if (matchingEmployee != null)
            {
                var alreadyMapped = workers.Any(w => w.InewiEmployeeId == matchingEmployee.Id && w.Id != worker.Id);
                if (!alreadyMapped)
                {
                    worker.InewiEmployeeId = matchingEmployee.Id;
                    hasChanges = true;
                }
            }
        }
        
        if (hasChanges)
        {
            await constructionRepository.SaveChangesAsync(cancellationToken);
        }

        // Map workers with their inewi employee info
        var workerMappings = workers.Select(w => new InewiWorkerMapping(
            w.Id,
            $"{w.FirstName} {w.LastName}",
            w.InewiEmployeeId,
            session.Employees.FirstOrDefault(e => e.Id == w.InewiEmployeeId)?.FullName
        )).ToList();

        // Find mapped inewi IDs
        var mappedInewiIds = workers
            .Where(w => !string.IsNullOrEmpty(w.InewiEmployeeId))
            .Select(w => w.InewiEmployeeId)
            .ToHashSet();

        var inewiEmployees = session.Employees
            .Select(e => new InewiEmployeeInfo(
                e.Id, 
                e.FullName, 
                e.Email, 
                mappedInewiIds.Contains(e.Id),
                e.TagIds
            ))
            .ToList();

        return new InewiEmployeesListResponse(
            session.OrganizationName,
            inewiEmployees,
            workerMappings,
            session.Tags.Select(t => new InewiTagInfo(t.Id, t.Name)).ToList()
        );
    }

    public async Task MapWorkerToInewiEmployeeAsync(Guid workerId, string? inewiEmployeeId, CancellationToken cancellationToken = default)
    {
        var worker = await constructionRepository.GetSubcontractorWorkerByIdAsync(workerId, cancellationToken);
        if (worker == null)
        {
            throw new ValidationException("Nie znaleziono pracownika.");
        }

        worker.InewiEmployeeId = inewiEmployeeId;
        await constructionRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> RefreshTokenAsync(InewiIntegrationSettings settings, CancellationToken cancellationToken)
    {
        var password = DecryptPassword(settings.EncryptedPassword);
        var tokenResponse = await inewiApiClient.AuthenticateAsync(settings.Email, password, cancellationToken);
        
        settings.AccessToken = tokenResponse.AccessToken;
        settings.TokenExpiresAt = DateTime.UtcNow.AddHours(24);
        
        await inewiRepository.UpdateIntegrationSettingsAsync(settings, cancellationToken);
        await inewiRepository.SaveChangesAsync(cancellationToken);
        
        return tokenResponse.AccessToken;
    }

    private static string EncryptPassword(string password)
    {
        using var aes = Aes.Create();
        aes.Key = EncryptionKey;
        aes.GenerateIV();
        
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(password);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
        
        return Convert.ToBase64String(result);
    }

    private static string DecryptPassword(string encryptedPassword)
    {
        var fullCipher = Convert.FromBase64String(encryptedPassword);
        
        using var aes = Aes.Create();
        aes.Key = EncryptionKey;
        
        var iv = new byte[16];
        var cipher = new byte[fullCipher.Length - 16];
        
        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);
        
        aes.IV = iv;
        
        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        
        return Encoding.UTF8.GetString(plainBytes);
    }
}

public interface IInewiApiClient
{
    Task<InewiTokenApiResponse> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<List<InewiWorkerApiRecord>> GetWorkersDataAsync(string accessToken, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<InewiOrganizationSession> GetOrganizationSessionAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<InewiExportResult> ExportDataAsync(string accessToken, List<string> peopleIds, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<byte[]> DownloadExportFileAsync(string url, CancellationToken cancellationToken = default);
}

public record InewiTokenApiResponse(string AccessToken, bool RedirectToAccountType);
public record InewiWorkerApiRecord(string WorkerId, string WorkerName, string Date, decimal Hours);
public record InewiOrganizationSession(string OrganizationName, List<InewiEmployeeApiRecord> Employees, List<InewiTagApiRecord> Tags);
public record InewiExportResult(string Url);
public record InewiEmployeeApiRecord(string Id, string FirstName, string LastName, string? Email, bool IsActive, List<string> TagIds)
{
    public string FullName => $"{FirstName} {LastName}";
}
public record InewiTagApiRecord(string Id, string Name);

public record InewiIntegrationStatusResponse(
    bool IsConfigured,
    bool IsEnabled,
    string? Email,
    DateTime? LastSyncAt,
    string? LastError,
    DateTime? TokenExpiresAt
);

public record ConfigureInewiIntegrationRequest(string Email, string Password);
public record InewiSyncRequest(DateOnly From, DateOnly To);
public record InewiSyncResult(int Imported, int Updated, int Total, string? Error);

public record InewiEmployeesListResponse(
    string OrganizationName,
    List<InewiEmployeeInfo> InewiEmployees,
    List<InewiWorkerMapping> Workers,
    List<InewiTagInfo> Tags
);

public record InewiEmployeeInfo(string Id, string FullName, string? Email, bool IsMapped, List<string> TagIds);
public record InewiWorkerMapping(Guid WorkerId, string WorkerName, string? InewiEmployeeId, string? InewiEmployeeName);
public record InewiTagInfo(string Id, string Name);
public record MapWorkerToInewiRequest(string? InewiEmployeeId);

public class InewiTokenExpiredException : Exception
{
    public InewiTokenExpiredException(string message) : base(message) { }
}
