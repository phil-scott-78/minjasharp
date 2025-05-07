namespace MinjaSharp;

/// <summary>
/// Represents a Minja context used for template rendering.
/// </summary>
public sealed class Context : IDisposable
{
    internal IntPtr Handle { get; private set; } // Make setter private
    private bool _disposed;

    /// <summary>
    /// Creates a context from a root value.
    /// </summary>
    /// <param name="root">The root value for this context. Cannot be null or disposed.</param>
    /// <exception cref="ArgumentNullException">If root is null.</exception>
    /// <exception cref="ObjectDisposedException">If root is disposed.</exception>
    /// <exception cref="MinjaException">If context creation fails in the native layer.</exception>
    public Context(Value root)
    {
        ArgumentNullException.ThrowIfNull(root);
            
        if (root.Handle == IntPtr.Zero) throw new ArgumentException("Root value has an invalid (null) handle.", nameof(root)); // Should be caught by disposed check mostly

        // Assuming Value class throws ObjectDisposedException if its Handle is accessed after disposal
        // or if its internal _disposed flag is checked.
        // For safety, we could check root._disposed if it were public/internal.
        // Given current Value implementation, Handle would be IntPtr.Zero if disposed and owned.

        var result = Native.mj_context_make(root.Handle, out var contextHandle);
        Native.CheckResult(result, "Creating context");
        Handle = contextHandle;
    }

    public static Context From<T>(T data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return new Context(ValueBuilder.From(data));
    }

    /// <summary>
    /// Disposes the context and frees native resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Free managed resources
            }

            if (Handle != IntPtr.Zero)
            {
                Native.mj_free_context(Handle);
                Handle = IntPtr.Zero;
            }
            _disposed = true;
        }
    }

    ~Context() => Dispose(false);
}