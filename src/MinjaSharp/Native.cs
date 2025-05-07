using System.Runtime.InteropServices;

namespace MinjaSharp;

internal static partial class Native
{
    internal static void CheckResult(int resultCode, string operationName)
    {
        if (resultCode == MjOk)
        {
            return;
        }

        var errorMsgPtr = mj_get_last_error();
        var errorMessage = "Unknown native error, and no details could be retrieved.";
        if (errorMsgPtr != IntPtr.Zero)
        {
            try
            {
                // PtrToStringUTF8 can return null if the pointer is invalid or the string is empty in some cases.
                errorMessage = Marshal.PtrToStringUTF8(errorMsgPtr) ?? "Native error message was null.";
            }
            catch (Exception ex) // Catch potential issues with PtrToStringUTF8 itself
            {
                errorMessage = $"Failed to retrieve error message string from native layer: {ex.Message}";
            }
            finally
            {
                mj_free_string(errorMsgPtr); // Always try to free the native string
            }
        }

        var fullMessage = $"{operationName} failed. Code: {resultCode}. Details: {errorMessage}";

        switch (resultCode)
        {
            case MjErrorInvalidArgument:
                throw new ArgumentException(fullMessage);
            case MjErrorAllocationFailed:
                throw new MinjaAllocationException(fullMessage, resultCode);
            case MjErrorJsonParse:
                throw new MinjaJsonException(fullMessage, resultCode);
            case MjErrorTemplateRender:
                throw new MinjaRenderException(fullMessage, resultCode);
            case MjErrorTemplateParse:
                throw new MinjaParseException(fullMessage, resultCode);
            case MjErrorOperationFailed:
                throw new MinjaOperationException(fullMessage, resultCode);
            default: // MJ_ERROR or any other code
                throw new MinjaException(fullMessage, resultCode);
        }
    }
        
    private const string DllName = "minja_shim_ext"; // Renamed for clarity, Dll is a bit generic

    // --- Error Code Definitions (mirroring C++ header) ---
    private const int MjOk = 0;
    public const int MjError = 1;
    private const int MjErrorInvalidArgument = 2;
    private const int MjErrorAllocationFailed = 3;
    private const int MjErrorJsonParse = 4;
    private const int MjErrorTemplateRender = 5;
    private const int MjErrorOperationFailed = 6;
    private const int MjErrorTemplateParse = 7;
    
    // --- Error Handling ---
    // Retrieves the last error message.
    // The returned IntPtr points to a C-string that must be freed using mj_free_string.
    // Returns IntPtr.Zero if no error message is set or if allocation of the message copy fails.
    [LibraryImport(DllName)] // Ansi for const char* that we'll marshal manually
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial IntPtr mj_get_last_error();

    // --- Template parsing / freeing ---
    // Parses a template string.
    // On success, returns MJ_OK and sets out_template_handle.
    // On failure, returns an error code and out_template_handle will be IntPtr.Zero.
    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int mj_parse([MarshalAs(UnmanagedType.LPUTF8Str)] string tmplStr, out IntPtr outTemplateHandle);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void mj_free_template(IntPtr templateHandle);

    // --- Render by JSON ---
    // Renders a template using a JSON string as context.
    // On success, returns MJ_OK and sets out_rendered_string.
    // On failure, returns an error code and out_rendered_string will be IntPtr.Zero.
    // The string pointed to by out_rendered_string must be freed using mj_free_string.
    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int mj_render_json(IntPtr templateHandle, [MarshalAs(UnmanagedType.LPUTF8Str)] string jsonCtxStr, out IntPtr outRenderedString);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void mj_free_string(IntPtr s);

    // --- Value APIs ---
    // All value constructors return MJ_OK on success and set out_value_handle.
    // On failure, they return an error code and out_value_handle will be IntPtr.Zero.
    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int mj_value_null(out IntPtr outValueHandle);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int mj_value_bool([MarshalAs(UnmanagedType.I1)] bool b, out IntPtr outValueHandle); // C++ bool is 1 byte

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int mj_value_int(long i, out IntPtr outValueHandle); // C# long is int64_t

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int mj_value_double(double d, out IntPtr outValueHandle);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int mj_value_string([MarshalAs(UnmanagedType.LPUTF8Str)] string s, out IntPtr outValueHandle);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int mj_value_array(out IntPtr outValueHandle);

    // Pushes a value to an array.
    // Returns MJ_OK on success or an error code on failure.
    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int mj_array_push(IntPtr arrayHandle, IntPtr valueHandle);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int mj_value_object(out IntPtr outValueHandle);

    // Sets a key-value pair in an object.
    // Returns MJ_OK on success or an error code on failure.
    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int mj_object_set(IntPtr objectHandle, [MarshalAs(UnmanagedType.LPUTF8Str)] string key, IntPtr valueHandle);

    // --- Context from a Value, and free it ---
    // Creates a rendering context from a root value.
    // On success, returns MJ_OK and sets out_context_handle.
    // On failure, returns an error code, and out_context_handle will be IntPtr.Zero.
    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int mj_context_make(IntPtr rootValueHandle, out IntPtr outContextHandle);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void mj_free_context(IntPtr contextHandle);

    // --- Render by Context ---
    // Renders a template using a pre-built context.
    // On success, returns MJ_OK and sets out_rendered_string.
    // On failure, returns an error code and out_rendered_string will be IntPtr.Zero.
    // The string pointed to by out_rendered_string must be freed using mj_free_string.
    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int mj_render_ctx(IntPtr templateHandle, IntPtr contextHandle, out IntPtr outRenderedString);

    // --- Value memory management ---
    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void mj_free_value(IntPtr valueHandle);
}