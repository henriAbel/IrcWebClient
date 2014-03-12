using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;

namespace IrcConnector.NetTools
{
    /// <summary>
    /// Simple class for handling asynchronous TCP connection
    /// </summary>
    public class AsyncConnector : IDisposable
    {
        #region variables
        private TcpClient _client;
        private Stream _networkStream;
        private byte[] _messageBuffer = new byte[4096];
        private MemoryStream _receiveBuffer;
        private MemoryStream _sendBuffer;
        private String _server;
        private int _port;
        private bool _close = false;
        public event onLineReceived lineReceive;
        public event onConnect onConnect;
        public event onError onError;
        #endregion

        public AsyncConnector(String server, int port)
        {
            _server = server;
            _port = port;
        }

        public void Connect()
        {
            IPAddress ip = null;

            var match = Regex.Match(_server, @"\b(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\b");
            if (match.Success) // Check if server is ipadress
                ip = IPAddress.Parse(_server);
            else if (_server.Equals("localhost")) // Or loopback
                ip = IPAddress.Loopback;
            else // Try to resolve aadress
            {
                try
                {
                    IPHostEntry entry = Dns.GetHostEntry(_server);
                    if (entry.AddressList.Length > 0)
                    {
                        ip = entry.AddressList[0];
                    }
                }
                catch (SocketException e)
                {       
                }
            }

            if (null != ip)
            {
                _client = new TcpClient();
                _client.BeginConnect(ip, _port, new AsyncCallback(connected), null);
            }
            else
            {
                connectionError(String.Format("Could not resolve server {0}", _server));
            }
        }

        private void connected(IAsyncResult iar)
        {
            try
            {
                _client.EndConnect(iar);
                // Get stream
                _networkStream = _client.GetStream();
                // Init buffers to recieive and send data
                _receiveBuffer = new MemoryStream();
                _sendBuffer = new MemoryStream();
                // Begin read from stream into _messageBuffer
                _networkStream.BeginRead(_messageBuffer, 0, _messageBuffer.Length, new AsyncCallback(receiveData), null);

                // Fire onConnect event
                if (null != onConnect)
                    onConnect();
            }
            catch (SocketException e)
            {
                connectionError(e.Message.ToString());
            }
            
        }

        /// <summary>
        /// Async callback for stream read
        /// </summary>
        /// <param name="iar"></param>
        private void receiveData(IAsyncResult iar)
        {
            int receivedDataLength;
            try
            {
                receivedDataLength = _networkStream.EndRead(iar);
            }
            catch (IOException e)
            {
                _close = true;
                onError(e.Message.ToString());
                return;
            }
            
            // Data length in buffer
            int bufferLength = (int)_receiveBuffer.Length;
            // Final data length
            int finalLength = bufferLength + receivedDataLength;
            // Init data[] array for holding received data
            byte[] data = new byte[finalLength];
            // Copy buffer to data beginning
            _receiveBuffer.ToArray().CopyTo(data, 0);
            // Copy recived data to end of data array
            Array.Copy(_messageBuffer, 0, data, bufferLength, receivedDataLength);
            // Empty received buffer
            _receiveBuffer.SetLength(0);
            
            int lineStartIndex = 0;
            for (int i = 0; i < data.Length - 1; i++)
            {
                // Find where line ends
                if (data[i] == 13 && data[i + 1] == 10) // 10 = LF, 13 = CR
                {
                    byte[] message = new byte[i - lineStartIndex];
                    Array.Copy(data, lineStartIndex, message, 0, i - lineStartIndex); 
                    // Fire onMessage event
                    onMessage(Encoding.UTF8.GetString(message));
                    lineStartIndex = i + 1;
                }
            }

            if (_close)
            {
                _client.Close();
                _networkStream.Close();
                GC.Collect();
                return;
            }

            // Lets write beginning of the half message to receiveBuffer.
            if (lineStartIndex < data.Length)
                _receiveBuffer.Write(data, lineStartIndex, data.Length - lineStartIndex);
           
            // And start receiving again
            _networkStream.BeginRead(_messageBuffer, 0, _messageBuffer.Length, new AsyncCallback(receiveData), null);
        }

        private void sendData(IAsyncResult iar)
        {
            if (!_close)
            {
                _networkStream.EndWrite(iar);    
            }
        }

        private void onMessage(String message)
        {
            if (null != lineReceive)
            {
                lineReceive(this, message);
            }
        }

        private void connectionError(String errorMessage)
        {
            if (null != onError)
            {
                onError(errorMessage);
            }
        }

        public void sendMessage(String message, params String[] values)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(String.Format(message, values));// Encoding.Convert(Encoding.UTF8, Encoding.ASCII, );
            var buffer = new byte[messageBytes.Length + 2];
            messageBytes.CopyTo(buffer, 0);
            // Enter line end bytes
            buffer[buffer.Length - 2] = 13;
            buffer[buffer.Length - 1] = 10;

            if (!_close && null != _networkStream)
            {
                // Maybe use temporary buffer?
                _networkStream.BeginWrite(buffer.ToArray(), 0, (int)buffer.Length, new AsyncCallback(sendData), null);
            }
        }

        public void closeConnection()
        {
            _close = true;
        }

        public void Dispose()
        {
        }
    }
}
