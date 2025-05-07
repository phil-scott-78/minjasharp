#include "minja_shim_ext.h" 
#include <minja/minja.hpp>
#include <cstdlib>
#include <cstring>
#include <nlohmann/json.hpp>
#include <exception>
#include <string>
#include <vector>
#include <utility>  // For std::move

using namespace minja;

// Namespace for internal error handling utilities
namespace minja_shim_ext_internal
{
  thread_local std::string g_last_error_message;

  void clear_last_error()
  {
    g_last_error_message.clear();
  }

  void set_last_error(const std::string &message)
  {
    g_last_error_message = message;
  }

  void format_and_set_error(const char *prefix, const char *details = nullptr)
  {
    std::string error_message = "ERROR: ";
    if (prefix) {
        error_message += prefix;
    } else {
        error_message += "Unknown operation";
    }
    
    if (details && *details)
    {
      error_message += ": ";
      error_message += details;
    }
    set_last_error(error_message);
  }
} // namespace minja_shim_ext_internal

// Utility functions (kept in anonymous namespace as they are local to this file)
namespace
{
  // Convert a string to use lowercase boolean values (true/false instead of True/False)
  std::string convert_boolean_representation(const std::string &input)
  {
    std::string result = input;
    size_t pos = result.find("True");
    while (pos != std::string::npos)
    {
      result.replace(pos, 4, "true");
      pos = result.find("True", pos + 4);
    }
    pos = result.find("False");
    while (pos != std::string::npos)
    {
      result.replace(pos, 5, "false");
      pos = result.find("False", pos + 5);
    }
    return result;
  }

  // Allocate and return a C-string. Returns nullptr on allocation failure.
  char *create_c_string(const std::string &str)
  {
    char *result = static_cast<char *>(std::malloc(str.size() + 1));
    if (!result)
    {
      return nullptr; // Caller must handle this
    }
    std::memcpy(result, str.c_str(), str.size() + 1);
    return result;
  }

  // Helper for value constructors
  template<typename Func>
  int create_value_helper(Func f, void** out_value_handle, const char* func_name) {
      if (!out_value_handle) {
          minja_shim_ext_internal::set_last_error(std::string(func_name) + ": Output parameter 'out_value_handle' is null.");
          return MJ_ERROR_INVALID_ARGUMENT;
      }
      *out_value_handle = nullptr;
      minja_shim_ext_internal::clear_last_error();
      try {
          *out_value_handle = f();
          if (!*out_value_handle && std::string(func_name) != "mj_value_null") { // new Value() can return non-null even for logical null
               // This case is tricky. `new Value()` for default constructor (null) will succeed.
               // Other `new Value(...)` could fail by throwing bad_alloc.
               // If `new` itself returns `nullptr` (non-standard but possible with `nothrow`), this would catch it.
               minja_shim_ext_internal::format_and_set_error(func_name, "Allocation failed (new returned null)");
               return MJ_ERROR_ALLOCATION_FAILED;
          }
          return MJ_OK;
      } catch (const std::bad_alloc& e) {
          minja_shim_ext_internal::format_and_set_error(func_name, e.what());
          return MJ_ERROR_ALLOCATION_FAILED;
      } catch (const std::exception& e) {
          minja_shim_ext_internal::format_and_set_error(func_name, e.what());
          return MJ_ERROR;
      } catch (...) {
          minja_shim_ext_internal::format_and_set_error(func_name, "Unknown exception occurred");
          return MJ_ERROR;
      }
  }
} // namespace

