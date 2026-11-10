namespace EthioFleet.Domain.Incidents;

public enum IncidentType
{
    Accident = 1,
    Mechanical = 2,
    Traffic = 3,
    Theft = 4,
    Damage = 5,
    Other = 6
}

public enum IncidentSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum IncidentStatus
{
    Open = 1,
    UnderInvestigation = 2,
    Resolved = 3,
    Closed = 4
}
