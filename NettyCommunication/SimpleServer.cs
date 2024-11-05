namespace NettyCommunication;

using DotNetty.Buffers;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DotNetty.Transport.Channels.Groups;


public class SimpleServer
{
    private IChannel boundChannel;
    private IEventLoopGroup bossGroup;
    private IEventLoopGroup workerGroup;

    public async Task RunServerAsync(int port)
    {
        bossGroup = new MultithreadEventLoopGroup(1);
        workerGroup = new MultithreadEventLoopGroup();

        try
        {
            var bootstrap = new ServerBootstrap();
            bootstrap.Group(bossGroup, workerGroup)
                .Channel<TcpServerSocketChannel>()
                .Option(ChannelOption.SoBacklog, 100)
                .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;
                    pipeline.AddLast(new BaseServerHandler());
                }));

            var endpoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), port);
            boundChannel = await bootstrap.BindAsync(endpoint);

            Console.WriteLine($"Server started on port {port}");
            Console.WriteLine("Press Enter to stop the server...");
            await Task.Run(() => Console.ReadLine());
        }
        finally
        {
            await ShutdownAsync();
        }
    }

    public async Task ShutdownAsync()
    {
        if (boundChannel != null)
            await boundChannel.CloseAsync();

        if (bossGroup != null && workerGroup != null)
        {
            await Task.WhenAll(
                bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)),
                workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1))
            );
        }
    }
}

public class BaseServerHandler : SimpleChannelInboundHandler<IByteBuffer>
{
    // FIXME: figure out channel grouping
    private static readonly IChannelGroup Channels = new DefaultChannelGroup();

    public override void ChannelActive(IChannelHandlerContext context)
    {
        var channel = context.Channel;
        Channels.Add(channel);
        Console.WriteLine($"Client connected: {channel.RemoteAddress}");
    }

    public override void ChannelInactive(IChannelHandlerContext context)
    {
        var channel = context.Channel;
        Channels.Remove(channel);
        Console.WriteLine($"Client disconnected: {channel.RemoteAddress}");
    }

    protected override void ChannelRead0(IChannelHandlerContext ctx, IByteBuffer msg)
    {
        string received = msg.ToString(Encoding.UTF8);
        Console.WriteLine($"Server received from {ctx.Channel.RemoteAddress}: {received}");

        // Broadcast the message to all connected clients
        var buffer = Unpooled.WrappedBuffer(Encoding.UTF8.GetBytes($"[{ctx.Channel.RemoteAddress}]: {received}"));
        foreach (var channel in Channels)
        {
            if (channel != ctx.Channel) // Don't send back to sender
            {
                channel.WriteAndFlushAsync(buffer.RetainedDuplicate());
            }
        }

        buffer.Release();
    }

    public override void ExceptionCaught(IChannelHandlerContext ctx, Exception e)
    {
        Console.WriteLine($"Server caught exception from {ctx.Channel.RemoteAddress}: {e}");
        ctx.CloseAsync();
    }
}