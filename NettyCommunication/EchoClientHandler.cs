using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using System.Text;

namespace NettyCommunication
{
    public class EchoClientHandler : ChannelHandlerAdapter
    {
        private readonly IByteBuffer _initialMessage;
        private string _clientName;

        public EchoClientHandler()
        {
            _initialMessage = Unpooled.Buffer(256);
            byte[] messageBytes = "Hello from client"u8.ToArray();
            _initialMessage.WriteBytes(messageBytes);
            _clientName = "Anonymous";
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            Console.Write("Enter your name: ");
            _clientName = Console.ReadLine() ?? "Anonymous";
            context.WriteAndFlushAsync(_initialMessage);
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var buffer = message as IByteBuffer;
            if (buffer != null)
            {
                Console.WriteLine($"{_clientName} received: {buffer.ToString(Encoding.UTF8)}");
            }
        }

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Console.WriteLine($"{_clientName} exception: {exception}");
            context.CloseAsync();
        }

        public override void UserEventTriggered(IChannelHandlerContext context, object evt)
        {
            if (evt is IByteBuffer message)
            {
                Console.WriteLine($"{_clientName}: ");
                context.WriteAndFlushAsync(message);
            }
        }
    }
}