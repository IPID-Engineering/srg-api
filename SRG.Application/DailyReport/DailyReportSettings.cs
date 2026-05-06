namespace SRG.Application.DailyReports;

public class DailyReportSettings
{
    public const string SectionName = "DailyReport";

    public int EditableDaysBack { get; set; } = 3;
}
