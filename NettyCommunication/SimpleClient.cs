namespace NettyCommunication;

using DotNetty.Buffers;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System;
using System.Text;
using System.Threading.Tasks;

public class SimpleClient
{
    private const string Host = "127.0.0.1";
    private const int Port = 8007;

    public static async Task RunClientAsync(string message)
    {
        var group = new MultithreadEventLoopGroup();

        try
        {
            var bootstrap = new Bootstrap();
            bootstrap.Group(group)
                .Channel<TcpSocketChannel>()
                .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;
                    pipeline.AddLast(new SimpleClientHandler());
                }));

            IChannel clientChannel = await bootstrap.ConnectAsync(Host, Port);

            // Send a message to the server
            var initialMessage = Unpooled.WrappedBuffer(Encoding.UTF8.GetBytes(message));
            await clientChannel.WriteAndFlushAsync(initialMessage);

            // Wait until the connection is closed
            await clientChannel.CloseCompletion;
        }
        finally
        {
            await group.ShutdownGracefullyAsync();
        }
    }
}

public class SimpleClientHandler : SimpleChannelInboundHandler<IByteBuffer>
{
    protected override void ChannelRead0(IChannelHandlerContext ctx, IByteBuffer msg)
    {
        string received = msg.ToString(Encoding.UTF8);
        Console.WriteLine($"Client received: {received}");
    }

    public override void ExceptionCaught(IChannelHandlerContext ctx, Exception e)
    {
        Console.WriteLine($"Client caught exception: {e}");
        ctx.CloseAsync();
    }
}
