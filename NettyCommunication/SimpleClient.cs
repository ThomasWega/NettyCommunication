using DotNetty.Buffers;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System.Net;
using System.Text;

namespace NettyCommunication
{
    public class SimpleClient
    {
        private IChannel clientChannel;
        private IEventLoopGroup group;

        public async Task ConnectAsync(string host, int port)
        {
            group = new MultithreadEventLoopGroup();

            try
            {
                var bootstrap = new Bootstrap();
                bootstrap
                    .Group(group)
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        pipeline.AddLast(new BaseClientHandler());
                    }));

                var endpoint = new IPEndPoint(IPAddress.Parse(host), port);
                Console.WriteLine($"Connecting to {endpoint}...");
                clientChannel = await bootstrap.ConnectAsync(endpoint);
                Console.WriteLine("Connected successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection failed: {ex.Message}");
                await DisconnectAsync();
                throw;
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (clientChannel?.Active != true)
            {
                throw new InvalidOperationException("Client is not connected");
            }

            try
            {
                var buffer = Unpooled.Buffer();
                buffer.WriteBytes(Encoding.UTF8.GetBytes(message));
                await clientChannel.WriteAndFlushAsync(buffer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send message: {ex.Message}");
                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            if (clientChannel != null)
            {
                await clientChannel.CloseAsync();
            }

            if (group != null)
            {
                await group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            }
        }
    }
}

public class BaseClientHandler : SimpleChannelInboundHandler<IByteBuffer>
{
    protected override void ChannelRead0(IChannelHandlerContext ctx, IByteBuffer msg)
    {
        string received = msg.ToString(Encoding.UTF8);
        Console.WriteLine($"\nReceived: {received}");
        Console.Write("Enter message (or 'exit' to quit): "); // Restore the prompt
    }

    public override void ExceptionCaught(IChannelHandlerContext ctx, Exception e)
    {
        Console.WriteLine($"\nError: {e.Message}");
        Console.Write("Enter message (or 'exit' to quit): "); // Restore the prompt
        ctx.CloseAsync();
    }
}