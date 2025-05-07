using System.Collections;
using System.Reflection;
using System.Text.Json.Serialization;

namespace MinjaSharp;

/// <summary>
/// Helper class that uses reflection to build Minja Value trees from C# objects.
/// </summary>
public static class ValueBuilder
{
    /// <summary>
    /// Creates a Value from a C# object, using reflection to build a Value tree.
    /// Supports primitives, strings, dictionaries, collections, and POCOs.
    /// </summary>
    /// <typeparam name="T">The type of the object to convert to a Value.</typeparam>
    /// <param name="data">The object to convert to a Value.</param>
    /// <returns>A Value representation of the provided object.</returns>
    internal static Value From<T>(T? data)
    {
        switch (data)
        {
            case null:
                return Value.Null();
            // Handle primitive types
            case string s:
                return Value.String(s);
            case bool b:
                return Value.Bool(b);
            case int i:
                return Value.Int(i);
            case long l:
                return Value.Int(l);
            case float f:
                return Value.Double(f);
            case double d:
                return Value.Double(d);
            case decimal dec:
                return Value.Double((double)dec);
            // Handle dictionaries
            case IDictionary<string, object> dict:
            {
                var obj = Value.Object();
                foreach (var kv in dict)
                {
                    using var val = From(kv.Value);
                    obj.Set(kv.Key, val);
                }
                return obj;
            }
            // Handle general dictionaries with string keys
            case IDictionary genericDict:
            {
                var obj = Value.Object();
                foreach (DictionaryEntry entry in genericDict)
                {
                    if (entry.Key is string key)
                    {
                        using var val = From(entry.Value);
                        obj.Set(key, val);
                    }
                }
                return obj;
            }
            // Handle collections/arrays
            case IEnumerable enumerable and not string:
            {
                var arr = Value.Array();
                foreach (var item in enumerable)
                {
                    using var val = From(item);
                    arr.Add(val);
                }
                return arr;
            }
        }

        // Handle POCO objects through reflection
        var objValue = Value.Object();
        var type = data.GetType();
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            try
            {
                var propVal = prop.GetValue(data);
                using var val = From(propVal);
                
                // Check for JsonPropertyName attribute
                var jsonPropertyAttr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
                var propertyName = jsonPropertyAttr != null ? jsonPropertyAttr.Name : prop.Name.ToLower();
                
                objValue.Set(propertyName, val);
            }
            catch
            {
                // Skip properties that can't be read
            }
        }
        return objValue;
    }
}