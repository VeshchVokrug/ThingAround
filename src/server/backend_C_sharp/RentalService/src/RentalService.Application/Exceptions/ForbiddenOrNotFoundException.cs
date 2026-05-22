namespace RentalService.Application.Exceptions;

public class ForbiddenOrNotFoundException(string entityName, Guid id)
    : Exception($"{entityName} с идентификатором {id} не найдено или у вас недостаточно прав.");