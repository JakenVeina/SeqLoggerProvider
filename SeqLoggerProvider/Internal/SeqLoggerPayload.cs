using System;
using System.IO;

namespace SeqLoggerProvider.Internal
{
    internal interface ISeqLoggerPayload
        : IDisposable
    {
        Stream Buffer { get; }

        int EntryCount { get; }

        void Append(ISeqLoggerEntry entry);

        void Reset();
    }

    internal sealed class SeqLoggerPayload
        : ISeqLoggerPayload
    {
        public SeqLoggerPayload()
            => _buffer = new();

        public Stream Buffer
            => _buffer;

        public int EntryCount
            => _entryCount;

        public void Append(ISeqLoggerEntry entry)
        {
            if (_entryCount is not 0)
                _buffer.WriteByte((byte)'\n');

            entry.CopyBufferTo(_buffer);

            ++_entryCount;
        }

        public void Dispose()
            => _buffer.Dispose();

        public void Reset()
        {
            _buffer.SetLength(0);
            _entryCount = 0;
        }

        private readonly MemoryStream _buffer;

        private int _entryCount;
    }
}
