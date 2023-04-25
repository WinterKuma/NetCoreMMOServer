using NetCoreMMOServer.Network;

namespace NetCoreMMOServer
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            MMOServer server = new(8080);
            server.Start();

            while (true) { }
        }
    }
}