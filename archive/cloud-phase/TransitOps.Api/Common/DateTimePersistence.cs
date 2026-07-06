namespace TransitOps.Api.Common;

public static class DateTimePersistence
{
    public static DateTime AsUnspecified(DateTime value)
    {
        return value.Kind == DateTimeKind.Unspecified
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
    }

    public static DateTime? AsUnspecified(DateTime? value)
    {
        return value.HasValue
            ? AsUnspecified(value.Value)
            : null;
    }
}
