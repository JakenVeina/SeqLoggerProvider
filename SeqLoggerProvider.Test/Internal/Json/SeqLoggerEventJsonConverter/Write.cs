using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NUnit.Framework;
using Shouldly;

using SeqLoggerProvider.Internal;

using Uut = SeqLoggerProvider.Internal.Json.SeqLoggerEventJsonConverter;

namespace SeqLoggerProvider.Test.Internal.Json.SeqLoggerEventJsonConverter
{
    [TestFixture]
    internal class Write
    {
        public static TestCaseData CreateWriteTestCase<TState>(
            string                                  testName,
            int                                     occurred,
            LogLevel                                logLevel,
            TState                                  state,
            bool                                    omitCategoryName    = default,
            int                                     eventId             = default,
            bool                                    omitEventName       = default,
            List<object>?                           scopeStatesBuffer   = default,
            IReadOnlyDictionary<string, string>?    globalFields        = default,
            string?                                 exceptionMessage    = default,
            string?                                 message             = default)
        {
            return new TestCaseData(
                    new SeqLoggerEvent<TState>(
                        categoryName:       omitCategoryName ? string.Empty : $"SeqLoggerEventJsonConverter.Write.{testName}",
                        eventId:            new(eventId, omitEventName ? null : $"{testName}Executed"),
                        exception:          (exceptionMessage is not null)
                            ? TestException.Create(exceptionMessage)
                            : null,
                        formatter:          (_, _) => message ?? string.Empty,
                        logLevel:           logLevel,
                        occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(occurred).UtcDateTime,
                        scopeStatesBuffer:  scopeStatesBuffer ?? new List<object>(),
                        state:              state),
                    TestCaseAssetLoader.GetAssetPath($"WriteAssets/{testName}.json"),
                    globalFields)
                .SetName($"{{m}}({testName})");
        }

