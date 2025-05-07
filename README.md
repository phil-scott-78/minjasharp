# MinjaSharp

MinjaSharp is a C# wrapper for the Minja templating engine, providing a clean and efficient interface for template rendering in .NET applications.

## What is Minja and How It Applies to LLMs

Minja is a lightweight, high-performance templating engine designed for efficient text generation. It's particularly well-suited for working with Large Language Models (LLMs) because:

1. It provides a flexible, Jinja-like syntax for creating prompts and templates
2. It supports complex logic, loops, and conditionals within templates
3. It can efficiently handle structured data for rendering

MinjaSharp brings these capabilities to the .NET ecosystem, allowing C# developers to work with Minja templates for LLM prompt engineering, text generation, and other templating needs without having to interact with the native C++ implementation directly.

## Usage

### Installation

```bash
dotnet add package MinjaSharp
```

### Creating a Template

Creating a template is straightforward:

```csharp
using MinjaSharp;

// Create a template with Jinja-like syntax
var template = new Template("Hello, {{ name }}! You have {{ unread }} unread message(s).");
```

### Rendering Templates

MinjaSharp provides multiple ways to render templates:

#### 1. Render with a typed object

The most convenient way to render templates is using strongly-typed C# objects:

```csharp
// Using anonymous type
var result = template.Render(new { 
    name = "World", 
    unread = 7 
});

// Or with a defined class
public class User {
    public string Name { get; set; }
    public int Unread { get; set; }
}

var user = new User { Name = "Alice", Unread = 3 };
var result = template.Render(user);
```

#### 2. Render with a JSON string

You can also provide context as a JSON string:

```csharp
var result = template.RenderJson("{\"name\":\"World\",\"unread\":7}");
```

#### 3. Render with direct value construction

For more control, you can build the template context manually:

```csharp
// Create values manually
var root = Value.Object();
root.Set("name", Value.String("World"));
root.Set("unread", Value.Int(7));

// Create a context
using var ctx = new Context(root);

// Render with the context
var result = template.Render(ctx);
```

### Working with Complex Structures

MinjaSharp handles nested objects, arrays, and complex structures seamlessly:

```csharp
var template = new Template(@"
{% for user in users %}
  {{ user.name }} is {{ user.age }} years old.
{% endfor %}
");

var result = template.Render(new {
    users = new[] {
        new { name = "Alice", age = 25 },
        new { name = "Bob", age = 30 },
        new { name = "Charlie", age = 35 }
    }
});
```

### LLM Chat Templates

MinjaSharp is particularly useful for working with LLM chat templates. Here's an example using a chat template for Qwen:

```csharp
// Create the template
var template = new Template(QwenChatTemplate);

// Create a chat request object
var request = new ChatRequest {
    Messages = [
        new ChatMessage { Role = "system", Content = "You are a helpful AI assistant." },
        new ChatMessage { Role = "user", Content = "Hello, how are you?" }
    ]
};

// Render the template to get the properly formatted prompt
var formattedPrompt = template.Render(request);
```

## Performance Considerations

When working with MinjaSharp, keep these performance tips in mind:

1. **Reuse Template Instances**: Creating a template involves parsing and compiling the template string, which is relatively expensive. For better performance, create templates once and reuse them for multiple renders.

   ```csharp
   // Good - Create once, reuse many times
   var template = new Template("Hello, {{ name }}!");
   
   // Render multiple times with different data
   for (int i = 0; i < 1000; i++) {
       template.Render(new { name = $"User{i}" });
   }
   ```

2. **Dispose Templates When Done**: Templates implement `IDisposable`, so use `using` statements or explicitly dispose them when they're no longer needed.

   ```csharp
   using (var template = new Template("Hello, {{ name }}!"))
   {
       var result = template.Render(new { name = "World" });
   } // Automatically disposed here
   ```

3. **Use Value Builder for Repeated Structures**: When repeatedly rendering with similar structures, consider building the value tree manually once and updating specific values for better performance.

## Building Locally

To build MinjaSharp locally:

1. **Prerequisites**:
   - .NET 9.0 SDK or later
   - C++ compiler (MSVC on Windows, GCC on Linux, or Clang on macOS)
   - CMake 3.14 or later

2. **Clone the Repository**:
   ```bash
   git clone https://github.com/phil-scott-78/minjasharp
   cd minjasharp
   ```

3. **Build the Native Library and .NET Wrapper**:
   ```powershell
   # On Windows
   .\build.ps1
   
   # On Linux/macOS
   ./build.sh
   ```

4. **Run Tests**:
   ```bash
   dotnet test
   ```

## License

MinjaSharp is released under the MIT License. See the LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.