using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

using Microsoft.Extensions.Logging;

namespace SeqLoggerProvider.Internal
{
    internal static partial class SeqLoggerLoggerMessages
    {
        public static void EventSerializationFailed(
                ILogger         logger,
                Exception       exception,
                ISeqLoggerEvent failedEvent)
            => logger.Log(
                logLevel:   LogLevel.Warning,
                eventId:    new(0x1F48B658, nameof(EventSerializationFailed)),
                state:      new EventSerializationFailedLoggerMessageState(failedEvent),
                exception:  exception,
                formatter:  EventSerializationFailedLoggerMessageState.Formatter);
        private struct EventSerializationFailedLoggerMessageState
            : IReadOnlyList<KeyValuePair<string, object?>>
        {
            public static readonly Func<EventSerializationFailedLoggerMessageState, Exception?, string> Formatter
                = (state, _) => state.ToString();

            public EventSerializationFailedLoggerMessageState(ISeqLoggerEvent failedEvent)
                => _failedEvent = failedEvent;

            public KeyValuePair<string, object?> this[int index]
                => index switch
                {
                    0   => new(nameof(FailedEventCategoryName), FailedEventCategoryName),
                    1   => new(nameof(FailedEventId),           FailedEventId),
                    2   => new(nameof(FailedEventName),         FailedEventName),
                    3   => new(nameof(FailedEventLogLevel),     FailedEventLogLevel.ToString()),
                    4   => new(nameof(FailedEventOccurredUtc),  FailedEventOccurredUtc),
                    _   => throw new KeyNotFoundException()
                };

            public int Count
                => 5;

            public string FailedEventCategoryName
                => _failedEvent.CategoryName;

            public int FailedEventId
                => _failedEvent.EventId.Id;

            public string FailedEventName
                => _failedEvent.EventId.Name;

            public LogLevel FailedEventLogLevel
                => _failedEvent.LogLevel;

            public DateTimeOffset FailedEventOccurredUtc
                => _failedEvent.OccurredUtc;

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
                var idIsSpecified = _failedEvent.EventId.Id is not 0;
                var nameIsSpecified = !string.IsNullOrWhiteSpace(_failedEvent.EventId.Name);

                return (idIsSpecified, nameIsSpecified) switch
                {
                    (false, false)  => $"An error occurred during serialization of a log event ({_failedEvent.CategoryName}).",
                    (false, true)   => $"An error occurred during serialization of a log event ({_failedEvent.CategoryName}:{_failedEvent.EventId.Name}).",
                    (true, false)   => $"An error occurred during serialization of a log event ({_failedEvent.CategoryName}:{_failedEvent.EventId.Id}).",
                    _               => $"An error occurred during serialization of a log event ({_failedEvent.CategoryName}:{_failedEvent.EventId.Id}:{_failedEvent.EventId.Name})."
                };
            }

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            private readonly ISeqLoggerEvent _failedEvent;
        }

        public static void EventDeliveryFailed(
                ILogger         logger,
                string?         response,
                string          serverUrl,
                HttpStatusCode? statusCode,
                Exception?      exception,
                TimeSpan        deliveryDuration)
            => logger.Log(
                logLevel:   LogLevel.Error,
                eventId:    new(0x5C0B7E4D, nameof(EventDeliveryFailed)),
                state:      new EventDeliveryFailedLoggerMessageState(
                    deliveryDuration,
                    response,
                    serverUrl,
                    statusCode),
                exception:  null,
                formatter: EventDeliveryFailedLoggerMessageState.Formatter);
        private struct EventDeliveryFailedLoggerMessageState
            : IReadOnlyList<KeyValuePair<string, object?>>
        {
            public static readonly Func<EventDeliveryFailedLoggerMessageState, Exception?, string> Formatter
                = (state, _) => state.ToString();

            public EventDeliveryFailedLoggerMessageState(
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
                    0   => new(nameof(Response),    Response),
                    1   => new(nameof(ServerUrl),   ServerUrl),
                    2   => new(nameof(StatusCode),  StatusCode),
                    _   => throw new KeyNotFoundException()
                };

            public int Count
                => 3;

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
                yield return new(nameof(Response),    Response);
                yield return new(nameof(ServerUrl),   ServerUrl);
                yield return new(nameof(StatusCode),  StatusCode);
            }

