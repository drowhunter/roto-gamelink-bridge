
using RotoGLBridge.Models;

using Sharpie.Helpers.Telemetry;


namespace RotoGLBridge.Services
{
    public interface IMmfSender
    {
        void Send(SixDofTracker tracker);
    }
    
    /// <summary>
    /// Send Yaw using roto tracker
    /// </summary>
    public class RotoMCSender: IMmfSender
    {



        MmfTelemetry<float> mmf;

        public RotoMCSender()
        {
            mmf = new(new( "RotoVrMotionRigPose",true), new MarshalByteConverter<float>());
        }

        public void Send(SixDofTracker tracker)
        {
            mmf.Send((float)tracker.yaw);
        }

    }

    /// <summary>
    /// Send flypt tracker format (compatible with openxr and openvr motion comp
    /// </summary>
    public class FlyPtSender : IMmfSender
    {
        
        MmfTelemetry<SixDofTracker> mmf;


        public FlyPtSender()
        {
            mmf = new MmfTelemetry<SixDofTracker>(new("motionRigPose", true) { Create = true }, 
                converter: new MarshalByteConverter<SixDofTracker>());

        }

        public void Send(SixDofTracker tracker)
        {
            mmf.Send(tracker);
        }
    }
}
