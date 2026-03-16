namespace Base.Contracts;

public class ConcurrencyException(string message = "A concurrency conflict occurred.") : Exception(message);
