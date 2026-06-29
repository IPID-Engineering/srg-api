using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SRG.Application.Inewi;

namespace SRG.Infrastructure.Inewi;

public class InewiApiClient : IInewiApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InewiApiClient> _logger;
    private const string BaseUrl = "https://inewi.pl/inewi/";

    public InewiApiClient(HttpClient httpClient, ILogger<InewiApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<InewiTokenApiResponse> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new InewiLoginRequest(email, password);
            var response = await _httpClient.PostAsJsonAsync("connect/token", request, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Inewi auth response: {StatusCode} - {Content}", response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Inewi authentication failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                throw new InewiApiException($"Błąd autentykacji: {response.StatusCode}");
            }

            var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<InewiTokenResponse>(responseContent);
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                _logger.LogWarning("Invalid token response from inewi: {Content}", responseContent);
                throw new InewiApiException("Nieprawidłowa odpowiedź z serwera inewi");
            }

            return new InewiTokenApiResponse(tokenResponse.AccessToken, tokenResponse.RedirectToAccountType);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error when connecting to inewi API");
            throw new InewiApiException("Nie udało się połączyć z serwerem inewi", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout when connecting to inewi API");
            throw new InewiApiException("Przekroczono limit czasu połączenia z serwerem inewi", ex);
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error from inewi API");
            throw new InewiApiException("Nieprawidłowy format odpowiedzi z serwera inewi", ex);
        }
    }

    public async Task<List<InewiWorkerApiRecord>> GetWorkersDataAsync(string accessToken, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/worktime/workers?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new InewiTokenExpiredException("Token wygasł - wymagana ponowna autentykacja");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Inewi API call failed: {StatusCode} - {Content}", response.StatusCode, errorContent);
                throw new InewiApiException($"Błąd pobierania danych: {response.StatusCode}");
            }

            var data = await response.Content.ReadFromJsonAsync<List<InewiWorkerRecord>>(cancellationToken);
            return data?.Select(r => new InewiWorkerApiRecord(r.WorkerId, r.WorkerName, r.Date, r.Hours)).ToList() ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error when fetching data from inewi API");
            throw new InewiApiException("Nie udało się pobrać danych z serwera inewi", ex);
        }
    }

    public async Task<InewiOrganizationSession> GetOrganizationSessionAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "session/organizationSession");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new InewiTokenExpiredException("Token wygasł - wymagana ponowna autentykacja");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Inewi organization session call failed: {StatusCode} - {Content}", response.StatusCode, errorContent);
                throw new InewiApiException($"Błąd pobierania danych organizacji: {response.StatusCode}");
            }

            var data = await response.Content.ReadFromJsonAsync<InewiOrganizationSessionResponse>(cancellationToken);
            if (data == null)
            {
                throw new InewiApiException("Nieprawidłowa odpowiedź z serwera inewi");
            }

            var employees = data.Employees
                .Where(e => e.IsActive)
                .Select(e => new InewiEmployeeApiRecord(
                    e.Id,
                    e.Name.FirstName,
                    e.Name.Surname,
                    e.Email,
                    e.IsActive,
                    e.TagsIds,
                    e.CustomEmployeeId
                ))
                .ToList();

            var tags = data.Tags
                .Select(t => new InewiTagApiRecord(t.Id, t.Name))
                .ToList();

            var workPositions = (data.WorkPositions ?? [])
                .Where(p => p.IsActive)
                .Select(p => new InewiWorkPositionApiRecord(p.Id, p.Name))
                .ToList();

            return new InewiOrganizationSession(data.Details.Name, employees, tags, workPositions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error when fetching organization session from inewi API");
            throw new InewiApiException("Nie udało się pobrać danych organizacji z serwera inewi", ex);
        }
    }

    public async Task<InewiExportResult> ExportDataAsync(string accessToken, List<string> peopleIds, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        try
        {
            // Convert dates to unix timestamps (midnight UTC)
            var fromUnix = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).ToUnixTimeSeconds();
            var toUnix = new DateTimeOffset(to.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).ToUnixTimeSeconds();

            var exportRequest = new InewiExportRequest(
                ExportType: 0,
                ExportDataType: 7,
                PeopleIds: peopleIds,
                SelectedColumns: ["0_6"],
                DatesUnix: [fromUnix, toUnix],
                GroupByMonth: false,
                UseBalancedReports: true
            );

            var request = new HttpRequestMessage(HttpMethod.Post, "export/export");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = JsonContent.Create(exportRequest);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new InewiTokenExpiredException("Token wygasł - wymagana ponowna autentykacja");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Inewi export call failed: {StatusCode} - {Content}", response.StatusCode, errorContent);
                throw new InewiApiException($"Błąd eksportu danych: {response.StatusCode}");
            }

            var exportResponse = await response.Content.ReadFromJsonAsync<InewiExportResponse>(cancellationToken);
            if (exportResponse == null || string.IsNullOrEmpty(exportResponse.Url))
            {
                throw new InewiApiException("Nieprawidłowa odpowiedź eksportu z serwera inewi");
            }

            return new InewiExportResult(exportResponse.Url);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error when exporting data from inewi API");
            throw new InewiApiException("Nie udało się wyeksportować danych z serwera inewi", ex);
        }
    }

    public async Task<byte[]> DownloadExportFileAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            using var downloadClient = new HttpClient();
            downloadClient.Timeout = TimeSpan.FromMinutes(2);
            return await downloadClient.GetByteArrayAsync(url, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error when downloading export file");
            throw new InewiApiException("Nie udało się pobrać pliku eksportu", ex);
        }
    }

    public async Task<InewiPrintQrResult> PrintQrCodesAsync(string accessToken, List<string> employeeIds, InewiPrintQrOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            var printQrRequest = new InewiPrintQrApiRequest(
                EmployeesIds: employeeIds,
                ShowFirstName: options.ShowFirstName,
                ShowLastName: options.ShowLastName,
                ShowTags: options.ShowTags,
                ShowPhoto: options.ShowPhoto
            );

            var request = new HttpRequestMessage(HttpMethod.Post, "employee/printQr");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = JsonContent.Create(printQrRequest);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new InewiTokenExpiredException("Token wygasł - wymagana ponowna autentykacja");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Inewi print QR call failed: {StatusCode} - {Content}", response.StatusCode, errorContent);
                throw new InewiApiException($"Błąd generowania kodów QR: {response.StatusCode}");
            }

            var printQrResponse = await response.Content.ReadFromJsonAsync<InewiPrintQrApiResponse>(cancellationToken);
            if (printQrResponse == null || string.IsNullOrEmpty(printQrResponse.Url))
            {
                throw new InewiApiException("Nieprawidłowa odpowiedź z serwera inewi");
            }

            return new InewiPrintQrResult(printQrResponse.Url);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error when generating QR codes from inewi API");
            throw new InewiApiException("Nie udało się wygenerować kodów QR z serwera inewi", ex);
        }
    }

    public async Task<InewiWorkPositionApiRecord?> CreateWorkPositionAsync(string accessToken, string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var createRequest = new InewiCreateWorkPositionRequest(name);
            var request = new HttpRequestMessage(HttpMethod.Post, "workPosition");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = JsonContent.Create(createRequest);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new InewiTokenExpiredException("Token wygasł - wymagana ponowna autentykacja");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Inewi create work position response: {StatusCode} - {Content}", response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                _logger.LogWarning("Inewi create work position failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                throw new InewiApiException($"Błąd tworzenia stanowiska w inewi: {response.StatusCode}");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent || string.IsNullOrWhiteSpace(responseContent))
            {
                _logger.LogInformation("Work position '{Name}' created successfully (NoContent response)", name);
                return null;
            }

            var position = System.Text.Json.JsonSerializer.Deserialize<InewiWorkPositionResponse>(responseContent);
            if (position == null)
            {
                return null;
            }

            return new InewiWorkPositionApiRecord(position.Id, position.Name);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error when creating work position in inewi API");
            throw new InewiApiException("Nie udało się utworzyć stanowiska w inewi", ex);
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error from inewi API (create work position)");
            throw new InewiApiException("Nieprawidłowy format odpowiedzi z serwera inewi", ex);
        }
    }

    public async Task DeleteWorkPositionsAsync(string accessToken, List<string> ids, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, "workPosition");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = JsonContent.Create(ids);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new InewiTokenExpiredException("Token wygasł - wymagana ponowna autentykacja");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Inewi delete work positions response: {StatusCode} - {Content}", response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Inewi delete work positions failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                throw new InewiApiException($"Błąd usuwania stanowisk w inewi: {response.StatusCode}");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error when deleting work positions in inewi API");
            throw new InewiApiException("Nie udało się usunąć stanowisk z inewi", ex);
        }
    }

    public async Task<Application.Inewi.InewiDetailedReportResult> GetDetailedReportAsync(string accessToken, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        try
        {
            var fromUnix = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).ToUnixTimeSeconds();
            var toUnix = new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero).ToUnixTimeSeconds();

            var reportRequest = new InewiReportRequest(fromUnix, toUnix);

            var request = new HttpRequestMessage(HttpMethod.Post, "report");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = JsonContent.Create(reportRequest);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new InewiTokenExpiredException("Token wygasł - wymagana ponowna autentykacja");
            }

            // Log raw response for debugging
            var rawContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Inewi report raw response ({StatusCode}): {ContentLength} chars, from={From} to={To}", 
                response.StatusCode, rawContent.Length, from, to);
            
            // Log first 2000 chars of response for debugging
            if (rawContent.Length > 0)
            {
                var preview = rawContent.Length > 2000 ? rawContent.Substring(0, 2000) + "..." : rawContent;
                _logger.LogInformation("Inewi report content preview: {Preview}", preview);
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Inewi report call failed: {StatusCode} - {Content}", response.StatusCode, rawContent);
                throw new InewiApiException($"Błąd pobierania raportu: {response.StatusCode}");
            }

            var reportResponse = System.Text.Json.JsonSerializer.Deserialize<InewiReportResponse>(rawContent);
            if (reportResponse == null)
            {
                throw new InewiApiException("Nieprawidłowa odpowiedź z serwera inewi");
            }
            
            _logger.LogInformation("Inewi report parsed: {EmployeeCount} employees, {DayCount} days", 
                reportResponse.EmployeesData?.Count ?? 0, reportResponse.Days?.Count ?? 0);

            // Build list of dates from response
            var dates = reportResponse.Days?
                .Select(d => DateTimeOffset.FromUnixTimeSeconds(d.DateUnix).Date)
                .Select(d => DateOnly.FromDateTime(d))
                .ToList() ?? [];

            // Parse employee data with clock logs
            var employeeReports = new List<Application.Inewi.InewiEmployeeDetailedReport>();
            
            foreach (var empData in reportResponse.EmployeesData ?? [])
            {
                var dailyReports = new List<Application.Inewi.InewiDailyDetailedReport>();
                
                if (empData.Days != null)
                {
                    for (var i = 0; i < empData.Days.Count && i < dates.Count; i++)
                    {
                        var dayReport = empData.Days[i];
                        var date = dates[i];
                        var clockEvents = new List<Application.Inewi.InewiClockEvent>();
                        
                        if (dayReport.DayData?.ClockLog != null)
                        {
                            foreach (var log in dayReport.DayData.ClockLog.OrderBy(l => l.TimeUtc))
                            {
                                var eventTime = DateTimeOffset.FromUnixTimeSeconds(log.TimeUtc);
                                clockEvents.Add(new Application.Inewi.InewiClockEvent(
                                    eventTime.UtcDateTime,
                                    log.IsEnd ? Application.Inewi.InewiClockEventType.End : Application.Inewi.InewiClockEventType.Start
                                ));
                            }
                        }
                        
                        var workTimeMinutes = dayReport.DayData?.WorkTime;
                        var breakTimeMinutes = dayReport.DayData?.BreakTime;
                        
                        dailyReports.Add(new Application.Inewi.InewiDailyDetailedReport(
                            date,
                            clockEvents,
                            workTimeMinutes,
                            breakTimeMinutes
                        ));
                    }
                }
                
                employeeReports.Add(new Application.Inewi.InewiEmployeeDetailedReport(empData.EmployeeId, dailyReports));
            }

            return new Application.Inewi.InewiDetailedReportResult(employeeReports, dates);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error when fetching detailed report from inewi API");
            throw new InewiApiException("Nie udało się pobrać raportu z serwera inewi", ex);
        }
    }

    public async Task<string?> CreateEmployeeAsync(string accessToken, string firstName, string surname, string? email, string? workPositionId, string? customEmployeeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var createRequest = new InewiCreateEmployeeRequest(
                new InewiEmployeeInfoRequest(string.IsNullOrWhiteSpace(email) ? null : email, firstName, surname),
                [],
                [],
                "4",
                workPositionId,
                customEmployeeId != null ? new InewiAdvancedEmployeeInfoRequest(customEmployeeId) : null
            );
            
            var jsonOptions = new System.Text.Json.JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            var jsonContent = System.Text.Json.JsonSerializer.Serialize(createRequest, jsonOptions);
            _logger.LogInformation("Creating employee in inewi with payload: {Payload}", jsonContent);
            
            var request = new HttpRequestMessage(HttpMethod.Post, "employeeManagement");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            
            var formContent = new MultipartFormDataContent();
            formContent.Add(new StringContent(jsonContent), "request");
            request.Content = formContent;

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new InewiTokenExpiredException("Token wygasł - wymagana ponowna autentykacja");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Inewi create employee response: {StatusCode} - {Content}", response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                _logger.LogWarning("Inewi create employee failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                throw new InewiApiException($"Błąd tworzenia pracownika w inewi: {response.StatusCode}");
            }

            if (!string.IsNullOrWhiteSpace(responseContent))
            {
                var cleanedResponse = responseContent.Trim('"');
                if (Guid.TryParse(cleanedResponse, out _))
                {
                    return cleanedResponse;
                }
                
                try
                {
                    var result = System.Text.Json.JsonSerializer.Deserialize<InewiCreateEmployeeResponse>(responseContent);
                    return result?.Id;
                }
                catch
                {
                    return cleanedResponse;
                }
            }

            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error when creating employee in inewi API");
            throw new InewiApiException("Nie udało się utworzyć pracownika w inewi", ex);
        }
    }
}