// Public C API functions
extern "C"
{
  SHIM_EXPORT const char *mj_get_last_error()
  {
    if (minja_shim_ext_internal::g_last_error_message.empty())
    {
      return nullptr;
    }

    std::string error_to_copy = minja_shim_ext_internal::g_last_error_message;
    // Clear the error after copying its content, before attempting allocation for the C string.
    // This ensures that if create_c_string fails, the error state is still "cleared" for the next operation.
    minja_shim_ext_internal::clear_last_error();

    char *error_c_str = create_c_string(error_to_copy);
    // If allocation fails, C# gets nullptr. It can't get details about *this* specific failure via this mechanism.
    // This is an extreme OOM, and getting nullptr is an indicator of that.
    return error_c_str; // C# must free this using mj_free_string
  }

  SHIM_EXPORT int mj_parse(const char *tmpl_str, void **out_template_handle)
  {
    if (!out_template_handle) {
        // This is a programming error on the caller's side.
        // We can't use format_and_set_error safely if we don't know if g_last_error_message itself is safe.
        // However, for consistency, we'll try.
        minja_shim_ext_internal::set_last_error("mj_parse: Output parameter 'out_template_handle' is null.");
        return MJ_ERROR_INVALID_ARGUMENT;
    }
    *out_template_handle = nullptr;
    minja_shim_ext_internal::clear_last_error();

    if (!tmpl_str)
    {
      minja_shim_ext_internal::format_and_set_error("mj_parse: Input template string is null");
      return MJ_ERROR_INVALID_ARGUMENT;
    }

    try
    {
      auto options = Options{};
      auto tpl = Parser::parse(tmpl_str, options);
      // The void* handle stores a pointer to a heap-allocated std::shared_ptr<TemplateNode>
      *out_template_handle = new std::shared_ptr<TemplateNode>(tpl); 
      return MJ_OK;
    }
    catch (const std::bad_alloc &e)
    {
      minja_shim_ext_internal::format_and_set_error("mj_parse: Allocation failed", e.what());
      return MJ_ERROR_ALLOCATION_FAILED;
    }
    catch (const std::exception &e)
    {
      // Handle all parse errors with the same exception type, since we can't rely on parse_error being accessible
      minja_shim_ext_internal::format_and_set_error("mj_parse: Template parsing failed", e.what());
      return MJ_ERROR_TEMPLATE_PARSE;
    }
    catch (...)
    {
      minja_shim_ext_internal::format_and_set_error("mj_parse: Unknown exception occurred");
      return MJ_ERROR;
    }
  }

  SHIM_EXPORT void mj_free_template(void *template_handle)
  {
    try
    {
      if (template_handle)
      {
        delete static_cast<std::shared_ptr<TemplateNode> *>(template_handle);
      }
    }
    catch (...)
    {
      // Ignore exceptions during cleanup as per original design
    }
  }

  SHIM_EXPORT int mj_render_json(void *template_handle, const char *json_ctx_str, char **out_rendered_string)
  {
    if (!out_rendered_string) {
        minja_shim_ext_internal::set_last_error("mj_render_json: Output parameter 'out_rendered_string' is null.");
        return MJ_ERROR_INVALID_ARGUMENT;
    }
    *out_rendered_string = nullptr;
    minja_shim_ext_internal::clear_last_error();

    if (!template_handle || !json_ctx_str)
    {
      minja_shim_ext_internal::format_and_set_error("mj_render_json: Template handle or JSON context string is null");
      return MJ_ERROR_INVALID_ARGUMENT;
    }

    try
    {
      auto tpl_ptr = static_cast<std::shared_ptr<TemplateNode> *>(template_handle);

      nlohmann::json parsed_json;
      try
      {
        parsed_json = nlohmann::json::parse(json_ctx_str);
      }
      catch (const nlohmann::json::parse_error &e)
      {
        minja_shim_ext_internal::format_and_set_error("mj_render_json: JSON parse error", e.what());
        return MJ_ERROR_JSON_PARSE;
      }

      json minja_json_val = parsed_json; // Assuming minja::json is nlohmann::json or compatible
      auto val = Value(minja_json_val);
      auto ctx = Context::make(std::move(val));

      std::string out_str;
      try
      {
        out_str = (*tpl_ptr)->render(ctx);
      }
      catch (const std::exception &e)
      {
        minja_shim_ext_internal::format_and_set_error("mj_render_json: Template rendering failed", e.what());
        return MJ_ERROR_TEMPLATE_RENDER;
      }

      out_str = convert_boolean_representation(out_str);
      *out_rendered_string = create_c_string(out_str);

      if (!*out_rendered_string)
      {
        minja_shim_ext_internal::format_and_set_error("mj_render_json: Failed to allocate memory for output string");
        return MJ_ERROR_ALLOCATION_FAILED;
      }
      return MJ_OK;
    }
    catch (const std::bad_alloc &e)
    {
      minja_shim_ext_internal::format_and_set_error("mj_render_json: Allocation failed", e.what());
      return MJ_ERROR_ALLOCATION_FAILED;
    }
    catch (const std::exception &e)
    {
      minja_shim_ext_internal::format_and_set_error("mj_render_json: Unexpected error", e.what());
      return MJ_ERROR;
    }
    catch (...)
    {
      minja_shim_ext_internal::format_and_set_error("mj_render_json: Unknown exception occurred");
      return MJ_ERROR;
    }
  }

  SHIM_EXPORT int mj_value_null(void **out_value_handle)
  {
    return create_value_helper([]{ return new Value(); }, out_value_handle, "mj_value_null");
  }

  SHIM_EXPORT int mj_value_bool(bool b, void **out_value_handle)
  {
    return create_value_helper([b]{ return new Value(b); }, out_value_handle, "mj_value_bool");
  }

  SHIM_EXPORT int mj_value_int(int64_t i, void **out_value_handle)
  {
    return create_value_helper([i]{ return new Value(i); }, out_value_handle, "mj_value_int");
  }

  SHIM_EXPORT int mj_value_double(double d, void **out_value_handle)
  {
    return create_value_helper([d]{ return new Value(d); }, out_value_handle, "mj_value_double");
  }

  SHIM_EXPORT int mj_value_string(const char *s, void **out_value_handle)
  {
    if (!s) { // Specific check for string input before helper
        if (out_value_handle) *out_value_handle = nullptr;
        minja_shim_ext_internal::clear_last_error();
        minja_shim_ext_internal::format_and_set_error("mj_value_string", "Input string is null");
        return MJ_ERROR_INVALID_ARGUMENT;
    }
    return create_value_helper([s]{ return new Value(std::string(s)); }, out_value_handle, "mj_value_string");
  }

  SHIM_EXPORT int mj_value_array(void **out_value_handle)
  {
    return create_value_helper([]{ 
        // Create an array value by creating an empty JSON array
        auto val = new Value(json::array());
        return val;
    }, out_value_handle, "mj_value_array");
  }

  SHIM_EXPORT int mj_array_push(void *array_handle, void *value_handle)
  {
    minja_shim_ext_internal::clear_last_error();
    if (!array_handle || !value_handle)
    {
      minja_shim_ext_internal::format_and_set_error("mj_array_push: Array or value handle is null");
      return MJ_ERROR_INVALID_ARGUMENT;
    }

    try
    {
      auto arr_val = static_cast<Value *>(array_handle);
      auto val_to_push = static_cast<Value *>(value_handle);
      arr_val->push_back(*val_to_push);
      return MJ_OK;
    }
    catch (const std::bad_alloc &e) {
        minja_shim_ext_internal::format_and_set_error("mj_array_push: Allocation failed", e.what());
        return MJ_ERROR_ALLOCATION_FAILED;
    }
    catch (const std::exception &e)
    {
      minja_shim_ext_internal::format_and_set_error("mj_array_push: Failed to push value", e.what());
      return MJ_ERROR_OPERATION_FAILED;
    }
    catch (...)
    {
      minja_shim_ext_internal::format_and_set_error("mj_array_push: Unknown exception occurred");
      return MJ_ERROR;
    }
  }

  SHIM_EXPORT int mj_value_object(void **out_value_handle)
  {
    return create_value_helper([]{ 
        // Create an object value by creating an empty JSON object
        auto val = new Value(json::object());
        return val;
    }, out_value_handle, "mj_value_object");
  }

  SHIM_EXPORT int mj_object_set(void *object_handle, const char *key, void *value_handle)
  {
    minja_shim_ext_internal::clear_last_error();
    if (!object_handle || !key || !value_handle)
    {
      minja_shim_ext_internal::format_and_set_error("mj_object_set: Object handle, key, or value handle is null");
      return MJ_ERROR_INVALID_ARGUMENT;
    }

    try
    {
      auto obj_val = static_cast<Value *>(object_handle);
      auto val_to_set = static_cast<Value *>(value_handle);
      // Assuming minja::Value::set throws on type error or other issues.
      obj_val->set(key, *val_to_set);
      return MJ_OK;
    }
    catch (const std::bad_alloc &e) {
        minja_shim_ext_internal::format_and_set_error("mj_object_set: Allocation failed", e.what());
        return MJ_ERROR_ALLOCATION_FAILED;
    }
    catch (const std::exception &e)
    {
      minja_shim_ext_internal::format_and_set_error("mj_object_set: Failed to set object property", e.what());
      return MJ_ERROR_OPERATION_FAILED;
    }
    catch (...)
    {
      minja_shim_ext_internal::format_and_set_error("mj_object_set: Unknown exception occurred");
      return MJ_ERROR;
    }
  }

  SHIM_EXPORT int mj_context_make(void *root_value_handle, void **out_context_handle)
  {
    if (!out_context_handle) {
        minja_shim_ext_internal::set_last_error("mj_context_make: Output parameter 'out_context_handle' is null.");
        return MJ_ERROR_INVALID_ARGUMENT;
    }
    *out_context_handle = nullptr;
    minja_shim_ext_internal::clear_last_error();

    if (!root_value_handle)
    {
      minja_shim_ext_internal::format_and_set_error("mj_context_make: Root value handle is null");
      return MJ_ERROR_INVALID_ARGUMENT;
    }

    try
    {
      Value val_copy = *static_cast<Value *>(root_value_handle);
      auto ctx = Context::make(std::move(val_copy));
      *out_context_handle = new std::shared_ptr<Context>(ctx);
      return MJ_OK;
    }
    catch (const std::bad_alloc &e)
    {
      minja_shim_ext_internal::format_and_set_error("mj_context_make: Allocation failed", e.what());
      return MJ_ERROR_ALLOCATION_FAILED;
    }
    catch (const std::exception &e)
    {
      minja_shim_ext_internal::format_and_set_error("mj_context_make: Failed to create context", e.what());
      return MJ_ERROR;
    }
    catch (...)
    {
      minja_shim_ext_internal::format_and_set_error("mj_context_make: Unknown exception occurred");
      return MJ_ERROR;
    }
  }

  SHIM_EXPORT void mj_free_context(void *context_handle)
  {
    try
    {
      if (context_handle)
      {
        delete static_cast<std::shared_ptr<Context> *>(context_handle);
      }
    }
    catch (...)
    {
      // Ignore exceptions during cleanup
    }
  }

  SHIM_EXPORT int mj_render_ctx(void *template_handle, void *context_handle, char **out_rendered_string)
  {
    if (!out_rendered_string) {
        minja_shim_ext_internal::set_last_error("mj_render_ctx: Output parameter 'out_rendered_string' is null.");
        return MJ_ERROR_INVALID_ARGUMENT;
    }
    *out_rendered_string = nullptr;
    minja_shim_ext_internal::clear_last_error();

    if (!template_handle || !context_handle)
    {
      minja_shim_ext_internal::format_and_set_error("mj_render_ctx: Template or context handle is null");
      return MJ_ERROR_INVALID_ARGUMENT;
    }

    try
    {
      auto tpl_ptr = static_cast<std::shared_ptr<TemplateNode> *>(template_handle);
      auto ctx_ptr = static_cast<std::shared_ptr<Context> *>(context_handle);

      std::string out_str;
      try
      {
        // Fixed this line to correctly use the shared_ptr
        out_str = (*tpl_ptr)->render(*ctx_ptr); 
      }
      catch (const std::exception &e)
      {
        minja_shim_ext_internal::format_and_set_error("mj_render_ctx: Template rendering failed", e.what());
        return MJ_ERROR_TEMPLATE_RENDER;
      }

      out_str = convert_boolean_representation(out_str);
      *out_rendered_string = create_c_string(out_str);

      if (!*out_rendered_string)
      {
        minja_shim_ext_internal::format_and_set_error("mj_render_ctx: Failed to allocate memory for output string");
        return MJ_ERROR_ALLOCATION_FAILED;
      }
      return MJ_OK;
    }
    catch (const std::bad_alloc &e)
    {
      minja_shim_ext_internal::format_and_set_error("mj_render_ctx: Allocation failed", e.what());
      return MJ_ERROR_ALLOCATION_FAILED;
    }
    catch (const std::exception &e)
    {
      minja_shim_ext_internal::format_and_set_error("mj_render_ctx: Unexpected error", e.what());
      return MJ_ERROR;
    }
    catch (...)
    {
      minja_shim_ext_internal::format_and_set_error("mj_render_ctx: Unknown exception occurred");
      return MJ_ERROR;
    }
  }

  SHIM_EXPORT void mj_free_string(char *s)
  {
    try
    {
      if (s) { // Check for null before freeing
        std::free(s);
      }
    }
    catch (...)
    {
      // Ignore exceptions during cleanup
    }
  }

  SHIM_EXPORT void mj_free_value(void *value_handle)
  {
    try
    {
      if (value_handle)
      {
        delete static_cast<Value *>(value_handle);
      }
    }
    catch (...)
    {
      // Ignore exceptions during cleanup
    }
  }

} // extern "C"