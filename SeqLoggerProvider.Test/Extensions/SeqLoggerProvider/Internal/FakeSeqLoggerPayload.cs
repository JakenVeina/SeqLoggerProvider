using System.Collections.Generic;
using System.IO;

namespace SeqLoggerProvider.Internal
{
    internal sealed class FakeSeqLoggerPayload
        : ISeqLoggerPayload
    {
        public FakeSeqLoggerPayload()
        {
            _buffer     = new();
            _entries    = new();
        }

        public MemoryStream Buffer
            => _buffer;

        public IReadOnlyList<ISeqLoggerEntry> Entries
            => _entries;

        public int EntryCount
        {
            get => _entryCount;
            set => _entryCount = value;
        }

        public void Dispose()
            => _buffer.Dispose();

        Stream ISeqLoggerPayload.Buffer
            => _buffer;

        void ISeqLoggerPayload.Append(ISeqLoggerEntry entry)
        {
            _entries.Add(entry);
            _buffer.SetLength(_buffer.Length + entry.BufferLength);
        }

        void ISeqLoggerPayload.Reset() { }

        private readonly MemoryStream           _buffer;
        private readonly List<ISeqLoggerEntry>  _entries;
        
        private int _entryCount;
    }
}