internal record InewiLoginRequest(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("password")] string Password
);

internal record InewiTokenResponse(
    [property: JsonPropertyName("accessToken")] string AccessToken,
    [property: JsonPropertyName("redirectToAccountType")] bool RedirectToAccountType
);

internal record InewiWorkerRecord(
    [property: JsonPropertyName("workerId")] string WorkerId,
    [property: JsonPropertyName("workerName")] string WorkerName,
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("hours")] decimal Hours
);

internal record InewiOrganizationSessionResponse(
    [property: JsonPropertyName("details")] InewiOrganizationDetails Details,
    [property: JsonPropertyName("employees")] List<InewiEmployeeResponse> Employees,
    [property: JsonPropertyName("tags")] List<InewiTagResponse> Tags,
    [property: JsonPropertyName("workPositions")] List<InewiWorkPositionResponse> WorkPositions
);

internal record InewiOrganizationDetails(
    [property: JsonPropertyName("name")] string Name
);

internal record InewiEmployeeResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] InewiEmployeeName Name,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("tagsIds")] List<string> TagsIds,
    [property: JsonPropertyName("customEmployeeId")] string? CustomEmployeeId
);

internal record InewiEmployeeName(
    [property: JsonPropertyName("firstName")] string FirstName,
    [property: JsonPropertyName("surname")] string Surname
);

