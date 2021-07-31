using System.Text.Json;

using Microsoft.Extensions.Options;

using SeqLoggerProvider.Json;

namespace SeqLoggerProvider.Internal.Json
{
    public class JsonSerializerOptionsConfigurator
        : IConfigureNamedOptions<JsonSerializerOptions>
    {
        public JsonSerializerOptionsConfigurator(IOptions<SeqLoggerConfiguration> seqLoggerConfiguration)
            => _seqLoggerConfiguration = seqLoggerConfiguration;

        public void Configure(string name, JsonSerializerOptions options)
        {
            if (name is SeqLoggerConstants.JsonSerializerOptionsName)
            {
                options.Converters.Add(new SeqLoggerEventJsonConverter(_seqLoggerConfiguration));
                options.Converters.Add(EventIdJsonConverter.Default);
            }
        }

        void IConfigureOptions<JsonSerializerOptions>.Configure(JsonSerializerOptions options) { }

        private readonly IOptions<SeqLoggerConfiguration> _seqLoggerConfiguration;
    }
}
