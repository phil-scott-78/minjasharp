using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Xunit;
using MinjaSharp;

namespace MinjaSharp.Tests
{
    public class ValueBuilderTests
    {
        [Fact]
        public void PrimitiveValuesConvertCorrectly()
        {
            using var template = new Template("string: {{ string }}, int: {{ int }}, long: {{ long }}, double: {{ double }}, bool: {{ bool }}");
            
            var result = template.Render(new
            {
                @string = "test",
                @int = 42,
                @long = 9223372036854775807L,
                @double = 3.14159,
                @bool = true
            });
            Assert.Equal("string: test, int: 42, long: 9223372036854775807, double: 3.14159, bool: true", result);
        }

        [Fact]
        public void NullValueConvertsCorrectly()
        {
            using var template = new Template("Value is: {% if value %}not null{% else %}null{% endif %}");
            var result = template.Render(new
            {
                value = (string?)null
            });
            Assert.Equal("Value is: null", result);
        }

        [Fact]
        public void DictionaryConvertsCorrectly()
        {
            var dictionary = new Dictionary<string, object>
            {
                ["name"] = "Alice",
                ["age"] = 30,
                ["isActive"] = true
            };

            using var template = new Template("{{ name }} is {{ age }} years old and {% if isActive %}active{% else %}inactive{% endif %}");
            var result = template.Render(dictionary);
            Assert.Equal("Alice is 30 years old and active", result);
        }

        [Fact]
        public void CollectionConvertsCorrectly()
        {
            var list = new List<string> { "Apple", "Banana", "Cherry" };

            using var template = new Template("{% for item in items %}{{ item }}{% if not loop.last %}, {% endif %}{% endfor %}");

            var result = template.Render(new { items = list });
            Assert.Equal("Apple, Banana, Cherry", result);
        }

        [Fact]
        public void NestedObjectsConvertCorrectly()
        {
            using var template = new Template("{{ user.name }} has {{ user.profile.points }} points");
  
            var result = template.Render(new
            {
                user = new
                {
                    name = "Bob",
                    profile = new
                    {
                        points = 150
                    }
                }
            });
            Assert.Equal("Bob has 150 points", result);
        }

        [Fact]
        public void ArrayOfObjectsConvertsCorrectly()
        {
            using var template = new Template("{% for user in users %}{{ user.name }}: {{ user.age }}{% if not loop.last %}, {% endif %}{% endfor %}");
            
            var result = template.Render(new
            {
                users = new[]
                {
                    new { name = "Alice", age = 25 },
                    new { name = "Bob", age = 30 },
                    new { name = "Charlie", age = 35 }
                }
            });
            Assert.Equal("Alice: 25, Bob: 30, Charlie: 35", result);
        }

        [Fact]
        public void JsonPropertyNameAttributesAreRespected()
        {
            var testObject = new TestJsonPropertyClass
            {
                RegularName = "Regular",
                CustomPropertyName = "Custom"
            };
            
            using var template = new Template("{{ regularname }}, {{ custom_name }}");
            var result = template.Render(testObject);
            Assert.Equal("Regular, Custom", result);
        }
        
        private class TestJsonPropertyClass
        {
            public string RegularName { get; set; } = string.Empty;
            
            [JsonPropertyName("custom_name")]
            public string CustomPropertyName { get; set; } = string.Empty;
        }
    }
}