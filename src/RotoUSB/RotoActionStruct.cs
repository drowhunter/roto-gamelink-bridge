using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace rotoUSB
{
    internal class RotoActionStruct : IRotoActionStruct
    {

        private readonly object _lockChairStatus = new object();


        // chair target speed and target position
        private int _targetChairSpeed;
        private int _targetChairDegree;

        // object follow target degree
        private int _targetObjectDegree;


        // rumble power and rumble duration
        private int _targetRumblePower;
        private int _targetRumbleDurationMS;


        // no use
        private int _lastTargetChairDegree;

 
        private bool _isMotorChanged;
        private bool _isRumbleChanged;



        public RotoActionStruct()
        {
            Reset();
        }


        public void Reset()
        {
            lock (_lockChairStatus)
            {
                _targetChairSpeed = 0;
                _targetObjectDegree = 0;

                _targetChairDegree = 0;

                _targetRumblePower = -1;
                _targetRumbleDurationMS = 0;
                _lastTargetChairDegree = 0;
                _isMotorChanged = false;
                _isRumbleChanged = false;
            }
        }


        private static  int Clamp(int value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }


       
        public void UpdateChairSpeed(int speed, int degree)
        {
            lock (_lockChairStatus)
            {
                _targetChairSpeed = speed;
                _targetChairDegree = degree;

                Console.WriteLine("chair speed "+ speed + " and degree " + degree);
                
                _isMotorChanged = true;
            }
        }

        public void UpdateObjectFollowDegree(int speed, int degree)
        {
            lock (_lockChairStatus)
            {
                _targetChairSpeed = speed;
                _targetObjectDegree = (degree % 360); //   Clamp(degree, 0, 359);

                Console.WriteLine("Update OF speed " + _targetChairSpeed + " and degree " + _targetObjectDegree);

                _isMotorChanged = true;
            }
        }

        public void UpdateRumble(int power, int milliSeconds)
        {
            lock (_lockChairStatus)
            {
                _targetRumblePower = Clamp(power, 0, 100);
                _targetRumbleDurationMS = Clamp(milliSeconds, 0, 25500);
                _isRumbleChanged = true;
            }
        }


        // out motor change , out motor 

        // Get all values atomically
        public bool GetRotoAction(out bool motorChanged, out int chairSpeed, out int objectDegree, out int chairAngle, out bool rumbleChanged, out int rumblePower, out int rumbleDurationMS)
        {

            bool valueChanged = false;

             

            //Console.WriteLine($"GetRotoAction   {DateTime.Now} - 1");
            lock (_lockChairStatus)
            {
                // is  rumble / motor value change?
                valueChanged = _isRumbleChanged || _isMotorChanged;
                chairSpeed = _targetChairSpeed;
                objectDegree = _targetObjectDegree;
                rumblePower = _targetRumblePower;
                rumbleDurationMS = _targetRumbleDurationMS;

                motorChanged = _isMotorChanged;
                rumbleChanged = _isRumbleChanged;

                chairAngle = _targetChairDegree;


                if (_isMotorChanged)
                {
                    //_targetChairSpeed = 0;
                   // _targetChairDegree = 0;

                   // _targetObjectDegree = 0;
                    _lastTargetChairDegree = objectDegree;

                    _isMotorChanged = false;
                }


                if (_isRumbleChanged)
                {
                    _targetRumblePower = -1;
                    _targetRumbleDurationMS = 0;
              
                    _isRumbleChanged = false;
                }
        
            }
            //Console.WriteLine($"GetRotoAction   {DateTime.Now} - 2");

            return valueChanged;
        }




    }
}
