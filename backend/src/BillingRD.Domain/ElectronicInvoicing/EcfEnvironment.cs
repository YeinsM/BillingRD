namespace BillingRD.Domain.ElectronicInvoicing;

/// <summary>
/// DGII electronic invoicing environments.
/// Production has fiscal validity; pre-certification and certification are non-production environments.
/// </summary>
public enum EcfEnvironment
{
    PreCertification = 1,
    Certification = 2,
    Production = 3
}