        public static IReadOnlyList<TestCaseData> Always_TestCaseData
            => new[]
            {
                CreateWriteTestCase(
                    testName:           "CategoryNameIsOmitted",
                    occurred:           1,
                    logLevel:           LogLevel.Critical,
                    state:              default(object?),
                    omitCategoryName:   true,
                    eventId:            2,
                    message:            "This test event contains no category name."),

                CreateWriteTestCase(
                    testName:           "DuplicateFieldsAcrossStatesAndGlobalFields",
                    occurred:           3,
                    logLevel:           LogLevel.Debug,
                    state:              new[]
                    {
                        new KeyValuePair<string, object?>("EventField1", "Field Value 1"),
                        new KeyValuePair<string, object?>("EventField2", "Field Value 2"),
                        new KeyValuePair<string, object?>("EventField3", "Field Value 3")
                    },
                    eventId:            4,
                    scopeStatesBuffer:  new List<object>()
                    {
                        new[]
                        {
                            new KeyValuePair<string, object?>("EventField2", "Field Value 4")
                        },
                        new[]
                        {
                            new KeyValuePair<string, object?>("EventField3", "Field Value 5"),
                            new KeyValuePair<string, object?>("EventField4", "Field Value 6")
                        }
                    },
                    globalFields:       new Dictionary<string, string>()
                    {
                        ["EventField3"] = "Field Value 7",
                        ["EventField4"] = "Field Value 8",
                        ["EventField5"] = "Field Value 9"
                    },
                    message:            "This test event contains a variety of duplicate fields."),

                CreateWriteTestCase(
                    testName:           "DuplicateFieldsWithinScopeState",
                    occurred:           5,
                    logLevel:           LogLevel.Error,
                    state:              default(object?),
                    eventId:            6,
                    scopeStatesBuffer:  new List<object>()
                    {
                        new[]
                        {
                            new KeyValuePair<string, object?>("EventField1", "Field Value 1"),
                            new KeyValuePair<string, object?>("EventField2", "Field Value 2"),
                            new KeyValuePair<string, object?>("EventField1", "Field Value 3")
                        }
                    },
                    message:            "This test event contains a scope state with duplicate fields."),

                CreateWriteTestCase(
                    testName:           "DuplicateFieldsWithinState",
                    occurred:           7,
                    logLevel:           LogLevel.Information,
                    state:              new[]
                    {
                        new KeyValuePair<string, object?>("EventField1", "Field Value 1"),
                        new KeyValuePair<string, object?>("EventField2", "Field Value 2"),
                        new KeyValuePair<string, object?>("EventField1", "Field Value 3")
                    },
                    eventId:            8,
                    message:            "This test event contains a state with duplicate fields."),

                CreateWriteTestCase(
                    testName:           "EventIdIsOmitted",
                    occurred:           9,
                    logLevel:           LogLevel.Trace,
                    state:              default(object?),
                    message:            "This test event contains a state with no event ID field."),

                CreateWriteTestCase(
                    testName:           "EventNameIsOmitted",
                    occurred:           10,
                    logLevel:           LogLevel.Warning,
                    state:              default(object?),
                    eventId:            11,
                    omitEventName:      true,
                    message:            "This test event contains a state with no event name field."),

                CreateWriteTestCase(
                    testName:           "ExceptionIsGiven",
                    occurred:           12,
                    logLevel:           LogLevel.Critical,
                    state:              default(object?),
                    eventId:            13,
                    exceptionMessage:   "This is a test exception.",
                    message:            "This test event contains an exception."),

                CreateWriteTestCase(
                    testName:           "ManyStatesAndGlobalFields",
                    occurred:           14,
                    logLevel:           LogLevel.Debug,
                    state:              new[]
                    {
                        new KeyValuePair<string, object?>("StateField1", "State Value 1"),
                        new KeyValuePair<string, object?>("StateField2", 15),
                        new KeyValuePair<string, object?>("StateField3", new
                        {
                            ObjectProperty1 = "State Value 2",
                            ObjectProperty2 = "State Value 3"
                        }),
                    },
                    eventId:            15,
                    scopeStatesBuffer:  new List<object>()
                    {
                        new[]
                        {
                            new KeyValuePair<string, object?>("ScopeStateField1", "State Value 4"),
                        },
                        new[]
                        {
                            new KeyValuePair<string, object?>("ScopeStateField2", true),
                            new KeyValuePair<string, object?>("ScopeStateField3", new[]
                            {
                                "Array Value 1",
                                "Array Value 2",
                                "Array Value 3"
                            }),
                        },
                        new
                        {
                            ScopeStateProperty1 = "State Value 5",
                            ScopeStateProperty2 = "State Value 6",
                            ScopeStateProperty3 = "State Value 7"
                        }
                    },
                    globalFields:       new Dictionary<string, string>()
                    {
                        ["GlobalField1"] = "State Value 8",
                        ["GlobalField2"] = "State Value 9",
                        ["GlobalField3"] = "State Value 10"
                    },
                    message:            "This test event contains a variety of state fields, from event state, scoped states, and global fields configuration."),

                CreateWriteTestCase(
                    testName:           "NoStates",
                    occurred:           16,
                    logLevel:           LogLevel.Error,
                    state:              default(object?),
                    eventId:            17,
                    message:            "This test event contains no state data."),

                CreateWriteTestCase(
                    testName:           "ScopeStateIncludesMessageTemplate",
                    occurred:           18,
                    logLevel:           LogLevel.Information,
                    state:              default(object?),
                    eventId:            19,
                    scopeStatesBuffer:  new List<object>()
                    {
                        new[]
                        {
                            new KeyValuePair<string, object?>("{OriginalFormat}", "This test event contains a scope-state fieldset with a message template field")
                        }
                    },
                    message:            "This test event contains a scope-state fieldset with a message template field"),

                CreateWriteTestCase(
                    testName:           "ScopeStateIsArray",
                    occurred:           20,
                    logLevel:           LogLevel.Trace,
                    state:              default(object?),
                    eventId:            21,
                    scopeStatesBuffer:  new List<object>()
                    {
                        new[]
                        {
                            "Array Value 1",
                            "Array Value 2",
                            "Array Value 3"
                        }
                    },
                    message:            "This test event contains a scope-state object that is an array."),

                CreateWriteTestCase(
                    testName:           "ScopeStateIsEmptyFieldset",
                    occurred:           22,
                    logLevel:           LogLevel.Warning,
                    state:              default(object?),
                    eventId:            23,
                    scopeStatesBuffer:  new List<object>()
                    {
                        Array.Empty<KeyValuePair<string, object?>>()
                    },
                    message:            "This test event contains a scope-state fieldset with no fields."),

                CreateWriteTestCase(
                    testName:           "ScopeStateIsFieldset",
                    occurred:           24,
                    logLevel:           LogLevel.Critical,
                    state:              default(object?),
                    eventId:            25,
                    scopeStatesBuffer:  new List<object>()
                    {
                        new[]
                        {
                            new KeyValuePair<string, object?>("StateField1", "State Value 1"),
                            new KeyValuePair<string, object?>("StateField2", 26),
                            new KeyValuePair<string, object?>("StateField3", true)
                        }
                    },
                    message:            "This test event contains a scope-state fieldset with a variety of fields"),

                CreateWriteTestCase(
                    testName:           "ScopeStateIsNumber",
                    occurred:           27,
                    logLevel:           LogLevel.Debug,
                    state:              default(object?),
                    eventId:            28,
                    scopeStatesBuffer:  new List<object>()
                    {
                        29
                    },
                    message:            "This test event contains a scope-state value that is a number"),

                CreateWriteTestCase(
                    testName:           "ScopeStateIsObject",
                    occurred:           30,
                    logLevel:           LogLevel.Error,
                    state:              default(object?),
                    eventId:            31,
                    scopeStatesBuffer:  new List<object>()
                    {
                        new
                        {
                            ObjectProperty1 = "Object Value 1",
                            ObjectProperty2 = 32,
                            ObjectProperty3 = false,
                        }
                    },
                    message:            "This test event contains a scope-state object with properties"),

                CreateWriteTestCase(
                    testName:           "ScopeStateIsString",
                    occurred:           33,
                    logLevel:           LogLevel.Information,
                    state:              default(object?),
                    eventId:            34,
                    scopeStatesBuffer:  new List<object>()
                    {
                        "EventState"
                    },
                    message:            "This test event contains a scope-state value that is a string"),

                CreateWriteTestCase(
                    testName:           "ScopeStatesAreFieldsets",
                    occurred:           35,
                    logLevel:           LogLevel.Trace,
                    state:              default(object?),
                    eventId:            36,
                    scopeStatesBuffer:  new List<object>()
                    {
                        new[]
                        {
                            new KeyValuePair<string, object?>("ScopeStateField1", "Value 1")
                        },
                        new[]
                        {
                            new KeyValuePair<string, object?>("ScopeStateField2", "Value 2"),
                            new KeyValuePair<string, object?>("ScopeStateField3", "Value 3")
                        },
                        new[]
                        {
                            new KeyValuePair<string, object?>("ScopeStateField4", "Value 4"),
                            new KeyValuePair<string, object?>("ScopeStateField5", "Value 5"),
                            new KeyValuePair<string, object?>("ScopeStateField6", "Value 6")
                        }
                    },
                    message:            "This test event contains scope state fieldset objects"),

                CreateWriteTestCase(
                    testName:           "ScopeStatesAreNotFieldsets",
                    occurred:           37,
                    logLevel:           LogLevel.Warning,
                    state:              default(object?),
                    eventId:            38,
                    scopeStatesBuffer:  new List<object>()
                    {
                        new
                        {
                            ScopeStateField1 = "Value 1"
                        },
                        new
                        {
                            ScopeStateField2 = "Value 2",
                            ScopeStateField3 = "Value 3"
                        },
                        new
                        {
                            ScopeStateField4 = "Value 4",
                            ScopeStateField5 = "Value 5",
                            ScopeStateField6 = "Value 6"
                        }
                    },
                    message:            "This test event contains scope state objects that are not fieldsets"),

                CreateWriteTestCase(
                    testName:           "StateIncludesMessageTemplate",
                    occurred:           39,
                    logLevel:           LogLevel.Critical,
                    state:              new[]
                    {
                        new KeyValuePair<string, object?>("{OriginalFormat}", "This test event contains a state fieldset with a message template field")
                    },
                    eventId:            40,
                    message:            "This test event contains a state fieldset with a message template field"),

                CreateWriteTestCase(
                    testName:           "StateIsArray",
                    occurred:           41,
                    logLevel:           LogLevel.Debug,
                    state:              new[]
                    {
                        "Array Value 1",
                        "Array Value 2",
                        "Array Value 3"
                    },
                    eventId:            42,
                    message:            "This test event contains a state object that is an array."),

                CreateWriteTestCase(
                    testName:           "StateIsEmptyFieldset",
                    occurred:           43,
                    logLevel:           LogLevel.Error,
                    state:              Array.Empty<KeyValuePair<string, object?>>(),
                    eventId:            44,
                    message:            "This test event contains a state fieldset with no fields."),

                CreateWriteTestCase(
                    testName:           "StateIsFieldset",
                    occurred:           45,
                    logLevel:           LogLevel.Information,
                    state:              new[]
                    {
                        new KeyValuePair<string, object?>("StateField1", "State Value 1"),
                        new KeyValuePair<string, object?>("StateField2", 46),
                        new KeyValuePair<string, object?>("StateField3", true)
                    },
                    eventId:            47,
                    message:            "This test event contains a state fieldset with a variety of fields"),

                CreateWriteTestCase(
                    testName:           "StateIsNumber",
                    occurred:           48,
                    logLevel:           LogLevel.Trace,
                    state:              49,
                    eventId:            50,
                    message:            "This test event contains a state value that is a number"),

                CreateWriteTestCase(
                    testName:           "StateIsObject",
                    occurred:           51,
                    logLevel:           LogLevel.Warning,
                    state:              new
                    {
                        ObjectProperty1 = "Object Value 1",
                        ObjectProperty2 = 52,
                        ObjectProperty3 = false,
                    },
                    eventId:            53,
                    message:            "This test event contains a state object with properties"),

                CreateWriteTestCase(
                    testName:           "StateIsString",
                    occurred:           54,
                    logLevel:           LogLevel.Critical,
                    state:              "EventState",
                    eventId:            55,
                    message:            "This test event contains a state value that is a string"),
            };

