using System;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

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
            Action<SeqLoggerConfiguration>? configure           = null,
            Action<IHttpClientBuilder>?     configureHttpClient = null)
        {
            builder.AddConfiguration();

            builder.Services.AddOptions<SeqLoggerConfiguration>();
            if (configure is not null)
                builder.Services.Configure(configure);
            
            builder.Services.AddOptions<JsonSerializerOptions>();
            builder.Services.ConfigureOptions<JsonSerializerOptionsConfigurator>();

            builder.Services.AddSingleton<ILoggerProvider>(serviceProvider =>
            {
                var internalServices = new ServiceCollection()
                    .AddSingleton<ISeqLoggerEventChannel, SeqLoggerEventChannel>()
                    .AddSingleton<ISeqLoggerEventDeliveryManager, SeqLoggerEventDeliveryManager>()
                    .AddSingleton<ISeqLoggerManager, SeqLoggerManager>()
                    .AddSingleton<ISeqLoggerPayloadBuilder, SeqLoggerPayloadBuilder>()
                    .AddSingleton<ISystemClock, DefaultSystemClock>()
                    .AddSingleton(serviceProvider.GetRequiredService<IOptions<SeqLoggerConfiguration>>())
                    .AddSingleton(serviceProvider.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>())
                    .AddSingleton(_ => serviceProvider.GetRequiredService<ILogger<SeqLoggerEventDeliveryManager>>())
                    .AddSingleton(_ => serviceProvider.GetRequiredService<ILogger<SeqLoggerManager>>())
                    .AddSingleton(_ => serviceProvider.GetRequiredService<ILogger<SeqLoggerPayloadBuilder>>());

                var httpClientBuilder = internalServices
                    .AddHttpClient(SeqLoggerConstants.HttpClientName);
                if (configureHttpClient is not null)
                    configureHttpClient.Invoke(httpClientBuilder);

                var internalServiceProvider = internalServices.BuildServiceProvider(new ServiceProviderOptions()
                {
                    ValidateOnBuild = true,
                    ValidateScopes  = true
                });

                return new SeqLoggerProvider.SeqLoggerProvider(
                    internalServiceProvider.DisposeAsync,
                    internalServiceProvider);
            });

            LoggerProviderOptions.RegisterProviderOptions<SeqLoggerConfiguration, SeqLoggerProvider.SeqLoggerProvider>(builder.Services);

            return builder;
        }
    }
}
