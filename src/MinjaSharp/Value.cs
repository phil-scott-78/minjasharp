namespace MinjaSharp;

/// <summary>
/// Represents a Minja value that can be used in template rendering.
/// </summary>
public sealed class Value : IDisposable
{
    internal IntPtr Handle { get; private set; } // Make setter private
    private bool _disposed;
    private readonly bool _ownsHandle; // To allow for non-owning Value instances if ever needed

    private Value(IntPtr handle, bool ownsHandle = true)
    {
        if (handle == IntPtr.Zero)
        {
            // This constructor should ideally not be called with IntPtr.Zero
            // unless it's an error path already handled by CheckResult.
            // If we reach here with Zero, it implies a logic error in the factory method.
            throw new ArgumentException("Cannot create Value with a null handle directly.");
        }
        Handle = handle;
        _ownsHandle = ownsHandle;
    }

    /// <summary>
    /// Creates a null value.
    /// </summary>
    public static Value Null()
    {
        var result = Native.mj_value_null(out var handle);
        Native.CheckResult(result, "Creating null value");
        return new Value(handle);
    }

    /// <summary>
    /// Creates a boolean value.
    /// </summary>
    public static Value Bool(bool b)
    {
        var result = Native.mj_value_bool(b, out var handle);
        Native.CheckResult(result, "Creating boolean value");
        return new Value(handle);
    }

    /// <summary>
    /// Creates an integer value.
    /// </summary>
    public static Value Int(long i)
    {
        var result = Native.mj_value_int(i, out var handle);
        Native.CheckResult(result, "Creating integer value");
        return new Value(handle);
    }

    /// <summary>
    /// Creates a double value.
    /// </summary>
    public static Value Double(double d)
    {
        var result = Native.mj_value_double(d, out var handle);
        Native.CheckResult(result, "Creating double value");
        return new Value(handle);
    }

    /// <summary>
    /// Creates a string value.
    /// </summary>
    public static Value String(string s)
    {
        ArgumentNullException.ThrowIfNull(s);
        
        var result = Native.mj_value_string(s, out var handle);
        Native.CheckResult(result, "Creating string value");
        return new Value(handle);
    }

    /// <summary>
    /// Creates an empty array value.
    /// </summary>
    public static Value Array()
    {
        var result = Native.mj_value_array(out var handle);
        Native.CheckResult(result, "Creating array value");
        return new Value(handle);
    }

    /// <summary>
    /// Creates an empty object value.
    /// </summary>
    public static Value Object()
    {
        var result = Native.mj_value_object(out var handle);
        Native.CheckResult(result, "Creating object value");
        return new Value(handle);
    }

    /// <summary>
    /// Adds an element to an array value.
    /// </summary>
    /// <param name="elem">The element value to add.</param>
    /// <exception cref="ObjectDisposedException">Thrown if this value is disposed.</exception>
    /// <exception cref="MinjaOperationException">Thrown if the native operation fails (e.g., not an array, allocation error).</exception>
    public void Add(Value elem)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(Value));
        ArgumentNullException.ThrowIfNull(elem);
        ObjectDisposedException.ThrowIf(elem._disposed, elem);

        var result = Native.mj_array_push(Handle, elem.Handle);
        Native.CheckResult(result, "Adding element to array");
    }

    /// <summary>
    /// Sets a property on an object value.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="val">The property value.</param>
    /// <exception cref="ObjectDisposedException">Thrown if this value is disposed.</exception>
    /// <exception cref="ArgumentNullException">Thrown if key or val is null.</exception>
    /// <exception cref="MinjaOperationException">Thrown if the native operation fails (e.g., not an object, allocation error).</exception>
    public void Set(string key, Value val)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(Value));
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(val);
        ObjectDisposedException.ThrowIf(val._disposed, val);

        var result = Native.mj_object_set(Handle, key, val.Handle);
        Native.CheckResult(result, $"Setting object property '{key}'");
    }

    /// <summary>
    /// Disposes the value and frees native resources if this instance owns the handle.
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
                // Free managed resources here, if any.
            }

            // Free unmanaged resources
            if (_ownsHandle && Handle != IntPtr.Zero)
            {
                Native.mj_free_value(Handle);
                Handle = IntPtr.Zero; // Mark as freed
            }
            _disposed = true;
        }
    }

    ~Value()
    {
        Dispose(false);
    }
}