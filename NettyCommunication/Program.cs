namespace NettyCommunication;

class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Select mode: (1) Server, (2) Client");
        string input = Console.ReadLine();

        if (input == "1")
        {
            Console.WriteLine("Starting server...");
            await SimpleServer.RunServerAsync();
        }
        else if (input == "2")
        {
            Console.WriteLine("Enter the message to send:");
            string message = Console.ReadLine();
            Console.WriteLine("Starting client...");
            await SimpleClient.RunClientAsync(message);
        }
        else
        {
            Console.WriteLine("Invalid selection. Please enter '1' for Server or '2' for Client.");
        }
    }
}