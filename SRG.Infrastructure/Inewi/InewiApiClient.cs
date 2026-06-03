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

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Inewi authentication failed: {StatusCode} - {Content}", response.StatusCode, errorContent);
                throw new InewiApiException($"Błąd autentykacji: {response.StatusCode}");
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<InewiTokenResponse>(cancellationToken);
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
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
                    e.TagsIds
                ))
                .ToList();

            var tags = data.Tags
                .Select(t => new InewiTagApiRecord(t.Id, t.Name))
                .ToList();

            return new InewiOrganizationSession(data.Details.Name, employees, tags);
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
    [property: JsonPropertyName("tags")] List<InewiTagResponse> Tags
);

internal record InewiOrganizationDetails(
    [property: JsonPropertyName("name")] string Name
);

internal record InewiEmployeeResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] InewiEmployeeName Name,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("tagsIds")] List<string> TagsIds
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

public class InewiApiException : Exception
{
    public InewiApiException(string message) : base(message) { }
    public InewiApiException(string message, Exception innerException) : base(message, innerException) { }
}
