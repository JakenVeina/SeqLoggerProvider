using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

using Microsoft.Extensions.Logging;

namespace SeqLoggerProvider.Internal
{
    internal static partial class SeqLoggerLoggerMessages
    {
        public static void EventSerializationFailed(
                ILogger         logger,
                Exception       exception,
                SeqLoggerEvent  failedEvent)
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

            public EventSerializationFailedLoggerMessageState(SeqLoggerEvent failedEvent)
                => _failedEvent = failedEvent;

            public KeyValuePair<string, object?> this[int index]
                => index switch
                {
                    0   => new(nameof(CategoryName),    CategoryName),  
                    1   => new(nameof(EventId),         EventId),       
                    2   => new(nameof(EventName),       EventName),     
                    3   => new(nameof(LogLevel),        LogLevel),      
                    4   => new(nameof(Occurred),        Occurred),      
                    _   => throw new KeyNotFoundException()
                };

            public int Count
                => 5;

            public string CategoryName
                => _failedEvent.CategoryName;

            public int EventId
                => _failedEvent.EventId.Id;

            public string EventName
                => _failedEvent.EventId.Name;

            public LogLevel LogLevel
                => _failedEvent.LogLevel;

            public DateTimeOffset Occurred
                => _failedEvent.Occurred;

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
                var nameIsSpecified = string.IsNullOrWhiteSpace(_failedEvent.EventId.Name);

                return (idIsSpecified, nameIsSpecified) switch
                {
                    (false, false)  => $"An error occurred during serialization of data of a log event ({_failedEvent.CategoryName}).",
                    (false, true)   => $"An error occurred during serialization of data of a log event ({_failedEvent.CategoryName}:{_failedEvent.EventId.Name}).",
                    (true, false)   => $"An error occurred during serialization of data of a log event ({_failedEvent.CategoryName}:{_failedEvent.EventId.Id}).",
                    _               => $"An error occurred during serialization of data of a log event ({_failedEvent.CategoryName}:{_failedEvent.EventId.Id}:{_failedEvent.EventId.Name})."
                };
            }

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            private readonly SeqLoggerEvent _failedEvent;
        }


        public static void EventDeliveryFailed(
                ILogger         logger,
                string          response,
                string          serverUrl,
                HttpStatusCode  statusCode)
            => logger.Log(
                logLevel:   LogLevel.Error,
                eventId:    new(0x5C0B7E4D, nameof(EventDeliveryFailed)),
                state:      new EventDeliveryFailedLoggerMessageState(
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
                string          response,
                string          serverUrl,
                HttpStatusCode  statusCode)
            {
                _response   = response;
                _serverUrl  = serverUrl;
                _statusCode = statusCode;
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

            public string Response
                => _response;

            public string ServerUrl
                => _serverUrl;

            public HttpStatusCode StatusCode
                => _statusCode;

            public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
            {
                yield return new(nameof(Response),    Response);
                yield return new(nameof(ServerUrl),   ServerUrl);
                yield return new(nameof(StatusCode),  StatusCode);
            }

            public override string ToString()
                => $"An error occurred during delivery of events to the server ({_serverUrl}, Status {_statusCode}).";

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            private readonly string         _response;
            private readonly string         _serverUrl;
            private readonly HttpStatusCode _statusCode;
        }

        public static void EventDeliveryFinished(
                ILogger     logger,
                int         eventCount,
                long        payloadLength,
                TimeSpan    deliveryDuration)
            => _eventDeliveryFinished.Invoke(
                logger,
                eventCount,
                payloadLength,
                deliveryDuration,
                null);
        private static readonly Action<ILogger, int, long, TimeSpan, Exception?> _eventDeliveryFinished
            = LoggerMessage.Define<int, long, TimeSpan>(
                logLevel:       LogLevel.Debug,
                eventId:        new(0x429D68DE, nameof(EventDeliveryFinished)),
                formatString:   "Finished delivery of {EventCount} events ({PayloadLength} bytes, {DeliveryDuration} elapsed)");

        public static void EventDeliveryStarting(
                ILogger     logger,
                int         eventCount,
                long        payloadLength)
            => _eventDeliveryStarting.Invoke(
                logger,
                eventCount,
                payloadLength,
                null);
        private static readonly Action<ILogger, int, long, Exception?> _eventDeliveryStarting
            = LoggerMessage.Define<int, long>(
                logLevel:       LogLevel.Debug,
                eventId:        new(0x7954EBB4, nameof(EventDeliveryStarting)),
                formatString:   "Starting delivery of {EventCount} events ({PayloadLength} bytes)");

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
                formatString:   "The Seq logger manager has crashed, due to an unrecoverable error, and can no longer sedelivernd events to the server.");

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
