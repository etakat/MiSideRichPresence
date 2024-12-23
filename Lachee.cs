// Modified code from Lachee/discord-rpc-unity

// MIT License
// 
// Copyright (c) 2023 Lachee
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.IO;
using System.Runtime.InteropServices;
using DiscordRPC;
using DiscordRPC.IO;
using DiscordRPC.Message;

#region Exceptions
namespace Lachee.IO.Exceptions
{
    public class NamedPipeConnectionException : Exception
    {
        internal NamedPipeConnectionException(string message) : base(message) { }
    }
    
    public class NamedPipeOpenException : Exception
    {
        public int ErrorCode { get; private set; }
        internal NamedPipeOpenException(int err)
            : base("An exception has occured while trying to open the pipe. Error Code: " + err)
        {
            ErrorCode = err;
        }
    }
    
    public class NamedPipeReadException : Exception
    {
        public int ErrorCode { get; private set; }
        internal NamedPipeReadException(int err)
            : base("An exception occured while reading from the pipe. Error Code: " + err)
        {
            ErrorCode = err;
        }
    }
    
    public class NamedPipeWriteException : Exception
    {
        public int ErrorCode { get; private set; }
        internal NamedPipeWriteException(int err)
            : base("An exception occured while reading from the pipe. Error Code: " + err)
        {
            ErrorCode = err;
        }
    }
}
#endregion

#region NamedPipeClientStream
namespace Lachee.IO
{
    using Exceptions;

    public class NamedPipeClientStream : Stream
    {
        private IntPtr ptr;
        private bool _isDisposed;

        /// <summary> Always true. </summary>
        public override bool CanRead { get { return true; } }

        /// <summary> Always false. </summary>
        public override bool CanSeek { get { return false; } }

        /// <summary> Always true. </summary>
        public override bool CanWrite { get { return true; } }

        /// <summary> Always 0. </summary>
        public override long Length { get { return 0; } }

        /// <summary> Always 0. </summary>
        public override long Position { get { return 0; } set { } }

        /// <summary>
        /// Checks if the current pipe is connected and running.
        /// </summary>
        public bool IsConnected
        {
            get { return Native.IsConnected(ptr); }
        }

        /// <summary>
        /// The pipe name for this client.
        /// </summary>
        public string PipeName { get; private set; }

        #region Constructors
        /// <summary>
        /// Creates a new instance of a NamedPipeClientStream.
        /// </summary>
        /// <param name="server">The remote to connect to (normally ".").</param>
        /// <param name="pipeName">The name of the pipe to connect.</param>
        public NamedPipeClientStream(string server, string pipeName)
        {
            ptr = Native.CreateClient();
            PipeName = FormatPipe(server, pipeName);
        }

