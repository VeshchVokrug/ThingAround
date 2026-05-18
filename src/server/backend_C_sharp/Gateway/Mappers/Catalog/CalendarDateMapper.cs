using CalendarDateDto = Gateway.Models.CalendarDate;
using CalendarDateGrpc = CatalogService.Grpc.CalendarDate;

namespace Gateway.Mappers.Catalog;

public static class CalendarDateMapper
{
    public static CalendarDateGrpc ToGrpc(this CalendarDateDto source)
    {
        return new CalendarDateGrpc
        {
            Year = source.Year,
            Month = source.Month,
            Day = source.Day
        };
    }

    public static CalendarDateDto ToDto(this CalendarDateGrpc source)
    {
        return new CalendarDateDto
        {
            Year = source.Year,
            Month = source.Month,
            Day = source.Day
        };
    }
}

