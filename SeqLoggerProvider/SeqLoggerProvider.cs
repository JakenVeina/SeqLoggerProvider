﻿using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using BatchingLoggerProvider;
using BatchingLoggerProvider.Internal;
using BatchingLoggerProvider.Utilities;

using SeqLoggerProvider.Internal;

namespace SeqLoggerProvider
{
    /// <summary>
    /// An implementation of <see cref="ILoggerProvider"/> for a logger that delivers log event data to a remote Seq server, in batches.
    /// </summary>
    [ProviderAlias(SeqLoggerConstants.ProviderName)]
    public sealed class SeqLoggerProvider
        : BatchingLoggerProviderBase<SeqLogger, ISeqLoggerEvent>
    {
        internal SeqLoggerProvider(
                Func<ValueTask>     onDisposedAsync,
                IServiceProvider    serviceProvider)
            : base(
                onDisposedAsync,
                serviceProvider)
        { }

        /// <inheritdoc/>
        protected override SeqLogger CreateLogger(
                string                                          categoryName,
                IBatchingLoggerEventChannel<ISeqLoggerEvent>    eventChannel,
                IExternalScopeProvider                          externalScopeProvider,
                Action                                          onLog,
                ISystemClock                                    systemClock)
            => new(
                categoryName,
                eventChannel,
                externalScopeProvider,
                onLog,
                systemClock);
    }
}