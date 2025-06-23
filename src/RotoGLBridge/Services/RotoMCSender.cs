
using RotoGLBridge.Models;

using Sharpie.Helpers.Telemetry;


namespace RotoGLBridge.Services
{
    public interface IMmfSender
    {
        void Send(float angle);
    }

    public class RotoMCSender: IMmfSender
    {



        MmfTelemetry<float> mmf;

        public RotoMCSender()
        {
            mmf = new(new( "RotoVrMotionRigPose",true), new MarshalByteConverter<float>());
        }

        public void Send(float angle)
        {
            mmf.Send(angle);
        }

    }

    public class FlyPtSender : IMmfSender
    {
        
        MmfTelemetry<SixDofTracker> mmf;


        public FlyPtSender()
        {
            mmf = new MmfTelemetry<SixDofTracker>(new("motionRigPose", true), 
                converter: new MarshalByteConverter<SixDofTracker>());

        }

        public void Send(float angle)
        {
            mmf.Send(new SixDofTracker() { yaw = angle });
        }
    }
}
