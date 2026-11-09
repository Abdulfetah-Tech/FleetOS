namespace EthioFleet.Domain.Inspections;

public enum InspectionStatus
{
    InProgress = 1,
    Passed = 2,
    Failed = 3
}

public enum InspectionResult
{
    Pass = 1,
    Fail = 2,
    NotApplicable = 3
}
