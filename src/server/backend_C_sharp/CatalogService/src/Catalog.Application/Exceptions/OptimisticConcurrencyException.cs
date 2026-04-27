namespace Application.Exceptions;

public class OptimisticConcurrencyException(string message) : Exception(message);

