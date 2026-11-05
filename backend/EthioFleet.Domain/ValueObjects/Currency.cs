namespace EthioFleet.Domain.ValueObjects;

public sealed record Currency
{
    public static Currency ETB { get; } = new("ETB");

    public string Code { get; }

    public Currency(string code)
    {
        if (code is null || code.Length != 3 || code.Any(character => character is < 'A' or > 'Z'))
        {
            throw new ArgumentException("Currency code must be three uppercase ASCII letters.", nameof(code));
        }

        Code = code;
    }
}
