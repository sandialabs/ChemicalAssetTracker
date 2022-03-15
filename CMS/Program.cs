using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace CMS
{
    public class Program
    {
        //---------------------------------------------------------------------
        //
        // HTTPS Control
        //
        //---------------------------------------------------------------------
        
        public const bool USE_HTTPS = false;
        public const int LISTEN_PORT = 5353;
        
        static bool UsingProxy = false;
        private static int _listenPort;
        private static X509Certificate2 _certificate;

        public static int ListenPort { get => _listenPort; private set => _listenPort = value; }

        public static void Main(string[] args)
        {
            List<string> webbuilder_args = new List<string>();
            _listenPort = LISTEN_PORT;

            int i = 0;
            while (i < args.Length)
            {
                string arg = args[i++];
                if (arg == "-proxy")
                {
                    UsingProxy = true;
                    continue;
                }
                if (arg == "-help")
                {
                    Banner(
                        "Use: dotnet run [-port <listen_port>] [-proxy]",
                        "     -port specifies the port to listen on - default is 5000",
                        "     -proxy will add headers to support running as reverse proxy"
                    );
                    return;
                }
                if (arg == "-port")
                {
                    if (i < args.Length)
                    {
                        string portstr = args[i++];
                        ListenPort = Int32.Parse(portstr);
                    }
                    continue;
                }
                webbuilder_args.Add(arg);
            }

            try
            {
                if (USE_HTTPS)
                {
                    // https://stackoverflow.com/a/46336873/706747
                    using (X509Store store = new X509Store(StoreName.My))
                    {
                        store.Open(OpenFlags.ReadOnly);
                        X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindBySubjectName, "localhost", false);
                        if (certs.Count > 0)
                            _certificate = certs[0];
                    }
                }
            }
            catch (Exception)
            {
            }

            BuildWebHost(webbuilder_args.ToArray()).Run();
        }

        public static void Banner(List<string> lines)
        {
            Console.WriteLine(" ");
            Console.WriteLine("########################################################################");
            Console.WriteLine("#");
            foreach (string line in lines)
            {
                Console.WriteLine("# " + line);
            }
            Console.WriteLine("#");
            Console.WriteLine("########################################################################");
            Console.WriteLine(" ");
        }

        public static void Banner(params string[] lines)
        {
            Console.WriteLine(" ");
            Console.WriteLine("########################################################################");
            Console.WriteLine("#");
            foreach (string line in lines)
            {
                Console.WriteLine("# " + line);
            }
            Console.WriteLine("#");
            Console.WriteLine("########################################################################");
            Console.WriteLine(" ");
        }


        public static IWebHost BuildWebHost(string[] args)
        {
            if (UsingProxy)
            {
                Console.WriteLine("BuildWebHost is using reverse proxy mode");
            }
            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                // .UseUrls($"http://*:{ListenPort}/")
                .UseKestrel(options =>
                {
                    options.Listen(IPAddress.Any, ListenPort, listenOptions =>
                    {
                        if (_certificate != null)
                            listenOptions.UseHttps(_certificate);
                    });
                })
                .Build();
        }
    }
}
