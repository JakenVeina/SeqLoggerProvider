using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

using Microsoft.Extensions.Logging;

namespace SeqLoggerProvider.Internal
{
    internal static class SeqLoggerLoggerMessages
    {
        public static void DeliveryFailed(
                ILogger         logger,
                string?         response,
                string          serverUrl,
                HttpStatusCode? statusCode,
                Exception?      exception,
                TimeSpan        deliveryDuration)
            => logger.Log(
                logLevel:   LogLevel.Error,
                eventId:    new(0x5C0B7E4D, nameof(DeliveryFailed)),
                state:      new DeliveryFailedLoggerMessageState(
                    deliveryDuration,
                    response,
                    serverUrl,
                    statusCode),
                exception:  null,
                formatter:  DeliveryFailedLoggerMessageState.Formatter);
        private struct DeliveryFailedLoggerMessageState
            : IReadOnlyList<KeyValuePair<string, object?>>
        {
            public static readonly Func<DeliveryFailedLoggerMessageState, Exception?, string> Formatter
                = (state, _) => state.ToString();

            public DeliveryFailedLoggerMessageState(
                TimeSpan        deliveryDuration,
                string?         response,
                string          serverUrl,
                HttpStatusCode? statusCode)
            {
                _deliveryDuration   = deliveryDuration;
                _response           = response;
                _serverUrl          = serverUrl;
                _statusCode         = statusCode;
            }

            public KeyValuePair<string, object?> this[int index]
                => index switch
                {
                    0   => new(nameof(DeliveryDuration),    DeliveryDuration),
                    1   => new(nameof(Response),            Response),
                    2   => new(nameof(ServerUrl),           ServerUrl),
                    3   => new(nameof(StatusCode),          StatusCode),
                    _   => throw new KeyNotFoundException()
                };

            public int Count
                => 4;

            public TimeSpan DeliveryDuration
                => _deliveryDuration;

            public string? Response
                => _response;

            public string ServerUrl
                => _serverUrl;

            public HttpStatusCode? StatusCode
                => _statusCode;

            public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
            {
                yield return this[0];
                yield return this[1];
                yield return this[2];
                yield return this[3];
            }

            public override string ToString()
                => $"An error occurred during delivery of log entries to the server ({_serverUrl}, Status {_statusCode}, {_deliveryDuration} elapsed).";

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            private readonly TimeSpan           _deliveryDuration;
            private readonly string?            _response;
            private readonly string             _serverUrl;
            private readonly HttpStatusCode?    _statusCode;
        }

        public static void DeliveryFinished(
                ILogger             logger,
                ISeqLoggerPayload   payload,
                TimeSpan            deliveryDuration)
            => _deliveryFinished.Invoke(
                logger,
                payload.EntryCount,
                payload.Buffer.Length,
                deliveryDuration,
                null);
        private static readonly Action<ILogger, int, long, TimeSpan, Exception?> _deliveryFinished
            = LoggerMessage.Define<int, long, TimeSpan>(
                logLevel:       LogLevel.Debug,
                eventId:        new(0x429D68DE, nameof(DeliveryFinished)),
                formatString:   "Finished delivery of {EntryCount} events ({PayloadLength} bytes, {DeliveryDuration} elapsed)");

        public static void DeliveryStarting(
                ILogger             logger,
                ISeqLoggerPayload   payload)
            => _deliveryStarting.Invoke(
                logger,
                payload.EntryCount,
                payload.Buffer.Length,
                null);
        private static readonly Action<ILogger, int, long, Exception?> _deliveryStarting
            = LoggerMessage.Define<int, long>(
                logLevel:       LogLevel.Debug,
                eventId:        new(0x7954EBB4, nameof(DeliveryStarting)),
                formatString:   "Starting delivery of {EntryCount} events ({PayloadLength} bytes)");

