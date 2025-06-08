namespace RotoGLBridge.Plugins.YawEmu.Commands
{
    internal record SetPowerCommand : ITcpCommand
    {
        public CommandEnum CommandId => CommandEnum.SET_POWER;

        public int Power { get; set; }

        public byte[] ToBytes()
        {
            byte[] b = BitConverter.GetBytes(Power);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(b); // Ensure big-endian format
            }

            byte[] buffer = new byte[1 + b.Length];
            buffer[0] = (byte)CommandId;
            b.CopyTo(buffer, 1);
            return buffer;
        }
    }

}
