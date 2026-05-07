using Microsoft.EntityFrameworkCore;
using SRG.Domain.Entities;
using SRG.Infrastructure.Persistence;

namespace SRG.Api.Extensions;

public static class SeedWorkTypesExtensions
{
    private static readonly (string Code, string Name, string Unit)[] DefaultWorkTypes =
    [
        ("BFR37", "brak frontu robót", "min"),
        ("DI40", "demontaże instalacji", "mb/szt"),
        ("KA27", "konfiguracja / adresowanie", "szt"),
        ("KZIB36", "kolizje z innymi branżami", "min"),
        ("MCB22", "montaż czujników BMS", "szt"),
        ("MDK04", "montaż drabinek kablowych", "mb"),
        ("MDSS18", "montaż dużych szaf sterowniczych / rozdzielnic", "szt"),
        ("MKKC03", "montaż koryt kablowych ciężkich (>300 mm)", "mb"),
        ("MKKL01", "montaż koryt kablowych lekkich (≤150 mm)", "mb"),
        ("MKKŚ02", "montaż koryt kablowych średnich (150–300 mm)", "mb"),
        ("MMSS16", "montaż małych szafek sterowniczych (np. natynkowe, do ~50 elementów)", "szt"),
        ("MRIP05", "montaż rur instalacyjnych (PVC)", "mb"),
        ("MRIS06", "montaż rur instalacyjnych (stalowych)", "mb"),
        ("MS23", "montaż siłowników", "szt"),
        ("MS24", "montaż sterowników", "szt"),
        ("MUW07", "montaż uchwytów / wsporników", "szt"),
        ("MŚSS17", "montaż średnich szaf sterowniczych", "szt"),
        ("OK14", "oznaczanie kabli", "szt"),
        ("ONDP34", "oczekiwanie na decyzję / projekt", "min"),
        ("ONM33", "oczekiwanie na materiał", "min"),
        ("PI41", "przeróbki instalacji", "mb/szt"),
        ("PKDS19", "podłączenie kabli do szafy (do 10 kabli)", "kpl"),
        ("PKDS20", "podłączenie kabli do szafy (10–30 kabli)", "kpl"),
        ("PKDS21", "podłączenie kabli do szafy (>30 kabli)", "kpl"),
        ("PKWR13", "przeciąganie kabli w rurach", "mb"),
        ("PO35", "przestoje organizacyjne", "min"),
        ("PPIE38", "poprawki po innych ekipach", "min"),
        ("PSP30", "przygotowanie stanowiska pracy", "min"),
        ("PW39", "poprawki własne", "min"),
        ("PWK15", "porządkowanie / wiązkowanie kabli", "mb"),
        ("RM29", "rozładunek materiału", "min/h"),
        ("SP31", "sprzątanie / porządkowanie", "min"),
        ("TMNB28", "transport materiału na budowie", "min/h"),
        ("TP26", "testy punktów", "szt"),
        ("UKDP12", "układanie kabli dużych przekrojów (>50 mm²)", "mb"),
        ("UKN09", "układanie kabli niskoprądowych", "mb"),
        ("UKZ511", "układanie kabli zasilających 5x16–5x50 mm²", "mb"),
        ("UKZD10", "układanie kabli zasilających do 5x10 mm²", "mb"),
        ("UPB25", "uruchomienie punktów BMS", "szt"),
        ("WIK08", "wiercenia i kotwienia", "szt"),
        ("ZB32", "zabezpieczenia BHP", "min"),
    ];

    public static async Task SeedWorkTypesAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existingCodes = await dbContext.WorkTypes
            .Select(wt => wt.Code)
            .ToListAsync();

        var workTypesToAdd = DefaultWorkTypes
            .Where(wt => !existingCodes.Contains(wt.Code))
            .Select(wt => new WorkType
            {
                Code = wt.Code,
                Name = wt.Name,
                Unit = wt.Unit,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            })
            .ToList();

        if (workTypesToAdd.Count > 0)
        {
            dbContext.WorkTypes.AddRange(workTypesToAdd);
            await dbContext.SaveChangesAsync();
        }
    }
}
