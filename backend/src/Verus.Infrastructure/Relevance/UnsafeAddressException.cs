namespace Verus.Infrastructure.Relevance;

/// <summary>Raised when a host resolves to a non-public address. Never retried.</summary>
internal sealed class UnsafeAddressException(string message) : IOException(message);
