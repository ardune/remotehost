using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteHost.IPC
{
    public abstract class DuplexPipeBase : IDisposable
    {
        public DuplexPipeServer(string name)
        {
            var asd = GetReadStreamName();
            try
            {
                writePipe = new NamedPipeServerStream(name + "_a", PipeDirection.Out);
                readPipe = new NamedPipeServerStream(name + "_b", PipeDirection.In);
            }
            catch
            {
                Dispose();
                throw;
            }

            writeStream = new BinaryWriter(writePipe);
            readStream = new BinaryReader(readPipe);
            Task.Factory.StartNew(ListenToEvents, tokenSource.Token);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        protected abstract PipeStream GetPipeStream(string name);
        protected abstract string GetReadStreamName();
        protected abstract string GetWriteStreamName();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
    public class DuplexPipeServer : IDisposable
    {
        private readonly CancellationTokenSource tokenSource = new CancellationTokenSource();

        public event EventHandler<PipeRecievedDataEvent> DataRecieved;
        public event EventHandler PipeDisconnected;

        private readonly NamedPipeServerStream writePipe;
        private readonly NamedPipeServerStream readPipe;
        private readonly BinaryWriter writeStream;
        private readonly BinaryReader readStream;


        public void WaitForConnection(CancellationToken token, TimeSpan timeout)
        {
            Task.WaitAll(new[]
            {
                writePipe.WaitForConnectionAsync(token),
                readPipe.WaitForConnectionAsync(token)
            }, timeout);
        }

        public void Send(byte[] bytes)
        {
            writeStream.Write(bytes.Length);
            writeStream.Write(bytes);
        }

        private void ListenToEvents()
        {
            try
            {
                while (!tokenSource.IsCancellationRequested)
                {
                    var readByte = readStream.ReadByte();
                    var bytes = readStream.ReadBytes(readByte);
                    OnDataRecieved(new PipeRecievedDataEvent(bytes));
                }
            }
            catch (ObjectDisposedException)
            {
                OnPipeDisconnected();
            }
            catch (IOException)
            {
                OnPipeDisconnected();
            }
            finally
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            tokenSource.Cancel();
            writePipe?.Dispose();
            readPipe?.Dispose();
            writeStream?.Dispose();
            readStream?.Dispose();
        }

        protected virtual void OnDataRecieved(PipeRecievedDataEvent e)
        {
            DataRecieved?.Invoke(this, e);
        }

        protected virtual void OnPipeDisconnected()
        {
            PipeDisconnected?.Invoke(this, EventArgs.Empty);
        }
    }
}
