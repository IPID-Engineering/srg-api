namespace SRG.Domain.Enums;

public enum DailyReportStatus
{
    Draft = 0,
    Submitted = 1,
    PmReview = 2,
    PmApproved = 3,
    SpmReview = 4,
    SpmApproved = 5,
    Rejected = 6,              // Rejected by PM - foreman sees
    SubcontractorReview = 7,
    SubcontractorApproved = 8,
    SubcontractorRejected = 9  // Rejected by Subcontractor - PM sees and decides
}

public enum DailyReportCommentSection
{
    General = 0,
    WorkHours = 1,
    WorkEntries = 2,
    MaterialUsages = 3
}
