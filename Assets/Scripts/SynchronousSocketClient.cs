using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace MNEServer
{
    class SynchronousSocketClient
    {
        public const int PORT = 9999;
        public static string StartClient(string msg)
        {
            string response = String.Empty;
            byte[] bytes = new byte[1024];
            try
            {
                //IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                //IPAddress iPAddress = ipHostInfo.AddressList[0];
                

                IPHostEntry ipHostInfo = Dns.GetHostEntry("localhost");
                IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, PORT);

                Socket sender = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    sender.Connect(remoteEP);

                    Debug.Log("Socket connected to: " + sender.RemoteEndPoint.ToString());

                    byte[] encodedMsg = Encoding.UTF8.GetBytes(msg);

                    int bytesSent = sender.Send(encodedMsg);

                    int bytesRec = sender.Receive(bytes);
                    response = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                    Debug.Log("Client Received: " + response);

                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                    

                }
                catch (ArgumentNullException ex)
                {
                    Debug.LogError(ex.ToString());
                }
                catch (SocketException ex)
                {
                    Debug.LogError(ex.ToString());
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex.ToString());
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());
            }

            return response;
        }
    }
}
