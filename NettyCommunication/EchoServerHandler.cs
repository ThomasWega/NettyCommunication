using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using System.Collections.Concurrent;
using System.Text;

namespace NettyCommunication
{
    public class EchoServerHandler : ChannelHandlerAdapter
    {
        private static readonly ConcurrentDictionary<IChannel, string> ConnectedClients = new();

        public override void ChannelActive(IChannelHandlerContext context)
        {
            // Add new client to the list of connected clients
            ConnectedClients.TryAdd(context.Channel, $"Client {ConnectedClients.Count + 1}");
            Console.WriteLine($"New client connected: {ConnectedClients[context.Channel]}");
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            if (message is not IByteBuffer buffer) return;
            string received = buffer.ToString(Encoding.UTF8);
            string clientName = ConnectedClients[context.Channel];
            Console.WriteLine($"{clientName} sent: {received}");

            // Broadcast the message to all connected clients
            foreach (var client in ConnectedClients)
            {
                if (!Equals(client.Key, context.Channel))
                {
                    client.Key.WriteAndFlushAsync(buffer.Duplicate());
                }
            }
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            // Remove the client from the list of connected clients
            if (ConnectedClients.TryRemove(context.Channel, out var clientName))
            {
                Console.WriteLine($"{clientName} disconnected");
            }
        }

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Console.WriteLine($"Server exception: {exception}");
            context.CloseAsync();
        }
    }
}