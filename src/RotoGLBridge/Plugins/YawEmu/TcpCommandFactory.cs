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
            if(bytes == null || bytes.Length < 1)
            {
                return null;
            }

            var cmd = (CommandEnum)bytes[0];

            switch (cmd)
            {
                case CommandEnum.CHECK_IN:
                    
                    return new Commands.CheckInCommand
                    {
                        UdpPort = BitConverter.ToInt32(bytes[1..5].Reverse().ToArray()) ,
                        GameName = Encoding.ASCII.GetString(bytes, 5, bytes.Length - 5).TrimEnd()
                    };

                case CommandEnum.SET_POWER:

                    return new Commands.SetPowerCommand { Power = BitConverter.ToInt32(bytes[1..5].Reverse().ToArray()) };
                // Add more cases for other command types as needed
                default:
                    return null;
            }

        }

        
    }
}
