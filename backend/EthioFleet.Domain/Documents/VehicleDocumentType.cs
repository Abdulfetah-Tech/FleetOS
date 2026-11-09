namespace EthioFleet.Domain.Documents;

public enum VehicleDocumentType
{
    Insurance = 1,
    Registration = 2,
    InspectionCertificate = 3,
    Permit = 4,
    Other = 5
}

public enum VehicleDocumentStatus
{
    Active = 1,
    Expired = 2,
    Archived = 3
}
