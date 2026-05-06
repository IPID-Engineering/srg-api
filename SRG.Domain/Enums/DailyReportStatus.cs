namespace SRG.Domain.Enums;

public enum DailyReportStatus
{
    Draft = 0,
    Submitted = 1,
    PmReview = 2,
    PmApproved = 3,
    SpmReview = 4,
    SpmApproved = 5,
    Rejected = 6,
    SubcontractorReview = 7,
    SubcontractorApproved = 8
}

public enum DailyReportCommentSection
{
    General = 0,
    WorkHours = 1,
    WorkEntries = 2,
    MaterialUsages = 3
}
