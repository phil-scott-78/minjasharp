using System.Runtime.InteropServices;

namespace MinjaSharp;
// Ensure MinjaRenderException and MinjaJsonException are defined as shown previously,
// inheriting from MinjaException.

/// <summary>
/// Represents a compiled Minja template.
/// </summary>
public sealed class Template : IDisposable
{
    private IntPtr Handle { get; set; } // Make setter private
    private bool _disposed;

    /// <summary>
    /// Creates a new Template instance by parsing the provided template string.
    /// </summary>
    /// <param name="tmpl">The template string to parse. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">If tmpl is null.</exception>
    /// <exception cref="MinjaParseException">Thrown when template parsing fails in the native layer.</exception>
    /// <exception cref="MinjaException">For other native errors during parsing.</exception>
    public Template(string tmpl)
    {
        if (tmpl == null) throw new ArgumentNullException(nameof(tmpl));

        var result = Native.mj_parse(tmpl, out var templateHandle);
        Native.CheckResult(result, "Parsing template");
        Handle = templateHandle;
    }

    /// <summary>
    /// Renders the template using the provided context.
    /// </summary>
    /// <param name="data">The data containing values for the template. Cannot be null.</param>
    /// <returns>The rendered template as a string.</returns>
    /// <exception cref="ObjectDisposedException">If the template is disposed.</exception>
    /// <exception cref="ArgumentNullException">If ctx is null.</exception>
    /// <exception cref="MinjaRenderException">Thrown if template rendering fails in the native layer.</exception>
    /// <exception cref="MinjaException">For other native errors during rendering.</exception>
    public string Render<T>(T data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return Render(Context.From(data));
    }
    
    /// <summary>
    /// Renders the template using the provided context.
    /// </summary>
    /// <param name="ctx">The context containing values for the template. Cannot be null.</param>
    /// <returns>The rendered template as a string.</returns>
    /// <exception cref="ObjectDisposedException">If the template is disposed.</exception>
    /// <exception cref="ArgumentNullException">If ctx is null.</exception>
    /// <exception cref="MinjaRenderException">Thrown if template rendering fails in the native layer.</exception>
    /// <exception cref="MinjaException">For other native errors during rendering.</exception>
    public string Render(Context ctx)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(Template));
        ArgumentNullException.ThrowIfNull(ctx);
        
        if (ctx.Handle == IntPtr.Zero) throw new ArgumentException("Context has an invalid (null) handle.", nameof(ctx));
        var result = Native.mj_render_ctx(Handle, ctx.Handle, out var renderedStringPtr);
        Native.CheckResult(result, "Rendering template with context");

        if (renderedStringPtr == IntPtr.Zero)
        {
            // This case should ideally be caught by CheckResult if MJ_OK was returned but ptr is null.
            // However, if MJ_OK is returned AND ptr is Zero, it's an empty string by convention.
            // The C++ code aims to return MJ_ERROR_ALLOCATION_FAILED if create_c_string fails.
            // If CheckResult didn't throw, and we get here with MJ_OK and Zero ptr, it means empty string.
            return string.Empty;
        }

        string renderedString;
        try
        {
            renderedString = Marshal.PtrToStringUTF8(renderedStringPtr) ?? string.Empty;
        }
        finally
        {
            Native.mj_free_string(renderedStringPtr);
        }
        return renderedString;
    }

    /// <summary>
    /// Renders the template using a JSON string as context.
    /// </summary>
    /// <param name="jsonContext">JSON string containing context data. Cannot be null.</param>
    /// <returns>The rendered template as a string.</returns>
    /// <exception cref="ObjectDisposedException">If the template is disposed.</exception>
    /// <exception cref="ArgumentNullException">If jsonContext is null.</exception>
    /// <exception cref="MinjaJsonException">Thrown when JSON parsing fails in the native layer.</exception>
    /// <exception cref="MinjaRenderException">Thrown when template rendering fails in the native layer.</exception>
    /// <exception cref="MinjaException">For other unexpected native errors.</exception>
    public string RenderJson(string jsonContext)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(Template));
        ArgumentNullException.ThrowIfNull(jsonContext);

        var result = Native.mj_render_json(Handle, jsonContext, out var renderedStringPtr);
        Native.CheckResult(result, "Rendering template with JSON context");
            
        if (renderedStringPtr == IntPtr.Zero)
        {
            // Similar to Render(Context), if MJ_OK and ptr is Zero, it's an empty string.
            return string.Empty;
        }

        string renderedString;
        try
        {
            renderedString = Marshal.PtrToStringUTF8(renderedStringPtr) ?? string.Empty;
        }
        finally
        {
            Native.mj_free_string(renderedStringPtr);
        }

        return renderedString;
    }


    /// <summary>
    /// Disposes the template and frees native resources.
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
                // free managed resources
            }
            if (Handle != IntPtr.Zero)
            {
                Native.mj_free_template(Handle);
                Handle = IntPtr.Zero;
            }
            _disposed = true;
        }
    }

    ~Template() => Dispose(false);
}