        [TestCaseSource(nameof(Always_TestCaseData))]
        public async Task Always_ResultIsCorrect(
            ISeqLoggerEvent                         value,
            string                                  expectedDocumentAssetPath,
            IReadOnlyDictionary<string, string>?    globalFields)
        {
            var seqLoggerOptions = FakeOptions.Create(new SeqLoggerOptions()
            {
                GlobalFields = globalFields
            });

            var uut = new Uut(seqLoggerOptions);

            var options = new JsonSerializerOptions()
            {
                WriteIndented = true
            };
            options.Converters.Add(uut);

            var resultText = JsonSerializer.Serialize(value, options);

            Console.WriteLine(resultText);

            var resultDocument = JsonDocument.Parse(resultText);

            resultDocument.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);

            var expectedDocument = JsonDocument.Parse(
                    await File.ReadAllTextAsync(expectedDocumentAssetPath));

            CompareJsonObjects(resultDocument.RootElement, expectedDocument.RootElement);
        }

        private static void CompareJsonArrays(JsonElement result, JsonElement expected)
        {
            result.GetArrayLength().ShouldBe(expected.GetArrayLength());

            foreach (var (resultElement, expectedElement) in result.EnumerateArray().Zip(expected.EnumerateArray()))
                CompareJsonElements(resultElement, expectedElement);
        }

        private static void CompareJsonElements(JsonElement result, JsonElement expected)
        {
            switch (expected)
            {
                case { ValueKind: JsonValueKind.Object }:
                    CompareJsonObjects(result, expected);
                    break;

                case { ValueKind: JsonValueKind.Array }:
                    CompareJsonArrays(result, expected);
                    break;

                case { ValueKind: JsonValueKind.String }:
                    result.GetString().ShouldBe(expected.GetString());
                    break;

                case { ValueKind: JsonValueKind.Number }:
                    result.GetRawText().ShouldBe(expected.GetRawText());
                    break;
            }
        }

        private static void CompareJsonObjects(JsonElement result, JsonElement expected)
        {
            var resultProperties = result
                .EnumerateObject()
                .ToArray();

            var expectedPropertiesByName = expected
                .EnumerateObject()
                .ToDictionary(property => property.Name);

            resultProperties.Select(property => property.Name).ShouldBe(expectedPropertiesByName.Keys, ignoreOrder: true);
            foreach (var resultProperty in resultProperties)
            {
                var expectedProperty = expectedPropertiesByName[resultProperty.Name];

                CompareJsonElements(resultProperty.Value, expectedProperty.Value);
            }
        }
    }
}
