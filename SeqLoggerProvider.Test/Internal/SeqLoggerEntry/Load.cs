using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using NUnit.Framework;
using Shouldly;

using Uut = SeqLoggerProvider.Internal.SeqLoggerEntry;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerEntry
{
    [TestFixture]
    public class Load
    {
        private static TestCaseData CreateAlwaysTestCaseData(
                string                                  testName,
                string                                  categoryName,
                EventId                                 eventId,
                Exception?                              exception,
                IReadOnlyDictionary<string, string>?    globalFields,
                LogLevel                                logLevel,
                string                                  message,
                DateTime                                occurredUtc,
                IReadOnlyList<object?>                  scopeStates,
                object?                                 state)
            => new TestCaseData(
                    testName,
                    categoryName,
                    eventId,
                    exception,
                    globalFields,
                    logLevel,
                    message,
                    occurredUtc,
                    scopeStates,
                    state)
                .SetName($"{{m}}({testName})");

        public static IReadOnlyList<TestCaseData> Always_TestCaseData
            => new[]
            {
                CreateAlwaysTestCaseData(
                    testName:           "CategoryNameIsOmitted",
                    categoryName:       "",
                    eventId:            new(1, "Event1"),
                    exception:          null,
                    globalFields:       null,
                    logLevel:           LogLevel.Trace,
                    message:            "This test event contains no category name.",
                    occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(1).UtcDateTime,
                    scopeStates:        Array.Empty<object?>(),
                    state:              null),

                CreateAlwaysTestCaseData(
                    testName:           "DuplicateFieldsAcrossStatesAndGlobalFields",
                    categoryName:       "Category1",
                    eventId:            new(2, "Event2"),
                    exception:          null,
                    message:            "This test event contains a variety of duplicate fields.",
                    globalFields:       new Dictionary<string, string>()
                    {
                        ["EventField3"] = "Field Value 7",
                        ["EventField4"] = "Field Value 8",
                        ["EventField5"] = "Field Value 9"
                    },
                    logLevel:           LogLevel.Debug,
                    occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(2).UtcDateTime,
                    scopeStates:        new[]
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
                    state:              new[]
                    {
                        new KeyValuePair<string, object?>("EventField1", "Field Value 1"),
                        new KeyValuePair<string, object?>("EventField2", "Field Value 2"),
                        new KeyValuePair<string, object?>("EventField3", "Field Value 3")
                    }),


                CreateAlwaysTestCaseData(
                    testName:           "DuplicateFieldsWithinScopeState",
                    categoryName:       "Category2",
                    eventId:            new(3, "Event3"),
                    exception:          null,
                    globalFields:       null,
                    logLevel:           LogLevel.Information,
                    message:            "This test event contains a scope state with duplicate fields.",
                    occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(3).UtcDateTime,
                    scopeStates:        new[]
                    {
                        new[]
                        {
                            new KeyValuePair<string, object?>("EventField1", "Field Value 1"),
                            new KeyValuePair<string, object?>("EventField2", "Field Value 2"),
                            new KeyValuePair<string, object?>("EventField1", "Field Value 3")
                        }
                    },
                    state:              default),

                CreateAlwaysTestCaseData(
                    testName:           "DuplicateFieldsWithinState",
                    categoryName:       "Category3",
                    eventId:            new(4, "Event4"),
                    exception:          null,
                    globalFields:       null,
                    logLevel:           LogLevel.Warning,
                    message:            "This test event contains a state with duplicate fields.",
                    occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(4).UtcDateTime,
                    scopeStates:        Array.Empty<object?>(),
                    state:              new[]
                    {
                        new KeyValuePair<string, object?>("EventField1", "Field Value 1"),
                        new KeyValuePair<string, object?>("EventField2", "Field Value 2"),
                        new KeyValuePair<string, object?>("EventField1", "Field Value 3")
                    }),

                CreateAlwaysTestCaseData(
                    testName:           "EventIdIsOmitted",
                    categoryName:       "Category4",
                    eventId:            new(0, "Event5"),
                    exception:          null,
                    globalFields:       null,
                    logLevel:           LogLevel.Error,
                    message:            "This test event contains a state with no event ID field.",
                    occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(5).UtcDateTime,
                    scopeStates:        Array.Empty<object?>(),
                    state:              null),

                CreateAlwaysTestCaseData(
                    testName:           "EventNameIsOmitted",
                    categoryName:       "Category5",
                    eventId:            new(5, string.Empty),
                    exception:          null,
                    globalFields:       null,
                    logLevel:           LogLevel.Critical,
                    message:            "This test event contains a state with no event name field.",
                    occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(6).UtcDateTime,
                    scopeStates:        Array.Empty<object?>(),
                    state:              null),

                CreateAlwaysTestCaseData(
                    testName:           "ExceptionIsGiven",
                    categoryName:       "Category6",
                    eventId:            new(6, "Event6"),
                    exception:          TestException.Create("This is a test exception."),
                    globalFields:       null,
                    logLevel:           LogLevel.Trace,
                    message:            "This test event contains an exception.",
                    occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(7).UtcDateTime,
                    scopeStates:        Array.Empty<object?>(),
                    state:              null),

                CreateAlwaysTestCaseData(
                    testName:           "ManyStatesAndGlobalFields",
                    categoryName:       "Category7",
                    eventId:            new(7, "Event7"),
                    exception:          null,
                    globalFields:       new Dictionary<string, string>()
                    {
                        ["GlobalField1"] = "State Value 8",
                        ["GlobalField2"] = "State Value 9",
                        ["GlobalField3"] = "State Value 10"
                    },
                    logLevel:           LogLevel.Debug,
                    message:            "This test event contains a variety of state fields, from event state, scoped states, and global fields configuration.",
                    occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(8).UtcDateTime,
                    scopeStates:        new object?[]
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
                    state:              new[]
                    {
                        new KeyValuePair<string, object?>("StateField1", "State Value 1"),
                        new KeyValuePair<string, object?>("StateField2", 9),
                        new KeyValuePair<string, object?>("StateField3", new
                        {
                            ObjectProperty1 = "State Value 2",
                            ObjectProperty2 = "State Value 3"
                        }),
                    }),

                CreateAlwaysTestCaseData(
                    testName:           "NoStates",
                    categoryName:       "Category8",
                    eventId:            new(8, "Event8"),
                    exception:          null,
                    globalFields:       null,
                    logLevel:           LogLevel.Information,
                    message:            "This test event contains no state data.",
                    occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(9).UtcDateTime,
                    scopeStates:        Array.Empty<object?>(),
                    state:              null),

                CreateAlwaysTestCaseData(
                    testName:           "ScopeStateIncludesMessageTemplate",
                    categoryName:       "Category9",
                    eventId:            new(9, "Event9"),
                    exception:          null,
                    globalFields:       null,
                    logLevel:           LogLevel.Warning,
                    message:            "This test event contains a scope-state fieldset with a message template field.",
                    occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(10).UtcDateTime,
                    scopeStates:        new[]
                    {
                        new[]
                        {
                            new KeyValuePair<string, object?>("{OriginalFormat}", "This test event contains a scope-state fieldset with a message template field")
                        }
                    },
                    state:              null),

                CreateAlwaysTestCaseData(
                    testName:           "ScopeStateIsArray",
                    categoryName:       "Category10",
                    eventId:            new(10, "Event10"),
                    exception:          null,
                    globalFields:       null,
                    logLevel:           LogLevel.Error,
                    message:            "This test event contains a scope-state object that is an array.",
                    occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(11).UtcDateTime,
                    scopeStates:        new[]
                    {
                        new[]
                        {
                            "Array Value 1",
                            "Array Value 2",
                            "Array Value 3"
                        }
                    },
                    state:              null),

                CreateAlwaysTestCaseData(
                    testName:           "ScopeStateIsEmptyFieldset",
                    categoryName:       "Category11",
                    eventId:            new(11, "Event11"),
                    exception:          null,
                    globalFields:       null,
                    logLevel:           LogLevel.Critical,
                    message:            "This test event contains a scope-state fieldset with no fields.",
                    occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(12).UtcDateTime,
                    scopeStates:        new[]
                    {
                        Array.Empty<KeyValuePair<string, object?>>()
                    },
                    state:              null),

                CreateAlwaysTestCaseData(
                    testName:           "ScopeStateIsFieldset",
                    categoryName:       "Category12",
                    eventId:            new(12, "Event12"),
                    exception:          null,
                    globalFields:       null,
                    logLevel:           LogLevel.Trace,
                    message:            "This test event contains a scope-state fieldset with a variety of fields.",
                    occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(13).UtcDateTime,
                    scopeStates:        new[]
                    {
                        new[]
                        {
                            new KeyValuePair<string, object?>("StateField1", "State Value 1"),
                            new KeyValuePair<string, object?>("StateField2", 14),
                            new KeyValuePair<string, object?>("StateField3", true)
                        }
                    },
                    state:              null),

                CreateAlwaysTestCaseData(
                    testName:           "ScopeStateIsNull",
                    categoryName:       "Category13",
                    eventId:            new(13, "Event13"),
                    exception:          null,
                    globalFields:       null,
                    logLevel:           LogLevel.Debug,
                    message:            "This test event contains a scope-state value that is null.",
                    occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(14).UtcDateTime,
                    scopeStates:        new object?[]
                    {
                        null
                    },
                    state:              null),

                CreateAlwaysTestCaseData(
                    testName:           "ScopeStateIsNumber",
                    categoryName:       "Category14",
                    eventId:            new(14, "Event14"),
                    exception:          null,
                    globalFields:       null,
                    logLevel:           LogLevel.Information,
                    message:            "This test event contains a scope-state value that is a number.",
                    occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(15).UtcDateTime,
                    scopeStates:        new object?[]
                    {
                        16
                    },
                    state:              null),

                CreateAlwaysTestCaseData(
                    testName:           "ScopeStateIsObject",
                    categoryName:       "Category15",
                    eventId:            new(15, "Event15"),
                    exception:          null,
                    globalFields:       null,
                    logLevel:           LogLevel.Warning,
                    message:            "This test event contains a scope-state object with properties.",
                    occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(16).UtcDateTime,
                    scopeStates:        new object?[]
                    {
                        new
                        {
                            ObjectProperty1 = "Object Value 1",
                            ObjectProperty2 = 17,
                            ObjectProperty3 = false,
                        }
                    },
                    state:              null),

                CreateAlwaysTestCaseData(
                    testName:           "ScopeStateIsString",
                    categoryName:       "Category16",
                    eventId:            new(16, "Event16"),
                    exception:          null,
                    globalFields:       null,
                    logLevel:           LogLevel.Error,
                    message:            "This test event contains a scope-state value that is a string.",
                    occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(17).UtcDateTime,
                    scopeStates:        new object?[]
                    {
                        "EventState"
                    },
                    state:              null),

                CreateAlwaysTestCaseData(
                    testName:           "ScopeStatesAreFieldsets",
                    categoryName:       "Category17",
                    eventId:            new(17, "Event17"),
                    exception:          null,
                    globalFields:       null,
                    logLevel:           LogLevel.Critical,
                    message:            "This test event contains scope state fieldset objects.",
                    occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(18).UtcDateTime,
                    scopeStates:        new object?[]
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
                    state:              null),

                CreateAlwaysTestCaseData(
                    testName:           "ScopeStatesAreNotFieldsets",
                    categoryName:       "Category18",
                    eventId:            new(18, "Event18"),
                    exception:          null,
                    globalFields:       null,
                    logLevel:           LogLevel.Trace,
                    message:            "This test event contains scope state objects that are not fieldsets.",
                    occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(19).UtcDateTime,
                    scopeStates:        new object?[]
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
                    state:              null),

                CreateAlwaysTestCaseData(
                    testName:           "StateIncludesMessageTemplate",
                    categoryName:       "Category19",
                    eventId:            new(19, "Event19"),
                    exception:          null,
                    globalFields:       null,
                    logLevel:           LogLevel.Debug,
                    message:            "This test event contains a state fieldset with a message template field.",
                    occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(20).UtcDateTime,
                    scopeStates:        Array.Empty<object?>(),
                    state:              new[]
                    {
                        new KeyValuePair<string, object?>("{OriginalFormat}", "This test event contains a state fieldset with a message template field")
                    }),

                CreateAlwaysTestCaseData(
                    testName:           "StateIsArray",
                    categoryName:       "Category20",
                    eventId:            new(20, "Event20"),
                    exception:          null,
                    globalFields:       null,
                    logLevel:           LogLevel.Information,
                    message:            "This test event contains a state object that is an array.",
                    occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(21).UtcDateTime,
                    scopeStates:        Array.Empty<object?>(),
                    state:              new[]
                    {
                        "Array Value 1",
                        "Array Value 2",
                        "Array Value 3"
                    }),

                CreateAlwaysTestCaseData(
                    testName:           "StateIsEmptyFieldset",
                    categoryName:       "Category21",
                    eventId:            new(21, "Event21"),
                    exception:          null,
                    globalFields:       null,
                    logLevel:           LogLevel.Warning,
                    message:            "This test event contains a state fieldset with no fields.",
                    occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(22).UtcDateTime,
                    scopeStates:        Array.Empty<object?>(),
                    state:              Array.Empty<KeyValuePair<string, object?>>()),

                CreateAlwaysTestCaseData(
                    testName:           "StateIsFieldset",
                    categoryName:       "Category22",
                    eventId:            new(22, "Event22"),
                    exception:          null,
                    globalFields:       null,
                    logLevel:           LogLevel.Error,
                    message:            "This test event contains a state fieldset with a variety of fields.",
                    occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(23).UtcDateTime,
                    scopeStates:        Array.Empty<object?>(),
                    state:              new[]
                    {
                        new KeyValuePair<string, object?>("StateField1", "State Value 1"),
                        new KeyValuePair<string, object?>("StateField2", 24),
                        new KeyValuePair<string, object?>("StateField3", true)
                    }),

                CreateAlwaysTestCaseData(
                    testName:           "StateIsNumber",
                    categoryName:       "Category23",
                    eventId:            new(23, "Event23"),
                    exception:          null,
                    globalFields:       null,
                    logLevel:           LogLevel.Critical,
                    message:            "This test event contains a state value that is a number.",
                    occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(24).UtcDateTime,
                    scopeStates:        Array.Empty<object?>(),
                    state:              25),

                CreateAlwaysTestCaseData(
                    testName:           "StateIsObject",
                    categoryName:       "Category24",
                    eventId:            new(24, "Event24"),
                    exception:          null,
                    globalFields:       null,
                    logLevel:           LogLevel.Trace,
                    message:            "This test event contains a state object with properties.",
                    occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(25).UtcDateTime,
                    scopeStates:        Array.Empty<object?>(),
                    state:              new
                    {
                        ObjectProperty1 = "Object Value 1",
                        ObjectProperty2 = 26,
                        ObjectProperty3 = false,
                    }),

                CreateAlwaysTestCaseData(
                    testName:           "StateIsString",
                    categoryName:       "Category25",
                    eventId:            new(25, "Event25"),
                    exception:          null,
                    globalFields:       null,
                    logLevel:           LogLevel.Debug,
                    message:            "This test event contains a state value that is a string.",
                    occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(26).UtcDateTime,
                    scopeStates:        Array.Empty<object?>(),
                    state:              "EventState")
            };

        [TestCaseSource(nameof(Always_TestCaseData))]
        public Task Always_ResultIsCorrect(
                string                                  testName,
                string                                  categoryName,
                EventId                                 eventId,
                Exception?                              exception,
                IReadOnlyDictionary<string, string>?    globalFields,
                LogLevel                                logLevel,
                string                                  message,
                DateTime                                occurredUtc,
                IReadOnlyList<object?>                  scopeStates,
                object?                                 state)
            => (Task)GetType()
                .GetMethod(nameof(Always_ResultIsCorrect), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(state?.GetType() ?? typeof(object))
                .Invoke(null, new[]
                {
                    testName,
                    categoryName,
                    eventId,
                    exception,
                    globalFields,
                    logLevel,
                    message,
                    occurredUtc,
                    scopeStates,
                    state
                })!;

        private static async Task Always_ResultIsCorrect<TState>(
            string                                  testName,
            string                                  categoryName,
            EventId                                 eventId,
            Exception?                              exception,
            IReadOnlyDictionary<string, string>?    globalFields,
            LogLevel                                logLevel,
            string                                  message,
            DateTime                                occurredUtc,
            IReadOnlyList<object?>                  scopeStates,
            TState                                  state)
        {
            using var uut = new Uut();

            var scopeProvider = new FakeExternalScopeProvider();
            foreach (var scopeState in scopeStates)
                scopeProvider.Push(scopeState);

            uut.Load(
                categoryName:   categoryName,
                eventId:        eventId,
                exception:      exception,
                formatter:      (_, _) => message,
                globalFields:   globalFields,
                logLevel:       logLevel,
                occurredUtc:    occurredUtc,
                scopeProvider:  scopeProvider,
                state:          state,
                options:        new JsonSerializerOptions()
                {
                    WriteIndented = true
                });

            uut.CategoryName    .ShouldBe(categoryName);
            uut.EventId.Id      .ShouldBe(eventId.Id);
            uut.EventId.Name    .ShouldBe(eventId.Name);
            uut.LogLevel        .ShouldBe(logLevel);
            uut.OccurredUtc     .ShouldBe(occurredUtc);

            using var buffer = new MemoryStream();
            uut.CopyBufferTo(buffer);

            uut.BufferLength.ShouldBe(buffer.Length);

            Console.WriteLine(Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length));

            buffer.Position = 0;
            var resultDocument = JsonDocument.Parse(buffer);

            var expectedDocument = await TestAssetLoader.LoadJsonAsync(Path.Combine("LoadAssets", $"{testName}.json"));
            
            resultDocument.RootElement.ShouldMatch(expectedDocument.RootElement, new[]
            {
                "@x"
            });
        }

        [Test]
        public async Task Always_RespectsOptions()
        {
            using var uut = new Uut();

            var scopeProvider = new FakeExternalScopeProvider();
            scopeProvider.Push(new CustomState() { Value = "This is a custom scope state object" });

            var options = new JsonSerializerOptions()
            {
                WriteIndented           = true,
                PropertyNamingPolicy    = JsonNamingPolicy.CamelCase
            };
            options.Converters.Add(new FakeWriteJsonConverter<CustomState>((writer, value, options) =>
                writer.WriteStringValue(value.Value)));

            uut.Load(
                categoryName:   "Category",
                eventId:        new(1, "Event"),
                exception:      null,
                formatter:      (_, _) => "This is a test message",
                globalFields:   null,
                logLevel:       LogLevel.Debug,
                occurredUtc:    DateTime.UnixEpoch,
                scopeProvider:  scopeProvider,
                state:          new CustomState() { Value = "This is a custom state object" },
                options:        options);

            using var buffer = new MemoryStream();
            uut.CopyBufferTo(buffer);

            uut.BufferLength.ShouldBe(buffer.Length);

            Console.WriteLine(Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length));

            buffer.Position = 0;
            var resultDocument = JsonDocument.Parse(buffer);

            var expectedDocument = await TestAssetLoader.LoadJsonAsync(Path.Combine("LoadAssets", "RespectsOptions.json"));
            
            resultDocument.RootElement.ShouldMatch(expectedDocument.RootElement);
        }

        private class CustomState
        {
            public string Value
                = string.Empty;
        }
    }
}
