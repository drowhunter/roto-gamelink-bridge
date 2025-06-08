using System.Text;

namespace RotoGLBridge.Plugins.YawEmu
{
    public interface ITcpCommand
    {
        CommandEnum CommandId { get; }

        byte[] ToBytes();
    }

    

    public class TcpCommandFactory
    {

        public static ITcpCommand Parse(byte[] bytes) 
        {
            var cmd = (CommandEnum)bytes[0];

            switch (cmd)
            {
                case CommandEnum.CHECK_IN:
                    
                    return new Commands.CheckInCommand
                    {
                        UdpPort = BitConverter.ToInt32(bytes[1..5].Reverse().ToArray()) ,
                        GameName = Encoding.ASCII.GetString(bytes, 5, bytes.Length - 5).TrimEnd()
                    };
               
                // Add more cases for other command types as needed
                default:
                    return null;
            }

        }

        
    }
}