internal record InewiTagResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name
);

internal record InewiExportRequest(
    [property: JsonPropertyName("exportType")] int ExportType,
    [property: JsonPropertyName("exportDataType")] int ExportDataType,
    [property: JsonPropertyName("peopleIds")] List<string> PeopleIds,
    [property: JsonPropertyName("selectedColumns")] List<string> SelectedColumns,
    [property: JsonPropertyName("datesUnix")] List<long> DatesUnix,
    [property: JsonPropertyName("groupByMonth")] bool GroupByMonth,
    [property: JsonPropertyName("useBalancedReports")] bool UseBalancedReports
);

internal record InewiExportResponse(
    [property: JsonPropertyName("url")] string Url
);

internal record InewiWorkPositionResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("isActive")] bool IsActive
);

internal record InewiCreateWorkPositionRequest(
    [property: JsonPropertyName("name")] string Name
);

internal record InewiCreateEmployeeRequest(
    [property: JsonPropertyName("employeeInfo")] InewiEmployeeInfoRequest EmployeeInfo,
    [property: JsonPropertyName("tagsIds")] List<string> TagsIds,
    [property: JsonPropertyName("additionalWorkPositions")] List<string> AdditionalWorkPositions,
    [property: JsonPropertyName("roleId")] string RoleId,
    [property: JsonPropertyName("defaultWorkPosition")] string? DefaultWorkPosition,
    [property: JsonPropertyName("advancedEmployeeInfo")] InewiAdvancedEmployeeInfoRequest? AdvancedEmployeeInfo
);

