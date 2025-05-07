using System;
using Xunit;
using MinjaSharp;

namespace MinjaSharp.Tests
{
    public class TemplateTests
    {
        [Fact]
        public void SimpleTemplateRendersCorrectly()
        {
            using var template = new Template("Hello, {{ name }}!");
            
            var root = Value.Object();
            root.Set("name", Value.String("World"));

            using var ctx = new Context(root);
            var result = template.Render(ctx);

            Assert.Equal("Hello, World!", result);
        }

        [Fact]
        public void ComplexTemplateRendersCorrectly()
        {
            using var template = new Template("Hello, {{ location }}! You have {{ unread }} unread message(s).");
            
            var root = Value.Object();
            root.Set("location", Value.String("World"));
            root.Set("unread", Value.Int(7));

            using var ctx = new Context(root);
            var result = template.Render(ctx);

            Assert.Equal("Hello, World! You have 7 unread message(s).", result);
        }

        [Fact]
        public void TemplateWithJsonRendersCorrectly()
        {
            using var template = new Template("Hello, {{ name }}!");
            var result = template.RenderJson("{\"name\":\"World\"}");
            Assert.Equal("Hello, World!", result);
        }

        [Fact]
        public void NestedValuesRenderCorrectly()
        {
            using var template = new Template("{{ user.name }} has {{ user.points }} points");
            
            var user = Value.Object();
            user.Set("name", Value.String("Alice"));
            user.Set("points", Value.Int(100));
            
            var root = Value.Object();
            root.Set("user", user);

            using var ctx = new Context(root);
            var result = template.Render(ctx);

            Assert.Equal("Alice has 100 points", result);
        }

        [Fact]
        public void ArrayValuesRenderCorrectly()
        {
            using var template = new Template("{% for item in items %}{{ item }}{% if not loop.last %}, {% endif %}{% endfor %}");
            
            var items = Value.Array();
            items.Add(Value.String("Apple"));
            items.Add(Value.String("Banana"));
            items.Add(Value.String("Cherry"));
            
            var root = Value.Object();
            root.Set("items", items);

            using var ctx = new Context(root);
            var result = template.Render(ctx);

            Assert.Equal("Apple, Banana, Cherry", result);
        }

        [Fact]
        public void DisposedTemplateThrowsException()
        {
            var template = new Template("test");
            template.Dispose();

            using var root = Value.Object();
            using var ctx = new Context(root);
            
            Assert.Throws<ObjectDisposedException>(() => template.Render(ctx));
        }
    }
}