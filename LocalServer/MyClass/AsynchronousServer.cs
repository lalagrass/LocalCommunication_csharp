using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LocalServer.MyClass
{
    // State object for reading client data asynchronously
    public class StateObject
    {
        // Client  socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 1024;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }

    public class AsynchronousServer
    {
        // Thread signal.
        private bool isListening = false;
        private Socket listener = null;
        private List<Socket> aliveSockets = new List<Socket>();

        public AsynchronousServer()
        {
        }

        public void StartListening()
        {
            // Data buffer for incoming data.
            byte[] bytes = new Byte[1024];

            // Establish the local endpoint for the socket.
            // The DNS name of the computer
            // running the listener is "host.contoso.com".
            //var entry = Dns.GetHostEntry();
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = null;
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    if (ni.Name.ToLowerInvariant().Contains("virtual"))
                    {
                        StaticUtils.WriteLine(ni.Name);
                        foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                StaticUtils.WriteLine(ip.Address.ToString());
                                ipAddress = ip.Address;
                            }

                        }
                    }
                }
            }
            if (ipAddress == null)
                return;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, StaticUtils.DefaultPort);
            StaticUtils.WriteLine(localEndPoint.ToString());
            // Create a TCP/IP socket.
            listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);
                isListening = true;
                // Start an asynchronous socket to listen for connections.
                ListenNew(listener);
            }
            catch (Exception e)
            {
                StaticUtils.WriteLine(e.ToString());
            }
        }

        public void StopListening()
        {
            try
            {
                isListening = false;
                foreach (var item in aliveSockets)
                {
                    if (item.Connected)
                    {
                        item.Shutdown(SocketShutdown.Both);
                        item.Disconnect(false);
                    }
                    item.Close();
                }
                if (listener != null)
                {
                    if (listener.Connected)
                    {
                        listener.Shutdown(SocketShutdown.Both);
                        listener.Disconnect(false);
                    }
                    listener.Close();
                }
            }
            catch (Exception ex)
            {
                StaticUtils.WriteLine(ex.ToString());
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.
            StateObject state = new StateObject();
            state.workSocket = handler;
            try
            {
                aliveSockets.Add(handler);
                StaticUtils.WriteLine("aliveSocket Count " + aliveSockets.Count);
            }
            catch (Exception ex)
            {
                StaticUtils.WriteLine(ex.ToString());
            }
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
            ListenNew(listener);
        }

        private void ListenNew(Socket listener)
        {
            StaticUtils.WriteLine("Waiting for a connection...");
            listener.BeginAccept(
                new AsyncCallback(AcceptCallback),
                listener);
        }

        public void ReadCallback(IAsyncResult ar)
        {
            try
            {
                String content = String.Empty;

                // Retrieve the state object and the handler socket
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.workSocket;

                // Read data from the client socket. 
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There  might be more data, so store the data received so far.
                    state.sb.Append(Encoding.ASCII.GetString(
                        state.buffer, 0, bytesRead));

                    // Check for end-of-file tag. If it is not there, read 
                    // more data.
                    content = state.sb.ToString();
                    StaticUtils.WriteLine(String.Format("Read {0} bytes from socket. \n Data : {1}",
                            content.Length, content));
                    state.sb.Clear();
                    Send(handler, "server got it, " + content);
                }
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
            }
            catch (Exception ex)
            {
                StaticUtils.WriteLine(ex.ToString());
            }
        }

        private void Send(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;
                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
                StaticUtils.WriteLine(String.Format("Sent {0} bytes to client.", bytesSent));
            }
            catch (Exception e)
            {
                StaticUtils.WriteLine(e.ToString());
            }
        }
    }
}