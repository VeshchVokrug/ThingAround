using Gateway.Models;
using CalendarDateGrpc = CatalogService.Grpc.CalendarDate;

namespace Gateway.Mappers.Catalog;

public static class CalendarDateMapper
{
    public static CalendarDateGrpc ToGrpc(this CatalogCalendarDateDto source)
    {
        return new CalendarDateGrpc
        {
            Year = source.Year,
            Month = source.Month,
            Day = source.Day
        };
    }

    public static CatalogCalendarDateDto ToDto(this CalendarDateGrpc source)
    {
        return new CatalogCalendarDateDto
        {
            Year = source.Year,
            Month = source.Month,
            Day = source.Day
        };
    }
}

