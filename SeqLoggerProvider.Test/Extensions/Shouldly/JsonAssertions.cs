using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Shouldly
{
    public static class JsonAssertions
    {
        public static void ShouldMatch(
                this JsonElement        actual,
                JsonElement             expected,
                IEnumerable<string>?    ignoredPaths = null)
            => actual.ShouldMatchInternal(expected,
                currentPath:    string.Empty,
                ignoredPaths:   (ignoredPaths is null)
                    ? Array.Empty<string>()
                    : ignoredPaths.ToHashSet());

        private static void ShouldMatchInternal(
            this JsonElement            actual,
            JsonElement                 expected,
            string                      currentPath,
            IReadOnlyCollection<string> ignoredPaths)
        {
            if (ignoredPaths.Contains(currentPath))
                return;

            actual.ValueKind.ShouldBe(expected.ValueKind);

            switch(actual.ValueKind)
            {
                case JsonValueKind.Array:
                    actual.GetArrayLength().ShouldBe(expected.GetArrayLength());
                    var i = 0;
                    foreach (var (actualElement, expectedElement) in actual.EnumerateArray().Zip(expected.EnumerateArray()))
                    {
                        actualElement.ShouldMatchInternal(expectedElement,
                            currentPath:    BuildArrayElementPath(currentPath, i),
                            ignoredPaths:   ignoredPaths);
                        
                        ++i;
                    }
                    break;
                
                case JsonValueKind.Number:
                    actual.GetDecimal().ShouldBe(expected.GetDecimal());
                    break;
                
                case JsonValueKind.Object:
                    var actualProperties = actual.EnumerateObject()
                        .ToArray();

                    actualProperties.Select(static property => property.Name).ShouldBe(expected.EnumerateObject().Select(static property => property.Name), ignoreOrder: true);

                    foreach(var actualProperty in actualProperties)
                        actualProperty.Value.ShouldMatchInternal(expected.GetProperty(actualProperty.Name),
                            currentPath:    BuildPropertyPath(currentPath, actualProperty.Name),
                            ignoredPaths:   ignoredPaths);
                    break;
                
                case JsonValueKind.String:
                    actual.GetString().ShouldBe(expected.GetString());
                    break;
            }

            static string BuildArrayElementPath(string currentPath, int elementIndex)
                => $"{currentPath}[{elementIndex}]";

            static string BuildPropertyPath(string currentPath, string elementName)
            {
                var escapedElementName = elementName.Contains('.')
                ? $"\"{elementName}\""
                : elementName;

                return (currentPath.Length is 0)
                    ? escapedElementName
                    : $"{currentPath}.{escapedElementName}";
            }

        }
    }
}
