using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using BatchingLoggerProvider.Internal;
using BatchingLoggerProvider.Utilities;

namespace SeqLoggerProvider.Internal
{
    internal class SeqLoggerManager
        : BatchingLoggerManager<SeqLoggerOptions>
    {
        public SeqLoggerManager(
                ILogger<SeqLoggerManager>           logger,
                IOptions<SeqLoggerOptions>          options,
                IBatchingLoggerPayloadManager       payloadManager,
                ISystemClock                        systemClock)
            : base(
                logger,
                options,
                payloadManager,
                SeqLoggerConstants.ProviderName,
                systemClock)
        { }
    }
}
