using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.core
{
    public class FileOuputStreamEx : Stream
    {
        private const int CHUNK_SIZE = 4096;
        private byte[] buffer = new byte[0];
        private long position;
        private readonly long length;
        private ITransferData transferData;

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => length;

        public override long Position
        {
            get => position;
            set
            {
                position = value;
            }
        }

        public FileOuputStreamEx(long position, long length, ITransferData transferData)
        {
            this.position = position;
            this.length = length;
            this.transferData = transferData;
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (Enumerable.Any(this.buffer) || offset != 0)
            {
                byte[] array = new byte[this.buffer.Length + count];
                Buffer.BlockCopy(this.buffer, 0, array, 0, this.buffer.Length);
                Buffer.BlockCopy(buffer, offset, array, this.buffer.Length, count);
                buffer = array;
                count = buffer.Length;
            }
            if (Position + count < Length)
            {
                int num = count % CHUNK_SIZE;
                int num2 = count - num;
                if (num2 > 0)
                {
                    transferData.Transfer(buffer, Position, num2);
                    Position += num2;
                }
                this.buffer = new byte[num];
                Buffer.BlockCopy(buffer, num2, this.buffer, 0, num);
            }
            else
            {
                transferData.Transfer(buffer, Position, count);
                Position += count;
            }
        }

    }
}
