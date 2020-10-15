using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebServer
{
    public class Server
    {
        private static HttpListener _httpListener;
        public static int maxSimultaneousConnections = 20;
        private static Semaphore sem = new Semaphore(maxSimultaneousConnections, maxSimultaneousConnections);

        /// <summary>
        /// Return list of IP adresses assigned to localhost network devices, 
        /// such as hardwired ethernet, wireless, etc 
        /// </summary>
        private static List<IPAddress> GetLocalHostIPs()
        {
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());
            List<IPAddress> ret = host.AddressList.Where(ip => ip.AddressFamily
                == AddressFamily.InterNetwork).ToList();

            return ret;
        }
        private static HttpListener InitializeListener(List<IPAddress> localhostIPs)
        {
            HttpListener httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://localhost/");

            //Listen to IP adress as well
            localhostIPs.ForEach(ip =>
            {
                Console.WriteLine($"Listening on http://{ip.ToString()}/");
                httpListener.Prefixes.Add($"http://{ip.ToString()}/");
            });

            return httpListener;
        }

        private static void Start(HttpListener listener)
        {
            listener.Start();
            Task.Run(() => RunServer(listener));
        }

        private static void RunServer(HttpListener listener)
        {
            while (true)
            {
                sem.WaitOne();
                StartConnectionListener(listener);
            }
        }

        /// <summary>
        /// Await connections
        /// </summary>
        private static async void StartConnectionListener(HttpListener listener)
        {
            //Wait for a connection. Return to a caller while we wait.
            HttpListenerContext context = await listener.GetContextAsync();

            // Release the semaphore so that another listener can be immediately started up.
            sem.Release();

            // We have a connection, do something...
            string responce = "hello, server!";
            byte[] encoded = Encoding.UTF8.GetBytes(responce);
            context.Response.ContentLength64 = encoded.Length;
            context.Response.OutputStream.Write(encoded, 0, encoded.Length);
            context.Response.OutputStream.Close();
        }

        /// <summary>
        /// Starts the web server
        /// </summary>
        public static void Start()
        {
            List<IPAddress> localhostIPs = GetLocalHostIPs();
            HttpListener listener = InitializeListener(localhostIPs);
            Start(listener);
        }
    }
}
