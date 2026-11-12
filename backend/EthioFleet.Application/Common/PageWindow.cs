namespace EthioFleet.Application.Common;

public readonly record struct PageWindow(int Page, int PageSize, int Offset)
{
    public static PageWindow Create(int page, int pageSize)
    {
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var offset = (int)Math.Min(((long)normalizedPage - 1) * normalizedPageSize, int.MaxValue);
        return new PageWindow(normalizedPage, normalizedPageSize, offset);
    }
}
