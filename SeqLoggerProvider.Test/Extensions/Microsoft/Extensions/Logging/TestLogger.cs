namespace Microsoft.Extensions.Logging
{
    public static class TestLogger
    {
        public static ILoggerFactory CreateFactory()
            => LoggerFactory.Create(builder => builder
                .SetMinimumLevel(LogLevel.Trace)
                .AddConsole());
    }
}
