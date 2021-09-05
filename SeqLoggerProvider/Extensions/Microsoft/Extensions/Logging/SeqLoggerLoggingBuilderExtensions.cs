using System;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

using SeqLoggerProvider;
using SeqLoggerProvider.Internal;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Contains extension methods for setup of a Seq logger.
    /// </summary>
    public static class SeqLoggerLoggingBuilderExtensions
    {
        /// <summary>
        /// Regsiters and configures an instance of <see cref="SeqLoggerProvider"/> with a given <see cref="ILoggingBuilder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to which a <see cref="SeqLoggerProvider"/> instance is to be registered.</param>
        /// <param name="configure">A delegate to be used to configure the registration of <see cref="SeqLoggerOptions"/> with the options system, used by the provider.</param>
        /// <param name="configureJsonSerializer">A delegate to be used to configure the registration of <see cref="JsonSerializerOptions"/> with the options system, used by the provider.</param>
        /// <param name="configureHttpClient">A delegate to be used to configure the <see cref="HttpClient"/> instances, used by the provider.</param>
        /// <returns></returns>
        public static ILoggingBuilder AddSeq(
            this ILoggingBuilder                            builder,
            Action<OptionsBuilder<SeqLoggerOptions>>?       configure               = null,
            Action<OptionsBuilder<JsonSerializerOptions>>?  configureJsonSerializer = null,
            Action<IHttpClientBuilder>?                     configureHttpClient     = null)
        {
            builder.AddConfiguration();

            var optionsBuilder = builder.Services.AddOptions<SeqLoggerOptions>()
                .ValidateDataAnnotations();
            if (configure is not null)
                configure.Invoke(optionsBuilder);

            builder.Services.AddSingleton<ILoggerProvider>(serviceProvider =>
            {
                var internalServices = new ServiceCollection()
                    .AddSingleton(serviceProvider.GetRequiredService<IOptions<SeqLoggerOptions>>())
                    .AddSingleton<ISystemClock, DefaultSystemClock>()
                    .AddSingleton(_ => Channel.CreateUnbounded<ISeqLoggerEntry>(new()
                    {
                        AllowSynchronousContinuations   = false,
                        // We actually only need one reader, but MORE imporantly, we need the channel to be countable. https://github.com/dotnet/runtime/issues/53355
                        SingleReader                    = false,
                        SingleWriter                    = false
                    }))
                    .AddSingleton(serviceProvider => serviceProvider.GetRequiredService<Channel<ISeqLoggerEntry>>().Reader)
                    .AddSingleton(serviceProvider => serviceProvider.GetRequiredService<Channel<ISeqLoggerEntry>>().Writer)
                    .AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>()
                    .AddSingleton(serviceProvider => serviceProvider.GetRequiredService<ObjectPoolProvider>().Create(new SeqLoggerEntryPooledObjectPolicy()))
                    .AddSingleton(serviceProvider => serviceProvider.GetRequiredService<ObjectPoolProvider>().Create(new SeqLoggerPayloadPooledObjectPolicy()))
                    .AddSingleton<ISeqLoggerManager, SeqLoggerManager>()
                    .AddSingleton<ISeqLoggerDeliveryManager, SeqLoggerDeliveryManager>()
                    .AddSingleton<ISeqLoggerSelfLogger>(_ => new SeqLoggerSelfLogger(() => serviceProvider
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger<SeqLoggerProvider.SeqLoggerProvider>()))
                    .AddSingleton(serviceProvider => new SeqLoggerProvider.SeqLoggerProvider(
                        entryChannelWriter:     serviceProvider.GetRequiredService<ChannelWriter<ISeqLoggerEntry>>(),
                        entryPool:              serviceProvider.GetRequiredService<ObjectPool<ISeqLoggerEntry>>(),
                        jsonSerializerOptions:  serviceProvider.GetRequiredService<IOptions<JsonSerializerOptions>>(),
                        manager:                serviceProvider.GetRequiredService<ISeqLoggerManager>(),
                        options:                serviceProvider.GetRequiredService<IOptions<SeqLoggerOptions>>(),
                        selfLogger:             serviceProvider.GetRequiredService<ISeqLoggerSelfLogger>(),
                        systemClock:            serviceProvider.GetRequiredService<ISystemClock>()));

                var jsonSerializerOptionsBuilder = internalServices
                    .AddOptions<JsonSerializerOptions>()
                    .Configure(options =>
                    {
                        options.Converters.Add(new MemberInfoWriteOnlyJsonConverterFactory()); // System.Text.Json doesn't support serializing Type and other System.Reflection objects.
                        options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
                    });
                if (configureJsonSerializer is not null)
                    configureJsonSerializer.Invoke(jsonSerializerOptionsBuilder);

                var httpClientBuilder = internalServices
                    .AddHttpClient(SeqLoggerConstants.HttpClientName);
                if (configureHttpClient is not null)
                    configureHttpClient.Invoke(httpClientBuilder);

                var internalServiceProvider = internalServices.BuildServiceProvider(new ServiceProviderOptions()
                {
                    ValidateOnBuild = true,
                    ValidateScopes  = true
                });

                var provider = internalServiceProvider.GetRequiredService<SeqLoggerProvider.SeqLoggerProvider>();

                provider.Disposed += internalServiceProvider.Dispose;

                return provider;
            });

            LoggerProviderOptions.RegisterProviderOptions<SeqLoggerOptions, SeqLoggerProvider.SeqLoggerProvider>(builder.Services);

            return builder;
        }
    }
}
