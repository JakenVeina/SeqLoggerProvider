using System.Text.Json;

namespace SeqLoggerProvider.Json
{
    /// <summary>
    /// A <see cref="JsonNamingPolicy"/> that simply leaves all names unchanged.
    /// </summary>
    public class PassthroughJsonNamingPolicy
        : JsonNamingPolicy
    {
        /// <summary>
        /// A default instance of the <see cref="PassthroughJsonNamingPolicy"/> class.
        /// </summary>
        public static readonly PassthroughJsonNamingPolicy Default
            = new();

        /// <inheritdoc/>
        public override string ConvertName(string name)
            => name;
    }
}
