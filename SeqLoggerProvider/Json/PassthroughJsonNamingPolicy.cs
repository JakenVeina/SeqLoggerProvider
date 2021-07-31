using System.Text.Json;

namespace SeqLoggerProvider.Json
{
    public class PassthroughJsonNamingPolicy
        : JsonNamingPolicy
    {
        public static readonly PassthroughJsonNamingPolicy Default
            = new();

        public override string ConvertName(string name)
            => name;
    }
}