internal record InewiEmployeeInfoRequest(
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("firstName")] string FirstName,
    [property: JsonPropertyName("surname")] string Surname
);

internal record InewiAdvancedEmployeeInfoRequest(
    [property: JsonPropertyName("customEmployeeId")] string CustomEmployeeId
);

internal record InewiCreateEmployeeResponse(
    [property: JsonPropertyName("id")] string? Id
);

internal record InewiPrintQrApiRequest(
    [property: JsonPropertyName("employeesIds")] List<string> EmployeesIds,
    [property: JsonPropertyName("showFirstName")] bool ShowFirstName,
    [property: JsonPropertyName("showLastName")] bool ShowLastName,
    [property: JsonPropertyName("showTags")] bool ShowTags,
    [property: JsonPropertyName("showPhoto")] bool ShowPhoto
);

internal record InewiPrintQrApiResponse(
    [property: JsonPropertyName("url")] string Url
);

// Detailed Report DTOs
internal record InewiReportRequest(
    [property: JsonPropertyName("startDateUnix")] long StartDateUnix,
    [property: JsonPropertyName("endDateUnix")] long EndDateUnix,
    [property: JsonPropertyName("showFullPayPeriods")] bool ShowFullPayPeriods = false
);

internal record InewiReportResponse(
    [property: JsonPropertyName("employeesData")] List<InewiEmployeeReportData> EmployeesData,
    [property: JsonPropertyName("days")] List<InewiReportDay>? Days
);

internal record InewiEmployeeReportData(
    [property: JsonPropertyName("employeeId")] string EmployeeId,
    [property: JsonPropertyName("days")] List<InewiEmployeeDayReport>? Days
);

internal record InewiEmployeeDayReport(
    [property: JsonPropertyName("dayData")] InewiDayData? DayData
);

internal record InewiDayData(
    [property: JsonPropertyName("clockLog")] List<InewiClockLogEntry>? ClockLog,
    [property: JsonPropertyName("workTime")] decimal? WorkTime,
    [property: JsonPropertyName("breakTime")] decimal? BreakTime
);

internal record InewiClockLogEntry(
    [property: JsonPropertyName("timeUtc")] long TimeUtc,
    [property: JsonPropertyName("isEnd")] bool IsEnd
);

internal record InewiReportDay(
    [property: JsonPropertyName("dateUnix")] long DateUnix
);

public class InewiApiException : Exception
{
    public InewiApiException(string message) : base(message) { }
    public InewiApiException(string message, Exception innerException) : base(message, innerException) { }
}
