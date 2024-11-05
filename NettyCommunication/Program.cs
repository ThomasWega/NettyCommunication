namespace NettyCommunication;

class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Select mode: (1) Server, (2) Client");
        string input = Console.ReadLine();

        if (input == "1")
        {
            Console.Write("Enter port to listen on (default 8007): ");
            string portInput = Console.ReadLine();
            int port = string.IsNullOrEmpty(portInput) ? 8007 : int.Parse(portInput);

            Console.WriteLine("Starting server...");
            var server = new SimpleServer();
            await server.RunServerAsync(port);
        }
        else if (input == "2")
        {
            Console.Write("Enter server IP (default 127.0.0.1): ");
            string ip = Console.ReadLine();
            ip = string.IsNullOrEmpty(ip) ? "127.0.0.1" : ip;

            Console.Write("Enter server port (default 8007): ");
            string portInput = Console.ReadLine();
            int port = string.IsNullOrEmpty(portInput) ? 8007 : int.Parse(portInput);

            Console.WriteLine("Starting client...");
            var client = new SimpleClient();
            await client.ConnectAsync(ip, port);

            // Start message loop
            while (true)
            {
                Console.Write("Enter message (or 'exit' to quit): ");
                string message = Console.ReadLine();
                if (message?.ToLower() == "exit")
                    break;

                await client.SendMessageAsync(message);
            }

            await client.DisconnectAsync();
        }
        else
        {
            Console.WriteLine("Invalid selection. Please enter '1' for Server or '2' for Client.");
        }
    }
}