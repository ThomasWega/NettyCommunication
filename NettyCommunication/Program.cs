using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System.Net;
using System.Text;
using DotNetty.Buffers;

namespace NettyCommunication
{
    static class Program
    {
        static void Main()
        {
            Console.WriteLine("Select mode: (1) Server, (2) Client");
            string? input = Console.ReadLine();

            if (input == "1")
            {
                Console.Write("Enter port to listen on (default 8007): ");
                string? portInput = Console.ReadLine();
                // FIXME make sure its also a valid port number
                int port = string.IsNullOrEmpty(portInput) ? 8007 : int.Parse(portInput);

                RunServerAsync(port).Wait();
            }
            else if (input == "2")
            {
                Console.Write("Enter server IP (default 127.0.0.1): ");
                string? ip = Console.ReadLine();
                ip = string.IsNullOrEmpty(ip) ? "127.0.0.1" : ip;

                Console.Write("Enter server port (default 8007): ");
                string? portInput = Console.ReadLine();
                int port = string.IsNullOrEmpty(portInput) ? 8007 : int.Parse(portInput);

                RunClientAsync(ip, port).Wait();
            }
            else
            {
                Console.WriteLine("Invalid selection. Please enter '1' for Server or '2' for Client.");
            }
        }

        static async Task RunServerAsync(int port)
        {
            IEventLoopGroup bossGroup = new MultithreadEventLoopGroup(1);
            IEventLoopGroup workerGroup = new MultithreadEventLoopGroup();

            try
            {
                var bootstrap = new ServerBootstrap();
                bootstrap.Group(bossGroup, workerGroup)
                    .Channel<TcpServerSocketChannel>()
                    .Option(ChannelOption.SoBacklog, 100)
                    .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
                        pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));
                        pipeline.AddLast("echo", new EchoServerHandler());
                    }));

                var endpoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), port);
                IChannel boundChannel = await bootstrap.BindAsync(endpoint);

                Console.WriteLine($"Server started on port {port}");
                Console.WriteLine("Press Enter to stop the server...");
                await Task.Run(() => Console.ReadLine());
            }
            finally
            {
                await bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
                await workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            }
        }

        static async Task RunClientAsync(string host, int port)
        {
            var group = new MultithreadEventLoopGroup();

            try
            {
                var bootstrap = new Bootstrap();
                bootstrap.Group(group)
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
                        pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));
                        pipeline.AddLast("echo", new EchoClientHandler());
                    }));

                var endpoint = new IPEndPoint(IPAddress.Parse(host), port);
                IChannel clientChannel = await bootstrap.ConnectAsync(endpoint);

                // Start message loop
                while (true)
                {
                    Console.WriteLine("Enter message (or 'exit' to quit): ");
                    string? message = Console.ReadLine();
                    if (message?.ToLower() == "exit")
                        break;
                    if (message != null)
                    {
                        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                        await clientChannel.WriteAndFlushAsync(Unpooled.WrappedBuffer(messageBytes));   
                    }
                }
            }
            finally
            {
                await group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            }
        }
    }
}