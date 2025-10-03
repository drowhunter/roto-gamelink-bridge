


namespace rotoUSB
{
    public class RotoStatus : ICloneable
    {

        // Connection status
        public bool USBConnected;  //  USB connected
        public bool HTConnected;   // Headtracker connected
        public bool AndroidConnected; // Android connected


        // HeadTracker status
        public int  HTDegree;         // headtracker compass degree ( 0-359 )
        public bool HTEnabled;        // headtracker enabled (Taken control)

        public int HTSensitivityDegree; // headtracker built-in sensitivity angle
        public bool HT_Calibrated;      // headtracker calibrated
        public bool HT_IR_detected;     // headtracker IR detected (Head detected)


        // Chair status
        public int ChairVersion;        // chair hardware version (Should be 2)
        public double BaseDegree;       // chair base angle degree 
        public int FirmwareVersion;     // chair firmware version
        public int ErrorMode;
         
        public int RunMode;             // chair current operation mode
        public int MaxPowerLimit;       // chair maximum power limit
        public int CockpitDegreeLimit;  // chair cockpit degree limit 

 
        public object Clone()
        {
            
            return new RotoStatus
            {
                USBConnected = this.USBConnected,
                HTConnected = this.HTConnected,
                AndroidConnected = this.AndroidConnected,

                HTDegree = this.HTDegree,
                HTEnabled = this.HTEnabled,
                HTSensitivityDegree = this.HTSensitivityDegree,
                HT_Calibrated = this.HT_Calibrated,
                HT_IR_detected = this.HT_IR_detected,

                ChairVersion = this.ChairVersion,
                BaseDegree = this.BaseDegree,
                FirmwareVersion = this.FirmwareVersion,
                ErrorMode = this.ErrorMode,

                RunMode = this.RunMode,
                MaxPowerLimit = this.MaxPowerLimit,
                CockpitDegreeLimit = this.CockpitDegreeLimit,
            };
            
        }

    }
}
