namespace BillingRD.Domain.ElectronicInvoicing;

public sealed class EcfSequenceAllocation
{
    private EcfSequenceAllocation() { }

    private EcfSequenceAllocation(
        Guid id,
        Guid businessId,
        Guid draftId,
        Guid sequenceRangeId,
        EcfEnvironment environment,
        EcfType type,
        string eNcf,
        DateOnly? expirationDate)
    {
        Id = id;
        BusinessId = businessId;
        DraftId = draftId;
        SequenceRangeId = sequenceRangeId;
        Environment = environment;
        Type = type;
        ENcf = eNcf;
        ExpirationDate = expirationDate;
        AllocatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public Guid DraftId { get; private set; }
    public Guid SequenceRangeId { get; private set; }
    public EcfEnvironment Environment { get; private set; }
    public EcfType Type { get; private set; }
    public string ENcf { get; private set; } = string.Empty;
    public DateOnly? ExpirationDate { get; private set; }
    public DateTimeOffset AllocatedAtUtc { get; private set; }

    public static EcfSequenceAllocation Create(
        Guid businessId,
        Guid draftId,
        EcfSequenceRange range,
        string eNcf)
    {
        if (draftId == Guid.Empty)
            throw new ArgumentException("Draft id is required.", nameof(draftId));

        if (range.BusinessId != businessId)
            throw new InvalidOperationException("The sequence range belongs to another business.");

        if (string.IsNullOrWhiteSpace(eNcf))
            throw new ArgumentException("e-NCF is required.", nameof(eNcf));

        return new EcfSequenceAllocation(
            Guid.CreateVersion7(),
            businessId,
            draftId,
            range.Id,
            range.Environment,
            range.Type,
            eNcf,
            range.ExpirationDate);
    }
}
