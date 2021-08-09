using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Configuration;

using SeqLoggerProvider;
using SeqLoggerProvider.Internal;
using SeqLoggerProvider.Internal.Json;
using SeqLoggerProvider.Utilities;

namespace Microsoft.Extensions.Logging
{
    public static class SeqLoggerLoggingBuilderExtensions
    {
        public static ILoggingBuilder AddSeq(
            this ILoggingBuilder            builder,
            Action<SeqLoggerConfiguration>? configure = null,
            Action<IHttpClientBuilder>?     configureHttpClient = null)
        {
            builder.AddConfiguration();

            builder.Services
                .AddSingleton<ILoggerProvider, SeqLoggerProvider.Internal.SeqLoggerProvider>()
                .AddSingleton<ISeqLoggerEventChannel, SeqLoggerEventChannel>()
                .AddSingleton<ISeqLoggerEventDeliveryManager, SeqLoggerEventDeliveryManager>()
                .AddSingleton<ISeqLoggerManager, SeqLoggerManager>()
                .AddSingleton<ISeqLoggerPayloadBuilder, SeqLoggerPayloadBuilder>()
                .AddSingleton<ISystemClock, DefaultSystemClock>()
                .ConfigureOptions<JsonSerializerOptionsConfigurator>();

            LoggerProviderOptions.RegisterProviderOptions<SeqLoggerConfiguration, SeqLoggerProvider.Internal.SeqLoggerProvider>(builder.Services);

            if (configure is not null)
                builder.Services.Configure(configure);

            var httpClientBuilder = builder.Services
                .AddHttpClient(SeqLoggerConstants.HttpClientName);

            if (configureHttpClient is not null)
                configureHttpClient.Invoke(httpClientBuilder);

            return builder;
        }
    }
}
