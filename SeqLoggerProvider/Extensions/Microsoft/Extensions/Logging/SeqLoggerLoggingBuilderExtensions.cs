using System;
using System.Net.Http;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using SeqLoggerProvider;

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
            new SeqLoggerProviderBuilder(configureHttpClient, configureJsonSerializer)
                .AddTo(builder, configure);

            return builder;
        }
    }
}
