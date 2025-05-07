#pragma once
#include <cstdint> // For int64_t

// --- Error Code Definitions ---
// These can be used by the C# wrapper to understand the nature of errors.
#define MJ_OK 0                         // Success
#define MJ_ERROR 1                      // Generic error
#define MJ_ERROR_INVALID_ARGUMENT 2     // Invalid input argument (e.g., null pointer for required input)
#define MJ_ERROR_ALLOCATION_FAILED 3    // Memory allocation failed
#define MJ_ERROR_JSON_PARSE 4           // JSON parsing failed
#define MJ_ERROR_TEMPLATE_RENDER 5      // Template rendering failed
#define MJ_ERROR_OPERATION_FAILED 6     // e.g., for array_push, object_set if the operation itself fails (not due to bad_alloc)
#define MJ_ERROR_TEMPLATE_PARSE 7       // Template parsing failed (specific to mj_parse)

// --- DLL Export Macro ---
#ifdef _WIN32
  #define SHIM_EXPORT __declspec(dllexport)
#else
  #define SHIM_EXPORT
#endif

extern "C" {

// --- Error Handling ---
// Retrieves the last error message.
// The returned string must be freed using mj_free_string.
// Returns nullptr if no error message is set or if allocation of the message copy fails.
SHIM_EXPORT const char* mj_get_last_error();

// --- Template parsing / freeing ---
// Parses a template string.
// On success, returns MJ_OK and sets out_template_handle.
// On failure, returns an error code and out_template_handle will be nullptr.
SHIM_EXPORT int mj_parse(const char* tmpl_str, void** out_template_handle);
SHIM_EXPORT void mj_free_template(void* template_handle); // Renamed param for consistency

// --- Render by JSON ---
// Renders a template using a JSON string as context.
// On success, returns MJ_OK and sets out_rendered_string.
// On failure, returns an error code and out_rendered_string will be nullptr.
// The returned string in out_rendered_string must be freed using mj_free_string.
SHIM_EXPORT int mj_render_json(void* template_handle, const char* json_ctx_str, char** out_rendered_string);
SHIM_EXPORT void mj_free_string(char* s);

// --- Value constructors ---
// All value constructors return MJ_OK on success and set out_value_handle.
// On failure, they return an error code and out_value_handle will be nullptr.
SHIM_EXPORT int mj_value_null(void** out_value_handle);
SHIM_EXPORT int mj_value_bool(bool b, void** out_value_handle);
SHIM_EXPORT int mj_value_int(int64_t i, void** out_value_handle);
SHIM_EXPORT int mj_value_double(double d, void** out_value_handle);
SHIM_EXPORT int mj_value_string(const char* s, void** out_value_handle);

// --- Compound values ---
SHIM_EXPORT int mj_value_array(void** out_value_handle);
// Pushes a value to an array.
// Returns MJ_OK on success, or an error code on failure (e.g., invalid arguments, type mismatch, allocation error).
SHIM_EXPORT int mj_array_push(void* array_handle, void* value_handle);

SHIM_EXPORT int mj_value_object(void** out_value_handle);
// Sets a key-value pair in an object.
// Returns MJ_OK on success, or an error code on failure.
SHIM_EXPORT int mj_object_set(void* object_handle, const char* key, void* value_handle);

// --- Context from a Value, and free it ---
// Creates a rendering context from a root value.
// On success, returns MJ_OK and sets out_context_handle.
// On failure, returns an error code and out_context_handle will be nullptr.
SHIM_EXPORT int mj_context_make(void* root_value_handle, void** out_context_handle);
SHIM_EXPORT void mj_free_context(void* context_handle); // Renamed param for consistency

// --- Render by Context ---
// Renders a template using a pre-built context.
// On success, returns MJ_OK and sets out_rendered_string.
// On failure, returns an error code and out_rendered_string will be nullptr.
// The returned string in out_rendered_string must be freed using mj_free_string.
SHIM_EXPORT int mj_render_ctx(void* template_handle, void* context_handle, char** out_rendered_string);

// --- Value memory management ---
SHIM_EXPORT void mj_free_value(void* value_handle); // Renamed param for consistency

}  // extern "C"