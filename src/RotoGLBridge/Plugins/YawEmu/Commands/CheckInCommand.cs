using System.Text;

namespace RotoGLBridge.Plugins.YawEmu.Commands
{
    internal record CheckInCommand : ITcpCommand
    {
        public CommandEnum CommandId => CommandEnum.CHECK_IN; // Example command ID, adjust as needed

        public int UdpPort { get; set; }

        public string GameName { get; set; }

        public byte[] ToBytes()
        {
            var gameNameBytes = Encoding.UTF8.GetBytes(GameName);
            var buffer = new byte[5 + gameNameBytes.Length];
            buffer[0] = (byte)CommandEnum.CHECK_IN;
            BitConverter.GetBytes(UdpPort).CopyTo(buffer, 1);
            gameNameBytes.CopyTo(buffer, 5);
            return buffer;
        }

    }

}