            public override string ToString()
                => $"An error occurred during delivery of events to the server ({_serverUrl}, Status {_statusCode}, {_deliveryDuration} elapsed).";

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            private readonly TimeSpan           _deliveryDuration;
            private readonly string?            _response;
            private readonly string             _serverUrl;
            private readonly HttpStatusCode?    _statusCode;
        }

        public static void EventDeliveryFinished(
                ILogger     logger,
                uint        eventCount,
                long        payloadLength,
                TimeSpan    deliveryDuration)
            => _eventDeliveryFinished.Invoke(
                logger,
                eventCount,
                payloadLength,
                deliveryDuration,
                null);
        private static readonly Action<ILogger, uint, long, TimeSpan, Exception?> _eventDeliveryFinished
            = LoggerMessage.Define<uint, long, TimeSpan>(
                logLevel:       LogLevel.Debug,
                eventId:        new(0x429D68DE, nameof(EventDeliveryFinished)),
                formatString:   "Finished delivery of {EventCount} events ({PayloadLength} bytes, {DeliveryDuration} elapsed)");

        public static void EventDeliveryStarting(
                ILogger     logger,
                uint        eventCount,
                long        payloadLength)
            => _eventDeliveryStarting.Invoke(
                logger,
                eventCount,
                payloadLength,
                null);
        private static readonly Action<ILogger, uint, long, Exception?> _eventDeliveryStarting
            = LoggerMessage.Define<uint, long>(
                logLevel:       LogLevel.Debug,
                eventId:        new(0x7954EBB4, nameof(EventDeliveryStarting)),
                formatString:   "Starting delivery of {EventCount} events ({PayloadLength} bytes)");

        public static void EventTooLarge(
                ILogger         logger,
                ISeqLoggerEvent failedEvent,
                long            eventSize,
                long            maxPayloadSize)
            => logger.Log(
                logLevel:   LogLevel.Warning,
                eventId:    new(0x2065AC0C, nameof(EventTooLarge)),
                state:      new EventTooLargeLoggerMessageState(eventSize, failedEvent, maxPayloadSize),
                exception:  null,
                formatter:  EventTooLargeLoggerMessageState.Formatter);
        private struct EventTooLargeLoggerMessageState
            : IReadOnlyList<KeyValuePair<string, object?>>
        {
            public static readonly Func<EventTooLargeLoggerMessageState, Exception?, string> Formatter
                = (state, _) => state.ToString();

            public EventTooLargeLoggerMessageState(
                long            eventSize,
                ISeqLoggerEvent failedEvent,
                long            maxPayloadSize)
            {
                _eventSize      = eventSize;
                _failedEvent    = failedEvent;
                _maxPayloadSize = maxPayloadSize;
            }

            public KeyValuePair<string, object?> this[int index]
                => index switch
                {
                    0 => new(nameof(FailedEventCategoryName),   FailedEventCategoryName),
                    1 => new(nameof(FailedEventId),             FailedEventId),
                    2 => new(nameof(EvaileEventName),           EvaileEventName),
                    3 => new(nameof(FailedEventSize),           FailedEventSize),
                    4 => new(nameof(FailedEventLogLevel),       FailedEventLogLevel.ToString()),
                    5 => new(nameof(MaxPayloadSize),            MaxPayloadSize),
                    6 => new(nameof(FailedEventOccurredUtc),    FailedEventOccurredUtc),
                    _ => throw new KeyNotFoundException()
                };

            public int Count
                => 7;

            public string FailedEventCategoryName
                => _failedEvent.CategoryName;

            public int FailedEventId
                => _failedEvent.EventId.Id;

            public string EvaileEventName
                => _failedEvent.EventId.Name;

            public long FailedEventSize
                => _eventSize;

            public LogLevel FailedEventLogLevel
                => _failedEvent.LogLevel;

            public long MaxPayloadSize
                => _maxPayloadSize;

            public DateTimeOffset FailedEventOccurredUtc
                => _failedEvent.OccurredUtc;

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
                var idIsSpecified = _failedEvent.EventId.Id is not 0;
                var nameIsSpecified = string.IsNullOrWhiteSpace(_failedEvent.EventId.Name);

                return (idIsSpecified, nameIsSpecified) switch
                {
                    (false, false)  => $"A log event ({_failedEvent.CategoryName}) was too large to deliver ({_eventSize}, max {_maxPayloadSize}).",
                    (false, true)   => $"A log event ({_failedEvent.CategoryName}:{_failedEvent.EventId.Name}) was too large to deliver ({_eventSize}, max {_maxPayloadSize}).",
                    (true, false)   => $"A log event ({_failedEvent.CategoryName}:{_failedEvent.EventId.Id}) was too large to deliver ({_eventSize}, max {_maxPayloadSize}).",
                    _               => $"A log event ({_failedEvent.CategoryName}:{_failedEvent.EventId.Id}:{_failedEvent.EventId.Name}) was too large to deliver ({_eventSize}, max {_maxPayloadSize})."
                };
            }

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            private readonly long               _eventSize;
            private readonly ISeqLoggerEvent    _failedEvent;
            private readonly long               _maxPayloadSize;
        }
    }
}
