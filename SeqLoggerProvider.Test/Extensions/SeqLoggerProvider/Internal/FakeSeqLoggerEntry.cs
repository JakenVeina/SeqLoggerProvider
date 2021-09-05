using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using Microsoft.Extensions.Logging;

namespace SeqLoggerProvider.Internal
{
    internal class FakeSeqLoggerEntry
        : ISeqLoggerEntry
    {
        public static FakeSeqLoggerEntry Create(
                long?       bufferLength    = null,
                string?     categoryName    = null,
                EventId?    eventId         = null,
                LogLevel?   logLevel        = null,
                DateTime?   occurredUtc     = null)
            => new(
                bufferLength:   bufferLength    ?? 0L,
                categoryName:   categoryName    ?? "FakeCategory",
                eventId:        eventId         ?? new(1, "FakeEvent"),
                logLevel:       logLevel        ?? LogLevel.Debug,
                occurredUtc:    occurredUtc     ?? DateTime.UnixEpoch);

        public static IEnumerable<FakeSeqLoggerEntry> Generate(
                int         entryCount,
                long?       bufferLength    = null,
                LogLevel?   maxLogLevel     = null)
            => Enumerable.Range(1, entryCount)
                .Select(i => Create(
                    bufferLength:   bufferLength ?? 1,
                    categoryName:   $"Category #{i}",
                    eventId:        new(i, $"Event #{i}"),
                    logLevel:       (LogLevel)(i % ((int)(maxLogLevel ?? LogLevel.Critical) + 1)),
                    occurredUtc:    DateTimeOffset.FromUnixTimeSeconds(i).UtcDateTime));

        public FakeSeqLoggerEntry(
            long        bufferLength,
            string      categoryName,
            EventId     eventId,
            LogLevel    logLevel,
            DateTime    occurredUtc)
        {
            _bufferLength   = bufferLength;
            _categoryName   = categoryName;
            _eventId        = eventId;
            _logLevel       = logLevel;
            _occurredUtc    = occurredUtc;

            _loadInvocations = new();
        }

        public FakeSeqLoggerEntry()
        {
            _categoryName   = string.Empty;
            _eventId        = new(default, string.Empty);

            _loadInvocations = new();
        }

        public long BufferLength
            => _bufferLength;

        public string CategoryName
            => _categoryName;

        public EventId EventId
            => _eventId;

        public Exception? LoadException
        {
            get => _loadException;
            set => _loadException = value;
        }

        public IReadOnlyList<LoadInvocation> LoadInvocations
            => _loadInvocations;

        public LogLevel LogLevel
            => _logLevel;

        public DateTime OccurredUtc
            => _occurredUtc;

        void ISeqLoggerEntry.CopyBufferTo(Stream destination) { }

        void ISeqLoggerEntry.Load<TState>(
            string                                  categoryName,
            EventId                                 eventId,
            Exception?                              exception,
            Func<TState, Exception?, string>        formatter,
            IReadOnlyDictionary<string, string>?    globalFields,
            LogLevel                                logLevel,
            DateTime                                occurredUtc,
            IExternalScopeProvider                  scopeProvider,
            TState                                  state,
            JsonSerializerOptions                   options)
        {
            _loadInvocations.Add(new(
                categoryName,
                eventId,
                exception,
                formatter,
                globalFields,
                logLevel,
                occurredUtc,
                scopeProvider,
                state,
                options));

            if (_loadException is not null)
                throw _loadException;
        }

        void ISeqLoggerEntry.Reset() { }

        void IDisposable.Dispose() { }

        private readonly long                   _bufferLength;
        private readonly string                 _categoryName;
        private readonly EventId                _eventId;
        private readonly List<LoadInvocation>   _loadInvocations;
        private readonly LogLevel               _logLevel;
        private readonly DateTime               _occurredUtc;

        private Exception?  _loadException;

        public class LoadInvocation
        {
            public LoadInvocation(
                string                                  categoryName,
                EventId                                 eventId,
                Exception?                              exception,
                Delegate                                formatter,
                IReadOnlyDictionary<string, string>?    globalFields,
                LogLevel                                logLevel,
                DateTime                                occurredUtc,
                IExternalScopeProvider                  scopeProvider,
                object?                                 state,
                JsonSerializerOptions                   options)
            {
                CategoryName    = categoryName;
                EventId         = eventId;
                Exception       = exception;
                Formatter       = formatter;
                GlobalFields    = globalFields;
                LogLevel        = logLevel;
                OccurredUtc     = occurredUtc;
                ScopeProvider   = scopeProvider;
                State           = state;
                Options         = options;
            }

            public readonly string                                  CategoryName;
            public readonly EventId                                 EventId;
            public readonly Exception?                              Exception;
            public readonly Delegate                                Formatter;
            public readonly IReadOnlyDictionary<string, string>?    GlobalFields;
            public readonly LogLevel                                LogLevel;
            public readonly DateTime                                OccurredUtc;
            public readonly IExternalScopeProvider                  ScopeProvider;
            public readonly object?                                 State;
            public readonly JsonSerializerOptions                   Options;
        }
    }
}
