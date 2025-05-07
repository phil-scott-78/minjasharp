using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MinjaSharp.Tests
{
    public class Qwen3ChatRequest
    {
        [JsonPropertyName("messages")]
        public List<ChatMessage> Messages { get; set; } = [];
        
        [JsonPropertyName("tools")]
        public List<Tool>? Tools { get; set; }
        
        [JsonPropertyName("add_generation_prompt")]
        public bool? AddGenerationPrompt { get; set; }
        
        [JsonPropertyName("enable_thinking")]
        public bool? EnableThinking { get; set; }
    }

    public class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;
        
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
        
        [JsonPropertyName("reasoning_content")]
        public string? ReasoningContent { get; set; }
        
        [JsonPropertyName("tool_calls")]
        public List<ToolCall>? ToolCalls { get; set; }
    }

    public class Tool
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        
        [JsonPropertyName("parameters")]
        public ToolParameters Parameters { get; set; } = new ToolParameters();
    }

    public class ToolParameters
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "object";
        
        [JsonPropertyName("properties")]
        public Dictionary<string, ToolProperty> Properties { get; set; } = new Dictionary<string, ToolProperty>();
        
        [JsonPropertyName("required")]
        public List<string>? Required { get; set; }
    }

    public class ToolProperty
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("enum")]
        public List<string>? Enum { get; set; }
    }

    public class ToolCall
    {
        [JsonPropertyName("function")]
        public FunctionCall? Function { get; set; }
    }

    public class FunctionCall
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("arguments")]
        public string Arguments { get; set; } = string.Empty;
    }
}