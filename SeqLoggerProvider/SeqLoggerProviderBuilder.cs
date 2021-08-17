using System;
using System.Threading.Tasks;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using BatchingLoggerProvider;
using BatchingLoggerProvider.Internal;

using SeqLoggerProvider.Internal;
using SeqLoggerProvider.Internal.Json;

namespace SeqLoggerProvider
{
    internal class SeqLoggerProviderBuilder
        : BatchingLoggerProviderBuilderBase<
            SeqLoggerProvider,
            SeqLoggerOptions,
            SeqLogger,
            ISeqLoggerEvent>
    {
        public SeqLoggerProviderBuilder(
            Action<IHttpClientBuilder>?                     configureHttpClient,
            Action<OptionsBuilder<JsonSerializerOptions>>?  configureJsonSerializer)
        {
            _configureHttpClient        = configureHttpClient;
            _configureJsonSerializer    = configureJsonSerializer;
        }

        protected override OptionsBuilder<SeqLoggerOptions> AddOptions(IServiceCollection services)
            => services.AddOptions<SeqLoggerOptions>()
                .ValidateDataAnnotations();

        protected override void ConfigureInternalServices(
            IServiceProvider    serviceProvider,
            IServiceCollection  internalServices)
        {
            var jsonSerializerOptionsBuilder = internalServices
                .AddOptions<JsonSerializerOptions>()
                .Configure<IOptions<SeqLoggerOptions>>((options, seqLoggerOptions) => options.Converters.Add(new SeqLoggerEventJsonConverter(seqLoggerOptions)));
            if (_configureJsonSerializer is not null)
                _configureJsonSerializer.Invoke(jsonSerializerOptionsBuilder);

            internalServices
                .AddSingleton<IBatchingLoggerManager, SeqLoggerManager>()
                .AddSingleton<IBatchingLoggerPayloadManager, SeqLoggerPayloadManager>()
                .AddSingleton(_ => serviceProvider.GetRequiredService<ILogger<SeqLoggerManager>>())
                .AddSingleton(_ => serviceProvider.GetRequiredService<ILogger<SeqLoggerPayloadManager>>());

            var httpClientBuilder = internalServices
                .AddHttpClient(SeqLoggerConstants.HttpClientName);
            if (_configureHttpClient is not null)
                _configureHttpClient.Invoke(httpClientBuilder);
        }

        protected override SeqLoggerProvider CreateProvider(
                Func<ValueTask>     onDisposedAsync,
                IServiceProvider    serviceProvider)
            => new(
                onDisposedAsync,
                serviceProvider);

        private readonly Action<IHttpClientBuilder>?                    _configureHttpClient;
        private readonly Action<OptionsBuilder<JsonSerializerOptions>>? _configureJsonSerializer;
    }
}
