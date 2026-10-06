namespace BillingRD.Domain.ElectronicInvoicing;

/// <summary>
/// DGII e-CF type codes verified against official documentation.
/// Presence here does not mean the type is implemented for issuance.
/// </summary>
public enum EcfType
{
    CreditFiscalInvoice31 = 31,
    ConsumerInvoice32 = 32,
    DebitNote33 = 33,
    CreditNote34 = 34,
    Purchases41 = 41,
    MinorExpenses43 = 43,
    SpecialRegimes44 = 44,
    Government45 = 45,
    Exports46 = 46,
    ForeignPayments47 = 47
}
