using NetCoreMMOServer.Network;

namespace NetCoreMMOServer
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            MMOServer server = new(8080);
            await server.StartAsync();

            while (true) { }
        }
    }
}