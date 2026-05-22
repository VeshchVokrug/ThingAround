namespace Application.Exceptions;

public class ListingInactiveException(Guid id) : Exception($"Объявление с идентификатором {id} неактивно.");