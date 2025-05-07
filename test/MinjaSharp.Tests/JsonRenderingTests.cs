using System;
using Xunit;
using MinjaSharp;

namespace MinjaSharp.Tests
{
    public class JsonRenderingTests
    {
        [Fact]
        public void SimpleJsonRendersCorrectly()
        {
            using var template = new Template("Hello, {{ name }}!");
            var result = template.RenderJson("{\"name\":\"World\"}");
            
            Assert.Equal("Hello, World!", result);
        }

        [Fact]
        public void ComplexJsonRendersCorrectly()
        {
            using var template = new Template("Hello, {{ location }}! You have {{ unread }} unread message(s).");
            var result = template.RenderJson("{\"location\":\"World\",\"unread\":7}");
            
            Assert.Equal("Hello, World! You have 7 unread message(s).", result);
        }

        [Fact]
        public void NestedJsonRendersCorrectly()
        {
            using var template = new Template("{{ user.name }} has {{ user.points }} points");
            var result = template.RenderJson("{\"user\":{\"name\":\"Alice\",\"points\":100}}");
            
            Assert.Equal("Alice has 100 points", result);
        }

        [Fact]
        public void JsonArrayRendersCorrectly()
        {
            using var template = new Template("{% for item in items %}{{ item }}{% if not loop.last %}, {% endif %}{% endfor %}");
            var result = template.RenderJson("{\"items\":[\"Apple\",\"Banana\",\"Cherry\"]}");
            
            Assert.Equal("Apple, Banana, Cherry", result);
        }

        [Fact]
        public void EmptyJsonObjectRendersCorrectly()
        {
            using var template = new Template("{% if name %}Name: {{ name }}{% else %}No name provided{% endif %}");
            var result = template.RenderJson("{}");
            
            Assert.Equal("No name provided", result);
        }

        [Fact]
        public void InvalidJsonThrowsException()
        {
            using var template = new Template("Hello, {{ name }}!");
            
            Assert.Throws<MinjaJsonException>(() => template.RenderJson("invalid json"));
        }

        [Fact]
        public void DisposedTemplateThrowsExceptionWhenRenderingJson()
        {
            var template = new Template("test");
            template.Dispose();
            
            Assert.Throws<ObjectDisposedException>(() => template.RenderJson("{}"));
        }
    }
}