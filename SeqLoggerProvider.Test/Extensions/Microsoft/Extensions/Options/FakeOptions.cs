using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Options
{
    public static class FakeOptions
    {
        public static FakeOptions<T> Create<T>(T value)
                where T : class
            => new(value);
    }

    public class FakeOptions<T>
            : IOptions<T>
        where T : class
    {
        public FakeOptions(T value)
            => Value = value;

        public T Value { get; set; }
    }
}
