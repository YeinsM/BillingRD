namespace BillingRD.Domain.ElectronicInvoicing;

public sealed class EcfSequenceRange
{
    private EcfSequenceRange() { }

    private EcfSequenceRange(
        Guid id,
        Guid businessId,
        EcfEnvironment environment,
        EcfType type,
        long fromNumber,
        long toNumber,
        long nextNumber,
        DateOnly? expirationDate)
    {
        Id = id;
        BusinessId = businessId;
        Environment = environment;
        Type = type;
        FromNumber = fromNumber;
        ToNumber = toNumber;
        NextNumber = nextNumber;
        ExpirationDate = expirationDate;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public EcfEnvironment Environment { get; private set; }
    public EcfType Type { get; private set; }
    public long FromNumber { get; private set; }
    public long ToNumber { get; private set; }
    public long NextNumber { get; private set; }
    public DateOnly? ExpirationDate { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static EcfSequenceRange Create(
        Guid businessId,
        EcfEnvironment environment,
        EcfType type,
        long fromNumber,
        long toNumber,
        long? nextNumber,
        DateOnly? expirationDate)
    {
        if (businessId == Guid.Empty)
            throw new ArgumentException("Business id is required.", nameof(businessId));

        if (fromNumber is < 1 or > 9_999_999_999L)
            throw new ArgumentOutOfRangeException(nameof(fromNumber), "Sequence start must be between 1 and 9,999,999,999.");

        if (toNumber < fromNumber || toNumber > 9_999_999_999L)
            throw new ArgumentOutOfRangeException(nameof(toNumber), "Sequence end must be within the e-NCF 10-digit range and not precede the start.");

        var normalizedNext = nextNumber ?? fromNumber;
        if (normalizedNext < fromNumber || normalizedNext > toNumber)
            throw new ArgumentOutOfRangeException(nameof(nextNumber), "Next sequence must fall inside the configured range.");

        return new EcfSequenceRange(
            Guid.CreateVersion7(),
            businessId,
            environment,
            type,
            fromNumber,
            toNumber,
            normalizedNext,
            expirationDate);
    }

    public string ReserveNext()
    {
        if (!IsActive)
            throw new InvalidOperationException("The e-NCF sequence range is inactive.");

        if (NextNumber > ToNumber)
            throw new InvalidOperationException("The e-NCF sequence range is exhausted.");

        var value = NextNumber;
        NextNumber++;

        return FormatENcf(Type, value);
    }

    public static string FormatENcf(EcfType type, long sequenceNumber)
    {
        if (sequenceNumber is < 1 or > 9_999_999_999L)
            throw new ArgumentOutOfRangeException(nameof(sequenceNumber));

        return $"E{(int)type:00}{sequenceNumber:0000000000}";
    }
}
