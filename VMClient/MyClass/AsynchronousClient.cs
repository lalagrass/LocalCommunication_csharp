using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace VMClient.MyClass
{
    public class StateObject
    {
        // Client socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 256;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }

    public class AsynchronousClient
    {
        // The response from the remote device.
        private String response = String.Empty;
        private Socket client;
        private bool isAlive;
        private DispatcherTimer mTimer;
        private DispatcherTimer mStartTimer;
        private int counter;
        private IPAddress ipAddress;

        public void StartClient()
        {
            // Connect to a remote device.
            try
            {
                mStartTimer = new DispatcherTimer();
                mStartTimer.Interval = new TimeSpan(0, 0, 10);
                mStartTimer.Tick += new EventHandler(mStartTimer_Tick);
                mTimer = new DispatcherTimer();
                mTimer.Interval = new TimeSpan(0, 0, 10);
                mTimer.Tick += new EventHandler(mTimer_Tick);
                // Establish the remote endpoint for the socket.
                // The name of the 
                // remote device is "host.contoso.com".
                //IPHostEntry ipHostInfo = Dns.Resolve("host.contoso.com");
                
                counter = 0;
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                ipAddress = GetDefaultGateway();
                setupClient();

            }
            catch (Exception e)
            {
                StaticUtils.WriteLine(e.ToString());
            }
        }

        private void setupClient()
        {
            if (ipAddress == null)
            {
                StaticUtils.WriteLine("No available gateway, stop");
                return;
            }
            StaticUtils.WriteLine("Start Client");
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, StaticUtils.DefaultPort);
            isAlive = true;
            // Create a TCP/IP socket.
            client = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
            StaticUtils.WriteLine(String.Format("begin connect to {0}...", remoteEP));
            // Connect to the remote endpoint.
            client.BeginConnect(remoteEP,
                new AsyncCallback(ConnectCallback), client);
        }

        private void mStartTimer_Tick(object sender, EventArgs e)
        {
            StaticUtils.WriteLine("start tick");
            mStartTimer.Stop();
            setupClient();
        }

        private void mTimer_Tick(object sender, EventArgs e)
        {
            if (isAlive)
            {
                counter++;
                StaticUtils.WriteLine("tick " + counter);
                Send(client, String.Format("client send {0}", counter));
            }
            else
            {
                StaticUtils.WriteLine("tick but not alive");
                mTimer.Stop();
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);

                StaticUtils.WriteLine(String.Format("Socket connected to {0}", client.RemoteEndPoint.ToString()));
                Receive(client);
                mTimer.Start();
            }
            catch (Exception e)
            {
                StaticUtils.WriteLine(e.ToString());
            }
        }

        private void Receive(Socket client)
        {
            try
            {
                // Create the state object.
                if (client.Connected)
                {
                    StaticUtils.WriteLine("start receve");
                    StateObject state = new StateObject();
                    state.workSocket = client;

                    // Begin receiving the data from the remote device.
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                    StaticUtils.WriteLine("start timer");
                }
                else
                {
                    StaticUtils.WriteLine("client not connect, retry...");
                    mStartTimer.Stop();
                    mStartTimer.Start();
                }
            }
            catch (Exception e)
            {
                StaticUtils.WriteLine(e.ToString());
                StaticUtils.WriteLine("client not connect, retry...");
                mStartTimer.Stop();
                mStartTimer.Start();
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.
                int bytesRead = client.EndReceive(ar);
                StaticUtils.WriteLine("[Recv byte] " + bytesRead);
                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                    response = state.sb.ToString();
                    state.sb.Clear();
                    StaticUtils.WriteLine(response);
                }
                else
                {
                    if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                        StaticUtils.WriteLine(response);
                        state.sb.Clear();
                    }
                    else
                    {
                        StaticUtils.WriteLine("receive nothing");
                    }
                }
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                         new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                StaticUtils.WriteLine(e.ToString());
            }
        }

        private void Send(Socket client, String data)
        {
            try
            {
                if (client != null && client.Connected)
                {
                    // Convert the string data to byte data using ASCII encoding.
                    byte[] byteData = Encoding.ASCII.GetBytes(data);

                    // Begin sending the data to the remote device.
                    client.BeginSend(byteData, 0, byteData.Length, 0,
                        new AsyncCallback(SendCallback), client);
                }
                else
                {
                    StaticUtils.WriteLine("client not Connected");
                }
            }
            catch (Exception ex)
            {
                StaticUtils.WriteLine(ex.ToString());
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
                StaticUtils.WriteLine(String.Format("Sent {0} bytes to server.", bytesSent));
            }
            catch (Exception e)
            {
                StaticUtils.WriteLine(e.ToString());
            }
        }

        public void StopClient()
        {
            isAlive = false;
            mTimer.Stop();
            if (client != null)
            {
                if (client.Connected)
                {
                    client.Shutdown(SocketShutdown.Both);
                    client.Disconnect(false);
                }
                client.Close();
            }
        }

        private IPAddress GetDefaultGateway()
        {
            var card = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault();
            if (card == null)
            {
                StaticUtils.WriteLine("FirstOrDefault NetworkInterface null");
                return null;
            }
            var address = card.GetIPProperties().GatewayAddresses.FirstOrDefault();
            if (address == null)
            {
                StaticUtils.WriteLine("FirstOrDefault GatewayAddress Null");
                return null;
            }
            StaticUtils.WriteLine("Get Gateway: " + address.ToString());
            return address.Address;
        }
    }
}