        ~NamedPipeClientStream()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!_isDisposed)
            {
                Disconnect();
                Native.DestroyClient(ptr);
                _isDisposed = true;
            }
        }

        private static string FormatPipe(string server, string pipeName)
        {
            return string.Format(@"\\{0}\pipe\{1}", server, pipeName);
        }
        #endregion

        #region Open / Close
        /// <summary>
        /// Attempts to open a named pipe.
        /// </summary>
        public void Connect()
        {
            int code = Native.Open(ptr, PipeName);
            if (!IsConnected)
                throw new NamedPipeOpenException(code);
        }

        /// <summary>
        /// Closes the named pipe already opened.
        /// </summary>
        public void Disconnect()
        {
            Native.Close(ptr);
        }
        #endregion

        #region Reading / Writing
        /// <summary>
        /// Reads a block of bytes from the stream (non-blocking).
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!IsConnected)
                throw new NamedPipeConnectionException("Cannot read stream as pipe is not connected");

            if (offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException("count", "Cannot read as the count exceeds the buffer size");

            int bytesRead = 0;
            int size = Marshal.SizeOf(buffer[0]) * count;
            IntPtr buffptr = Marshal.AllocHGlobal(size);

            try
            {
                bytesRead = Native.ReadFrame(ptr, buffptr, count);
                if (bytesRead <= 0)
                {
                    if (bytesRead < 0)
                        throw new NamedPipeReadException(bytesRead);

                    return 0;
                }
                else
                {
                    Marshal.Copy(buffptr, buffer, offset, bytesRead);
                    return bytesRead;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffptr);
            }
        }

        /// <summary>
        /// Writes a block of bytes to the current stream using data from a buffer.
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!IsConnected)
                throw new NamedPipeConnectionException("Cannot write stream as pipe is not connected");

            int size = Marshal.SizeOf(buffer[0]) * count;
            IntPtr buffptr = Marshal.AllocHGlobal(size);

            try
            {
                Marshal.Copy(buffer, offset, buffptr, count);
                int result = Native.WriteFrame(ptr, buffptr, count);
                if (result < 0)
                    throw new NamedPipeWriteException(result);
            }
            finally
            {
                Marshal.FreeHGlobal(buffptr);
            }
        }
        #endregion

        #region Unsupported
        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
        #endregion

        private static class Native
        {
            const string LIBRARY_NAME = "NativeNamedPipe";

            #region Creation and Destruction
            [DllImport(LIBRARY_NAME, EntryPoint = "createClient", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr CreateClient();

            [DllImport(LIBRARY_NAME, EntryPoint = "destroyClient", CallingConvention = CallingConvention.Cdecl)]
            public static extern void DestroyClient(IntPtr client);
            #endregion

            #region State Control
            [DllImport(LIBRARY_NAME, EntryPoint = "isConnected", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            public static extern bool IsConnected(IntPtr client);

            [DllImport(LIBRARY_NAME, EntryPoint = "open", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            public static extern int Open(IntPtr client, string pipename);

            [DllImport(LIBRARY_NAME, EntryPoint = "close", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            public static extern void Close(IntPtr client);
            #endregion

            #region IO
            [DllImport(LIBRARY_NAME, EntryPoint = "readFrame", CallingConvention = CallingConvention.Cdecl)]
            public static extern int ReadFrame(IntPtr client, IntPtr buffer, int length);

            [DllImport(LIBRARY_NAME, EntryPoint = "writeFrame", CallingConvention = CallingConvention.Cdecl)]
            public static extern int WriteFrame(IntPtr client, IntPtr buffer, int length);
            #endregion
        }
    }
}
#endregion

#region UnityNamedPipe
namespace Lachee.Discord.Control
{
    using IO;
    using DiscordRPC.Logging;

    /// <summary>
    /// Pipe Client used to communicate with Discord.
    /// </summary>
    public class UnityNamedPipe : INamedPipeClient
    {
        private const string PIPE_NAME = @"discord-ipc-{0}";

        private NamedPipeClientStream _stream;
        private byte[] _buffer = new byte[PipeFrame.MAX_SIZE];

        public ILogger Logger { get; set; }

        public bool IsConnected
        {
            get { return _stream != null && _stream.IsConnected; }
        }

        public int ConnectedPipe { get; private set; }

        private volatile bool _isDisposed = false;

        public bool Connect(int pipe)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("NamedPipe");

            if (pipe > 9)
                throw new ArgumentOutOfRangeException("pipe", "Argument cannot be greater than 9");

            if (pipe < 0)
            {
                // If -1, iterate over every pipe from 0..9
                for (int i = 0; i < 10; i++)
                {
                    if (AttemptConnection(i))
                        return true;
                }
                return false;
            }
            else
            {
                // Connect to a specific pipe
                return AttemptConnection(pipe);
            }
        }

        private bool AttemptConnection(int pipe)
        {
            if (_stream != null)
            {
                Logger?.Error("Attempted to create a new stream while one already exists!");
                return false;
            }

            if (IsConnected)
            {
                Logger?.Error("Attempted to create a new connection while one already exists!");
                return false;
            }

            try
            {
                string pipename = string.Format(PIPE_NAME, pipe);

                Logger?.Info("Connecting to " + pipename);
                ConnectedPipe = pipe;

                _stream = new NamedPipeClientStream(".", pipename);
                _stream.Connect();

                Logger?.Info("Connected");
                return true;
            }
            catch (Exception e)
            {
                Logger?.Error("Failed: " + e.GetType().FullName + ", " + e.Message);
                ConnectedPipe = -1;
                Close();
                return false;
            }
        }

        public void Close()
        {
            if (_stream != null)
            {
                Logger?.Trace("Closing stream");
                _stream.Dispose();
                _stream = null;
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            Logger?.Trace("Disposing Stream");
            _isDisposed = true;
            Close();
        }

        public bool ReadFrame(out PipeFrame frame)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("_stream");

            if (!IsConnected)
            {
                frame = default(PipeFrame);
                return false;
            }

            int length = _stream.Read(_buffer, 0, _buffer.Length);
            Logger?.Trace("Read {0} bytes", length);

            if (length == 0)
            {
                frame = default(PipeFrame);
                return false;
            }

            using (MemoryStream memory = new MemoryStream(_buffer, 0, length))
            {
                frame = new PipeFrame();
                if (!frame.ReadStream(memory))
                {
                    Logger?.Error("Failed to read a frame! {0}", frame.Opcode);
                    return false;
                }
                else
                {
                    Logger?.Trace("Read pipe frame!");
                    return true;
                }
            }
        }

        public bool WriteFrame(PipeFrame frame)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("_stream");

            if (!IsConnected)
            {
                Logger?.Error("Failed to write frame because the stream is closed");
                return false;
            }

            try
            {
                Logger?.Trace("Writing frame");
                frame.WriteStream(_stream);
                return true;
            }
            catch (IOException io)
            {
                Logger?.Error("Failed to write frame because of a IO Exception: {0}", io.Message);
            }
            catch (ObjectDisposedException)
            {
                Logger?.Warning("Failed to write frame as the stream was already disposed");
            }
            catch (InvalidOperationException)
            {
                Logger?.Warning("Failed to write frame because of an invalid operation");
            }

            return false;
        }
    }
}
#endregion

#region MessageEvents
namespace Lachee.Discord.Control
{
    // If you actually use Unity events, uncomment the below. Otherwise you can remove or replace them.
    // using UnityEngine.Events;

    [Serializable]
    public sealed class MessageEvents
    {
        // Typically you'd define:
        // [Serializable]
        // public sealed class ReadyMessageEvent : UnityEvent<ReadyMessage> { }
        // ... etc ...
        // But if you're not using Unity, replace these with your own delegate/event system.

        [Serializable]
        public sealed class ReadyMessageEvent : UnityEngine.Events.UnityEvent<ReadyMessage> { }
        [Serializable]
        public sealed class CloseMessageEvent : UnityEngine.Events.UnityEvent<CloseMessage> { }
        [Serializable]
        public sealed class ErrorMessageEvent : UnityEngine.Events.UnityEvent<ErrorMessage> { }
        [Serializable]
        public sealed class PresenceMessageEvent : UnityEngine.Events.UnityEvent<PresenceMessage> { }
        [Serializable]
        public sealed class SubscribeMessageEvent : UnityEngine.Events.UnityEvent<SubscribeMessage> { }
        [Serializable]
        public sealed class UnsubscribeMessageEvent : UnityEngine.Events.UnityEvent<UnsubscribeMessage> { }
        [Serializable]
        public sealed class JoinMessageEvent : UnityEngine.Events.UnityEvent<JoinMessage> { }
        [Serializable]
        public sealed class SpectateMessageEvent : UnityEngine.Events.UnityEvent<SpectateMessage> { }
        [Serializable]
        public sealed class JoinRequestMessageEvent : UnityEngine.Events.UnityEvent<JoinRequestMessage> { }
        [Serializable]
        public sealed class ConnectionEstablishedMessageEvent : UnityEngine.Events.UnityEvent<ConnectionEstablishedMessage> { }
        [Serializable]
        public sealed class ConnectionFailedMessageEvent : UnityEngine.Events.UnityEvent<ConnectionFailedMessage> { }

        public ReadyMessageEvent OnReady = new ReadyMessageEvent();
        public CloseMessageEvent OnClose = new CloseMessageEvent();
        public ErrorMessageEvent OnError = new ErrorMessageEvent();
        public PresenceMessageEvent OnPresenceUpdate = new PresenceMessageEvent();
        public SubscribeMessageEvent OnSubscribe = new SubscribeMessageEvent();
        public UnsubscribeMessageEvent OnUnsubscribe = new UnsubscribeMessageEvent();
        public JoinMessageEvent OnJoin = new JoinMessageEvent();
        public SpectateMessageEvent OnSpectate = new SpectateMessageEvent();
        public JoinRequestMessageEvent OnJoinRequest = new JoinRequestMessageEvent();
        public ConnectionEstablishedMessageEvent OnConnectionEstablished = new ConnectionEstablishedMessageEvent();
        public ConnectionFailedMessageEvent OnConnectionFailed = new ConnectionFailedMessageEvent();

        public void RegisterEvents(DiscordRpcClient client)
        {
            client.OnReady += (s, args) => OnReady?.Invoke(args);
            client.OnClose += (s, args) => OnClose?.Invoke(args);
            client.OnError += (s, args) => OnError?.Invoke(args);

            client.OnPresenceUpdate += (s, args) => OnPresenceUpdate?.Invoke(args);
            client.OnSubscribe += (s, args) => OnSubscribe?.Invoke(args);
            client.OnUnsubscribe += (s, args) => OnUnsubscribe?.Invoke(args);

            client.OnJoin += (s, args) => OnJoin?.Invoke(args);
            client.OnSpectate += (s, args) => OnSpectate?.Invoke(args);
            client.OnJoinRequested += (s, args) => OnJoinRequest?.Invoke(args);

            client.OnConnectionEstablished += (s, args) => OnConnectionEstablished?.Invoke(args);
            client.OnConnectionFailed += (s, args) => OnConnectionFailed?.Invoke(args);
        }
    }
}
#endregion
