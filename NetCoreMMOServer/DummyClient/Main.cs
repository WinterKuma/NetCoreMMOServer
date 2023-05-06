using System.Diagnostics;

namespace DummyClient
{
    internal class Program
    {
        private static List<DummyClient> clients = new();
        private static bool ShouldStop = false;
        private static readonly int ClientCount = 64;

        private static void Main(string[] args)
        {
            // Create clients
            for (int i = 0; i < ClientCount; i++)
            {
                clients.Add(new DummyClient(i));
            }
            Console.WriteLine("Create dummy clients success");

            // Try connect to server
            foreach (DummyClient client in clients)
            {
                client.Connect();
                Console.WriteLine($"Try connect client[{client.ClientID}]...");
            }

            // Start process logic thread
            Thread loop = new Thread(ProcessLoop);
            loop.Start();

            // Stop process
            while (Console.ReadLine() != "q") ;
            ShouldStop = true;
            Console.WriteLine("Stop dummy client program...");

            // Join logic thread
            loop.Join();
            Console.WriteLine("Join logic thread...");

            // Dispose sockets
            try
            {
                foreach (DummyClient client in clients)
                {
                    client.Disconnect();
                    Console.WriteLine($"Client[{client.ClientID}] disconnected! UserID[{client.UserID}]");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("Clear all sockets...");
            Console.ReadLine();
        }

        public static void ProcessLoop()
        {
            Stopwatch st = new Stopwatch();
            st.Start();
            while (!ShouldStop)
            {
                long deltaMilliseconds = st.ElapsedMilliseconds;
                float dt = deltaMilliseconds / 1000.0f;
                st.Restart();
                foreach (var client in clients)
                {
                    client.Update(dt);
                }
                Thread.Sleep(Math.Max(0, (int)(33 - st.ElapsedMilliseconds)));
            }
        }
    }
}