namespace System.Text.Json
{
    internal sealed class PassthroughJsonNamingPolicy
        : JsonNamingPolicy
    {
        public static readonly PassthroughJsonNamingPolicy Default
            = new();

        public override string ConvertName(string name)
            => name;
    }
}
