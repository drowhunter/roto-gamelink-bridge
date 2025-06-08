using Sharpie.Extras.Telemetry;

namespace RotoGLBridge.Plugins.YawEmu
{
    internal class YawTcpCommandConverter : IByteConverter<ITcpCommand> 
    {
        public ITcpCommand FromBytes(byte[] data) => TcpCommandFactory.Parse(data);

        

        public byte[] ToBytes(ITcpCommand data) => data.ToBytes();
                
    }
}
