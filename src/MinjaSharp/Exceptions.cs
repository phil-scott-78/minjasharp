namespace MinjaSharp;

/// <summary>
/// Base exception for errors originating from the MinjaSharp library or its native component.
/// </summary>
public class MinjaException : Exception
{
    public int ErrorCode { get; }

    public MinjaException(string message, int errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    public MinjaException(string message, int errorCode, Exception innerException) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Exception thrown when a Minja template parsing operation fails.
/// </summary>
public class MinjaParseException(string message, int errorCode)
    : MinjaException(message, errorCode);

/// <summary>
/// Exception thrown when a Minja template rendering operation fails.
/// </summary>
public class MinjaRenderException(string message, int errorCode)
    : MinjaException(message, errorCode); // Modified to inherit MinjaException

/// <summary>
/// Exception thrown when JSON parsing fails within Minja operations.
/// </summary>
public class MinjaJsonException(string message, int errorCode)
    : MinjaException(message, errorCode); // Modified to inherit MinjaException

/// <summary>
/// Exception thrown when a specific Minja operation (like array push or object set) fails.
/// </summary>
public class MinjaOperationException(string message, int errorCode)
    : MinjaException(message, errorCode);

/// <summary>
/// Exception thrown when a Minja operation fails due to memory allocation issues.
/// </summary>
public class MinjaAllocationException(string message, int errorCode)
    : MinjaException(message, errorCode); // Could also be OutOfMemoryException