namespace NettyCommunication;

using DotNetty.Buffers;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

public class SimpleServer
{
    private const int Port = 8007;

    public static async Task RunServerAsync()
    {
        var bossGroup = new MultithreadEventLoopGroup(1);
        var workerGroup = new MultithreadEventLoopGroup();

        try
        {
            var bootstrap = new ServerBootstrap();
            bootstrap.Group(bossGroup, workerGroup)
                     .Channel<TcpServerSocketChannel>()
                     .Option(ChannelOption.SoBacklog, 100)
                     .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                     {
                         IChannelPipeline pipeline = channel.Pipeline;
                         pipeline.AddLast(new SimpleServerHandler());
                     }));

            // Bind the server to an IP address
            var localIpAddress = GetLocalIpAddress();
            IChannel boundChannel = await bootstrap.BindAsync(new IPEndPoint(localIpAddress, Port));

            Console.WriteLine($"Server started on IP {localIpAddress} and port {Port}");
            await boundChannel.CloseCompletion;
        }
        finally
        {
            await Task.WhenAll(bossGroup.ShutdownGracefullyAsync(), workerGroup.ShutdownGracefullyAsync());
        }
    }

    // Helper method to get the local IP address
    private static IPAddress GetLocalIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip;
            }
        }
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }
}

public class SimpleServerHandler : SimpleChannelInboundHandler<IByteBuffer>
{
    protected override void ChannelRead0(IChannelHandlerContext ctx, IByteBuffer msg)
    {
        string received = msg.ToString(Encoding.UTF8);
        Console.WriteLine($"Server received: {received}");

        // Send the message back to the client (echo)
        var buffer = Unpooled.WrappedBuffer(Encoding.UTF8.GetBytes($"Server Echo: {received}"));
        ctx.WriteAndFlushAsync(buffer);
    }

    public override void ExceptionCaught(IChannelHandlerContext ctx, Exception e)
    {
        Console.WriteLine($"Server caught exception: {e}");
        ctx.CloseAsync();
    }
}
