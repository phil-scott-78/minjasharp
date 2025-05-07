using System;
using System.Collections.Generic;
using Xunit;
using MinjaSharp;

namespace MinjaSharp.Tests
{
    public sealed class Qwen3Fixture : IDisposable
    {
        public Template Template { get; } = new(QwenChatTemplate.QwenTemplate);

        public void Dispose()
        {
            Template.Dispose();
        }
    }
    
    public class Qwen3TemplateTests(Qwen3Fixture fixture) : IClassFixture<Qwen3Fixture>
    {
        private readonly Template _template = fixture.Template;

        [Fact]
        public void RenderSystemMessageOnly()
        {
            // Create a model object instead of using raw JSON
            var request = new Qwen3ChatRequest
            {
                Messages = [new ChatMessage { Role = "system", Content = "You are a helpful AI assistant." }]
            };

            // Use the Render<T> method instead of RenderJson
            var result = _template.Render(request);
            
            var expected = "<|im_start|>system\nYou are a helpful AI assistant.<|im_end|>\n";
            Assert.Equal(expected, result);
        }

        [Fact]
        public void RenderUserMessageAfterSystem()
        {
            var request = new Qwen3ChatRequest
            {
                Messages =
                [
                    new ChatMessage { Role = "system", Content = "You are a helpful AI assistant." },
                    new ChatMessage { Role = "user", Content = "Hello, how are you?" }
                ]
            };

            var result = _template.Render(request);
            
            var expected = "<|im_start|>system\nYou are a helpful AI assistant.<|im_end|>\n" +
                           "<|im_start|>user\nHello, how are you?<|im_end|>\n";
            Assert.Equal(expected, result);
        }

        [Fact]
        public void RenderAssistantResponse()
        {
            var request = new Qwen3ChatRequest
            {
                Messages =
                [
                    new ChatMessage { Role = "system", Content = "You are a helpful AI assistant." },
                    new ChatMessage { Role = "user", Content = "Hello, how are you?" },
                    new ChatMessage
                    {
                        Role = "assistant", Content = "I'm doing well, thank you for asking! How can I help you today?"
                    }
                ]
            };

            var result = _template.Render(request);

            var expected = """
                           <|im_start|>system
                           You are a helpful AI assistant.<|im_end|>
                           <|im_start|>user
                           Hello, how are you?<|im_end|>
                           <|im_start|>assistant
                           <think>
                           
                           </think>
                           
                           I'm doing well, thank you for asking! How can I help you today?<|im_end|>

                           """;
            Assert.Equal(expected, result);
        }

        [Fact]
        public void RenderComplexConversation()
        {
            var request = new Qwen3ChatRequest
            {
                Messages =
                [
                    new ChatMessage { Role = "system", Content = "You are a helpful AI assistant." },
                    new ChatMessage { Role = "user", Content = "Hello, how are you?" },
                    new ChatMessage
                    {
                        Role = "assistant", Content = "I'm doing well, thank you for asking! How can I help you today?"
                    },
                    new ChatMessage { Role = "user", Content = "What's the weather like?" },
                    new ChatMessage
                    {
                        Role = "assistant",
                        Content =
                            "I don't have real-time access to weather data. To get the current weather, you would need to check a weather service or app."
                    }
                ]
            };

            var result = _template.Render(request);

            var expected = """
                           <|im_start|>system
                           You are a helpful AI assistant.<|im_end|>
                           <|im_start|>user
                           Hello, how are you?<|im_end|>
                           <|im_start|>assistant
                           I'm doing well, thank you for asking! How can I help you today?<|im_end|>
                           <|im_start|>user
                           What's the weather like?<|im_end|>
                           <|im_start|>assistant
                           <think>
                           
                           </think>
                           
                           I don't have real-time access to weather data. To get the current weather, you would need to check a weather service or app.<|im_end|>

                           """;
            Assert.Equal(expected, result);
        }
        
        [Fact]
        public void RenderWithToolCalls()
        {
            var request = new Qwen3ChatRequest
            {
                Tools =
                [
                    new Tool
                    {
                        Name = "weather",
                        Description = "Get the current weather for a location",
                        Parameters = new ToolParameters
                        {
                            Type = "object",
                            Properties = new Dictionary<string, ToolProperty>
                            {
                                ["location"] = new ToolProperty
                                {
                                    Type = "string",
                                    Description = "The location to get weather for"
                                }
                            },
                            Required = ["location"]
                        }
                    }
                ],
                Messages =
                [
                    new ChatMessage
                        { Role = "system", Content = "You are a helpful AI assistant with access to tools." },
                    new ChatMessage { Role = "user", Content = "What's the weather in Seattle?" },
                    new ChatMessage
                    {
                        Role = "assistant",
                        Content = "I'll check the weather for you.",
                        ToolCalls =
                        [
                            new ToolCall
                            {
                                Function = new FunctionCall
                                {
                                    Name = "weather",
                                    Arguments = "{\"location\":\"Seattle\"}"
                                }
                            }
                        ]
                    },

                    new ChatMessage { Role = "tool", Content = "{\"temperature\": 52, \"conditions\": \"Cloudy\"}" }
                ]
            };

            var result = _template.Render(request);
            
            // Check for key elements in the output
            Assert.Contains("<|im_start|>system", result);
            Assert.Contains("<tools>", result);
            Assert.Contains("weather", result);
            Assert.Contains("<tool_call>", result);
            Assert.Contains("<tool_response>", result);
        }
    }
}