        public static void EntrySerializationFailed(
                ILogger     logger,
                string      failedEntryCategoryName,
                EventId     failedEntryEventId,
                LogLevel    failedEntryLogLevel,
                DateTime    failedEntryOccurredUtc,
                Exception   exception)
            => logger.Log(
                logLevel:   LogLevel.Warning,
                eventId:    new(0x1F48B658, nameof(EntrySerializationFailed)),
                state:      new EntrySerializationFailedLoggerMessageState(
                    failedEntryCategoryName,
                    failedEntryEventId,
                    failedEntryLogLevel,
                    failedEntryOccurredUtc),
                exception:  exception,
                formatter:  EntrySerializationFailedLoggerMessageState.Formatter);
        private struct EntrySerializationFailedLoggerMessageState
            : IReadOnlyList<KeyValuePair<string, object?>>
        {
            public static readonly Func<EntrySerializationFailedLoggerMessageState, Exception?, string> Formatter
                = (state, _) => state.ToString();

            public EntrySerializationFailedLoggerMessageState(
                string      failedEntryCategoryName,
                EventId     failedEntryId,
                LogLevel    failedEntryLogLevel,
                DateTime    failedEntryOccurredUtc)
            {
                _failedEntryCategoryName    = failedEntryCategoryName;
                _failedEntryId              = failedEntryId;
                _failedEntryLogLevel        = failedEntryLogLevel;
                _failedEntryOccurredUtc     = failedEntryOccurredUtc;
            }

            public KeyValuePair<string, object?> this[int index]
                => index switch
                {
                    0   => new(nameof(FailedEntryCategoryName), FailedEntryCategoryName),
                    1   => new(nameof(FailedEntryEventId),      FailedEntryEventId),
                    2   => new(nameof(FailedEntryEventName),    FailedEntryEventName),
                    3   => new(nameof(FailedEntryLogLevel),     FailedEntryLogLevel.ToString()),
                    4   => new(nameof(FailedEntryOccurredUtc),  FailedEntryOccurredUtc),
                    _   => throw new KeyNotFoundException()
                };

            public int Count
                => 5;

            public string FailedEntryCategoryName
                => _failedEntryCategoryName;

            public int FailedEntryEventId
                => _failedEntryId.Id;

            public string FailedEntryEventName
                => _failedEntryId.Name;

            public LogLevel FailedEntryLogLevel
                => _failedEntryLogLevel;

            public DateTimeOffset FailedEntryOccurredUtc
                => _failedEntryOccurredUtc;

            public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
            {
                yield return this[0];
                yield return this[1];
                yield return this[2];
                yield return this[3];
                yield return this[4];
            }

            public override string ToString()
            {
                var idIsSpecified = _failedEntryId.Id is not 0;
                var nameIsSpecified = !string.IsNullOrWhiteSpace(_failedEntryId.Name);

                return (idIsSpecified, nameIsSpecified) switch
                {
                    (false, false)  => $"An error occurred during serialization of a log entry ({_failedEntryCategoryName}).",
                    (false, true)   => $"An error occurred during serialization of a log entry ({_failedEntryCategoryName}:{_failedEntryId.Name}).",
                    (true, false)   => $"An error occurred during serialization of a log entry ({_failedEntryCategoryName}:{_failedEntryId.Id}).",
                    _               => $"An error occurred during serialization of a log entry ({_failedEntryCategoryName}:{_failedEntryId.Id}:{_failedEntryId.Name})."
                };
            }

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            private readonly string     _failedEntryCategoryName;
            private readonly EventId    _failedEntryId;
            private readonly LogLevel   _failedEntryLogLevel;
            private readonly DateTime   _failedEntryOccurredUtc;
        }

