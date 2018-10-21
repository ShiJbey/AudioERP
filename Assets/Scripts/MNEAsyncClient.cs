// Asynchronous Client Socket
// This code is borrowed from: 
// https://docs.microsoft.com/en-us/dotnet/framework/network-programming/asynchronous-client-socket-example

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

namespace MNEServer
{
    // State object for receiving data from the server
    public class StateObject {
        // Client socket
        public Socket workSocket = null;
        // Size of receive buffer
        public const int BUFFER_SIZE = 1024;
        // Receive buffer
        public byte[] buffer = new byte[BUFFER_SIZE];
        // Received data string
        public StringBuilder stringBuilder = new StringBuilder();
    }

    public class MNEAsyncClient {
        //port number for the server
        private const int port = 9999;

        // ManualResetEvent instances signal completion
        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private static ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        // Response from the server
        private static String response = String.Empty;

        private static void StartClient() {
            // Connect to serve
            try {
                // Establish the remote endpoint for the socket
                IPHostEntry ipHostInfo = Dns.GetHostEntry("localhost");
                //IPAddress ipAddress = ipHostInfo.AddressList[0].MapToIPv4();
                IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                // Create TCP/IP socket
                Socket client = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect to remote endpoint
                client.BeginConnect(remoteEP,
                    new AsyncCallback(ConnectCallback), client);
                connectDone.WaitOne();

                // Send test data to the server
                Send(client, "This is a test");
                sendDone.WaitOne();

                // Receive the response from the server
                Receive(client);
                receiveDone.WaitOne();

                // Write the response to the console
                Console.WriteLine("Received response: {0}", response);

                // Release the socket
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }
        }

        public static string StartClient(string msg) {
            // Connect to serve
            try {
                // Establish the remote endpoint for the socket
                IPHostEntry ipHostInfo = Dns.GetHostEntry("localhost");
                //IPAddress ipAddress = ipHostInfo.AddressList[0].MapToIPv4();
                IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                // Create TCP/IP socket
                Socket client = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect to remote endpoint
                client.BeginConnect(remoteEP,
                    new AsyncCallback(ConnectCallback), client);
                connectDone.WaitOne();

                // Send test data to the server
                Send(client, msg);
                sendDone.WaitOne();

                // Receive the response from the server
                Receive(client);
                receiveDone.WaitOne();

                // Write the response to the console
                Console.WriteLine("Received response: {0}", response);

                // Release the socket
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }

            return response;
        }

        private static void ConnectCallback(IAsyncResult ar) {
            try {
                // Receive the socket from the state object
                Socket client = (Socket) ar.AsyncState;

                // Complete the connection
                client.EndConnect(ar);

                Console.WriteLine("Socket connected to {0}",
                    client.RemoteEndPoint.ToString());
                
                // Signal that the connection has been made
                connectDone.Set();

            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void Receive(Socket client) {
            try {
                // Creste the state object
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remot device
                client.BeginReceive(state.buffer, 0, StateObject.BUFFER_SIZE, 0,
                    new AsyncCallback(ReceiveCallback), state);
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void ReceiveCallback(IAsyncResult ar) {
            try {
                // Retrieve the state object and the client socket
                // from the asynchronous state object
                StateObject state = (StateObject) ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the server
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0) {
                    // Store the data incase there is still more coming
                    state.stringBuilder.Append(Encoding.UTF8.GetString(state.buffer,0,bytesRead));

                    // Get the rest of the data
                    client.BeginReceive(state.buffer, 0, StateObject.BUFFER_SIZE, 0,
                        new AsyncCallback(ReceiveCallback), state);
                } else {
                    // all the data has arrived
                    if (state.stringBuilder.Length > 1) {
                        response = state.stringBuilder.ToString();
                    }
                    receiveDone.Set();
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void Send(Socket client, String data) {
            // Convert data to UTF-8 encoding
            byte[] byteData = Encoding.UTF8.GetBytes(data);

            // Begin sending the data to the remote device
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        private static void SendCallback(IAsyncResult ar) {
            try {
                // Rertieve the socket from the state object
                Socket client = (Socket) ar.AsyncState;

                // Completer the sening of the data to the server
                int bytesSent =  client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all the bytes have been sent
                sendDone.Set();

            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }
        }

        public static int Main(String[] args) {
            StartClient("Jimmy Johns!");
            return 0;
        }
    }
}
