using System;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using SeqLoggerProvider;

namespace Microsoft.Extensions.Logging
{
    public static class SeqLoggerLoggingBuilderExtensions
    {
        public static ILoggingBuilder AddSeq(
            this ILoggingBuilder                            builder,
            Action<OptionsBuilder<SeqLoggerOptions>>?       configure               = null,
            Action<OptionsBuilder<JsonSerializerOptions>>?  configureJsonSerializer = null,
            Action<IHttpClientBuilder>?                     configureHttpClient     = null)
        {
            new SeqLoggerProviderBuilder(configureHttpClient, configureJsonSerializer)
                .AddTo(builder, configure);

            return builder;
        }
    }
}