        public static void EntryTooLarge(
                ILogger         logger,
                ISeqLoggerEntry failedEntry,
                long            maxPayloadSize)
            => logger.Log(
                logLevel:   LogLevel.Warning,
                eventId:    new(0x2065AC0C, nameof(EntryTooLarge)),
                state:      new EntryTooLargeLoggerMessageState(failedEntry, maxPayloadSize),
                exception:  null,
                formatter:  EntryTooLargeLoggerMessageState.Formatter);
        private struct EntryTooLargeLoggerMessageState
            : IReadOnlyList<KeyValuePair<string, object?>>
        {
            public static readonly Func<EntryTooLargeLoggerMessageState, Exception?, string> Formatter
                = (state, _) => state.ToString();

            public EntryTooLargeLoggerMessageState(
                ISeqLoggerEntry failedEvent,
                long            maxPayloadSize)
            {
                _failedEventCategoryName    = failedEvent.CategoryName;
                _failedEventId              = failedEvent.EventId.Id;
                _failedEventLogLevel        = failedEvent.LogLevel;
                _failedEventName            = failedEvent.EventId.Name;
                _failedEventOccurredUtc     = failedEvent.OccurredUtc;
                _failedEventSize            = failedEvent.BufferLength;
                _maxPayloadSize             = maxPayloadSize;
            }

            public KeyValuePair<string, object?> this[int index]
                => index switch
                {
                    0 => new(nameof(FailedEventCategoryName),   FailedEventCategoryName),
                    1 => new(nameof(FailedEventId),             FailedEventId),
                    2 => new(nameof(FailedEventLogLevel),       FailedEventLogLevel.ToString()),
                    3 => new(nameof(EvaileEventName),           EvaileEventName),
                    4 => new(nameof(FailedEventOccurredUtc),    FailedEventOccurredUtc),
                    5 => new(nameof(FailedEventSize),           FailedEventSize),
                    6 => new(nameof(MaxPayloadSize),            MaxPayloadSize),
                    _ => throw new KeyNotFoundException()
                };

            public int Count
                => 7;

            public string FailedEventCategoryName
                => _failedEventCategoryName;

            public int FailedEventId
                => _failedEventId;

            public LogLevel FailedEventLogLevel
                => _failedEventLogLevel;

            public string EvaileEventName
                => _failedEventName;

            public DateTimeOffset FailedEventOccurredUtc
                => _failedEventOccurredUtc;

            public long FailedEventSize
                => _failedEventSize;

            public long MaxPayloadSize
                => _maxPayloadSize;

            public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
            {
                yield return this[0];
                yield return this[1];
                yield return this[2];
                yield return this[3];
                yield return this[4];
                yield return this[5];
                yield return this[6];
            }

            public override string ToString()
            {
                var idIsSpecified = _failedEventId is not 0;
                var nameIsSpecified = string.IsNullOrWhiteSpace(_failedEventName);

                return (idIsSpecified, nameIsSpecified) switch
                {
                    (false, false)  => $"A log event ({_failedEventCategoryName}) was too large to deliver ({_failedEventSize}, max {_maxPayloadSize}).",
                    (false, true)   => $"A log event ({_failedEventCategoryName}:{_failedEventName}) was too large to deliver ({_failedEventSize}, max {_maxPayloadSize}).",
                    (true, false)   => $"A log event ({_failedEventCategoryName}:{_failedEventId}) was too large to deliver ({_failedEventSize}, max {_maxPayloadSize}).",
                    _               => $"A log event ({_failedEventCategoryName}:{_failedEventId}:{_failedEventName}) was too large to deliver ({_failedEventSize}, max {_maxPayloadSize})."
                };
            }

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            private readonly string     _failedEventCategoryName;
            private readonly int        _failedEventId;
            private readonly LogLevel   _failedEventLogLevel;
            private readonly string     _failedEventName;
            private readonly long       _failedEventSize;
            private readonly DateTime   _failedEventOccurredUtc;
            private readonly long       _maxPayloadSize;
        }

        public static void ManagerCrashed(
                ILogger     logger,
                Exception   exception)
            => _managerCrashed.Invoke(
                logger,
                exception);
        private static readonly Action<ILogger, Exception?> _managerCrashed
            = LoggerMessage.Define(
                logLevel:       LogLevel.Critical,
                eventId:        new(0x1B322285, nameof(ManagerCrashed)),
                formatString:   "The Seq logger manager has crashed, due to an unrecoverable error, and can no longer deliver events to the server.");

        public static void ManagerStarted(ILogger logger)
            => _managerStarted.Invoke(logger, null);
        private static readonly Action<ILogger, Exception?> _managerStarted
            = LoggerMessage.Define(
                logLevel:       LogLevel.Information,
                eventId:        new(0x2E48908A, nameof(ManagerStarted)),
                formatString:   "The Seq logger manager has started delivering events to the server.");

        public static void ManagerStopped(ILogger logger)
            => _managerStopped.Invoke(logger, null);
        private static readonly Action<ILogger, Exception?> _managerStopped
            = LoggerMessage.Define(
                logLevel:       LogLevel.Information,
                eventId:        new(0x12EB92DA, nameof(ManagerStopped)),
                formatString:   "The Seq logger manager has stopped delivering events to the server.");
    }
}
