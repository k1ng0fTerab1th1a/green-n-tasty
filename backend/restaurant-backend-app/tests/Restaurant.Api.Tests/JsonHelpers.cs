using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Restaurant.Api.Tests
{
    internal static class JsonHelpers
    {
        public static JsonElement GetPropertyIgnoreCase(this JsonElement element, string propertyName)
        {
            foreach (var p in element.EnumerateObject())
            {
                if (string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    return p.Value;
            }

            throw new KeyNotFoundException($"Property '{propertyName}' was not found. Available: [{string.Join(", ", element.EnumerateObject().Select(x => x.Name))}]");
        }
    }
}
