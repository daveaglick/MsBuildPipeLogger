using System;
using System.Collections.Concurrent;
using System.IO;

namespace MsBuildPipeLogger
{
    internal class PipeBuffer : Stream
    {
        private const int BufferSize = 8192;

        private readonly ConcurrentBag<Buffer> _pool = new ConcurrentBag<Buffer>();

        private readonly BlockingCollection<Buffer> _queue =
            new BlockingCollection<Buffer>(new ConcurrentQueue<Buffer>());

        private Buffer _current;

        public void CompleteAdding() => _queue.CompleteAdding();
        
        public bool IsCompleted => _queue.IsCompleted;
        
        public bool Write(Stream stream)
        {
            if (!_pool.TryTake(out Buffer buffer))
            {
                buffer = new Buffer();
            }
            if(buffer.Write(stream) == 0)
            {
                // Didn't write anything, return it to the pool
                _pool.Add(buffer);
                return false;
            }
            _queue.Add(buffer);
            return true;
        }
        
        public override void Write(byte[] buffer, int offset, int count) =>
            _queue.Add(new Buffer(buffer, offset, count));

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = 0;
            while(read < count)
            {
                // Ensure a buffer is available
                if(TakeBuffer())
                {
                    // Get as much as we can from the current buffer
                    read += _current.Read(buffer, offset + read, count - read);
                    if (_current.Count == 0)
                    {
                        // Used up this buffer, return to the pool if it's a pool buffer
                        if (_current.FromPool)
                        {
                            _pool.Add(_current);
                        }
                        _current = null;
                    }
                }
                else
                {
                    break;
                }
            }
            return read;
        }

        private bool TakeBuffer()
        {
            if (_current == null)
            {
                if (_queue.IsCompleted)
                {
                    return false;
                }
                try
                {
                    _current = _queue.Take();
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
            return true;
        }

        private class Buffer
        {
            private readonly byte[] _buffer;

            private int _offset;

            public int Count { get; private set; }

            public bool FromPool { get; }

            public Buffer()
            {
                _buffer = new byte[BufferSize];
                FromPool = true;
            }

            public Buffer(byte[] buffer, int offset, int count)
            {
                _buffer = buffer;
                _offset = offset;
                Count = count;
            }

            public int Write(Stream stream)
            {
                _offset = 0;
                Count = stream.Read(_buffer, _offset, BufferSize);
                return Count;
            }

            public int Read(byte[] buffer, int offset, int count)
            {
                int available = count > Count ? Count : count;
                Array.Copy(_buffer, _offset, buffer, offset, available);
                _offset += available;
                Count -= available;
                return available;
            }
        }

        // Not implemented

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new System.NotImplementedException();

        public override long Position
        {
            get => throw new System.NotImplementedException();
            set => throw new System.NotImplementedException();
        }

        public override void Flush() => throw new System.NotImplementedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new System.NotImplementedException();

        public override void SetLength(long value) => throw new System.NotImplementedException();
    }
}
