using System.Runtime.Serialization;

namespace System
{
    public class TestException
        : Exception
    {
        public static TestException Create(string? message = default)
        {
            try
            {
                throw new TestException(message ?? "This is a test exception");
            }
            catch(TestException ex)
            {
                return ex;
            }
        }

        public TestException()
            : base() { }

        public TestException(string message)
            : base(message) { }

        public TestException(string message, Exception innerException)
            : base(message, innerException) { }

        protected TestException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
