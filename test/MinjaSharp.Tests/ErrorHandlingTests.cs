using System;
using Xunit;
using MinjaSharp;

namespace MinjaSharp.Tests
{
    public class ErrorHandlingTests
    {
        [Fact]
        public void TemplateParsingError_ThrowsMinjaParseException_WithDetailedMessage()
        {
            // Syntax error in template - unclosed variable tag
            var exception = Assert.Throws<MinjaParseException>(() => new Template("Hello, {{ name"));
            
            // Verify exception contains line and column information
            Assert.Contains("ERROR:", exception.Message);
            Assert.Contains("Template parsing failed", exception.Message);
            Assert.Contains("at row", exception.Message);
            Assert.Contains("column", exception.Message);
            Assert.Contains("^", exception.Message); // Should contain the visual pointer
        }

        [Fact]
        public void UnclosedBlockError_ThrowsMinjaParseException_WithDetailedMessage()
        {
            // Unclosed for block
            var exception = Assert.Throws<MinjaParseException>(() => new Template("{% for item in items %}{{ item }}"));
            
            // Verify exception contains helpful error details
            Assert.Contains("ERROR:", exception.Message);
            Assert.Contains("Template parsing failed", exception.Message);
            Assert.Contains("for", exception.Message); // Should mention the unclosed block
        }

        [Fact]
        public void InvalidSyntaxError_ThrowsMinjaParseException_WithDetailedMessage()
        {
            // Invalid syntax in an expression
            var exception = Assert.Throws<MinjaParseException>(() => new Template("{{ items[[ }}"));
            
            // Verify exception contains line and column information
            Assert.Contains("ERROR:", exception.Message);
            Assert.Contains("Template parsing failed", exception.Message);
            Assert.Contains("at row", exception.Message);
            Assert.Contains("column", exception.Message);
        }

        [Fact]
        public void JsonParsingError_ThrowsMinjaJsonException_WithDetailedMessage()
        {
            using var template = new Template("Hello, {{ name }}!");
            
            // Invalid JSON
            var exception = Assert.Throws<MinjaJsonException>(() => template.RenderJson("{ invalid json }"));
            
            // Verify exception contains helpful error details
            Assert.Contains("ERROR:", exception.Message);
            Assert.Contains("JSON parse error", exception.Message);
        }

        [Fact]
        public void MissingVariableError_ThrowsMinjaRenderException_WithDetailedMessage()
        {
            // Reference to a non-existent variable in a template
            using var template = new Template("{{ missing_variable.property }}");
            
            using var ctx = Context.From(new { existing = "value" });
            var exception = Assert.Throws<MinjaRenderException>(() => template.Render(ctx));
            
            // Verify exception contains helpful error details
            Assert.Contains("ERROR:", exception.Message);
            Assert.Contains("Template rendering failed", exception.Message);
            Assert.Contains("missing_variable", exception.Message);
        }
    }
}