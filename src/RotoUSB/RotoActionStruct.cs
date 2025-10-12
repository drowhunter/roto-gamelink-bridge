using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace rotoUSB
{
    /// <summary>
    /// Represents the structure for managing chair and rumble actions in the RotoUSB system.
    /// Provides thread-safe methods to update and retrieve chair movement and rumble parameters.
    /// </summary>
    internal class RotoActionStruct : IRotoActionStruct
    {
        /// <summary>
        /// Synchronization object for thread-safe access to chair status fields.
        /// </summary>
        private readonly object _lockChairStatus = new object();

        /// <summary>
        /// Target speed for the chair motor.
        /// </summary>
        private int _targetChairSpeed;

        /// <summary>
        /// Target angle (degree) for the chair position.
        /// </summary>
        private int _targetChairDegree;

        /// <summary>
        /// Target angle (degree) for object-follow mode.
        /// </summary>
        private int _targetObjectDegree;

        /// <summary>
        /// Target power for the rumble motor (0-100).
        /// </summary>
        private int _targetRumblePower;

        /// <summary>
        /// Target duration for the rumble motor in milliseconds (0-65535).
        /// </summary>
        private int _targetRumbleDurationMS;

        /// <summary>
        /// Stores the last target chair degree (not currently used).
        /// </summary>
        private int _lastTargetChairDegree;

        /// <summary>
        /// Indicates whether the motor parameters have changed since the last retrieval.
        /// </summary>
        private bool _isMotorChanged;

        /// <summary>
        /// Indicates whether the rumble parameters have changed since the last retrieval.
        /// </summary>
        private bool _isRumbleChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="RotoActionStruct"/> class and resets all parameters.
        /// </summary>
        public RotoActionStruct()
        {
            Reset();
        }

        /// <summary>
        /// Resets all chair and rumble parameters to their default values.
        /// </summary>
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

        /// <summary>
        /// Clamps an integer value between a minimum and maximum value.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum allowed value.</param>
        /// <param name="max">The maximum allowed value.</param>
        /// <returns>The clamped value.</returns>
        private static int Clamp(int value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        /// <summary>
        /// Updates the chair's target speed and angle.
        /// </summary>
        /// <param name="speed">The target speed for the chair motor.</param>
        /// <param name="degree">The target angle (degree) for the chair position.</param>
        public void UpdateChairSpeed(int speed, int degree)
        {
            lock (_lockChairStatus)
            {
                _targetChairSpeed = speed;
                _targetChairDegree = degree;

                Console.WriteLine("chair speed " + speed + " and degree " + degree);

                _isMotorChanged = true;
            }
        }

        /// <summary>
        /// Updates the chair's target speed and object-follow angle.
        /// </summary>
        /// <param name="speed">The target speed for the chair motor.</param>
        /// <param name="degree">The target angle (degree) for object-follow mode.</param>
        public void UpdateObjectFollowDegree(int speed, int degree)
        {
            lock (_lockChairStatus)
            {
                _targetChairSpeed = speed;
                _targetObjectDegree = (degree % 360);

                Console.WriteLine("Update OF speed " + _targetChairSpeed + " and degree " + _targetObjectDegree);

                _isMotorChanged = true;
            }
        }

        /// <summary>
        /// Updates the rumble motor's power and duration.
        /// </summary>
        /// <param name="power">The target power for the rumble motor (0-100).</param>
        /// <param name="milliSeconds">The target duration for the rumble motor in milliseconds (0-65535).</param>
        public void UpdateRumble(int power, int milliSeconds)
        {
            lock (_lockChairStatus)
            {
                _targetRumblePower = Clamp(power, 0, 100);
                _targetRumbleDurationMS = Clamp(milliSeconds, 0, 65535);
                _isRumbleChanged = true;
            }
        }

        /// <summary>
        /// Atomically retrieves all chair and rumble parameters, and resets change flags as appropriate.
        /// </summary>
        /// <param name="motorChanged">Indicates if motor parameters have changed since last retrieval.</param>
        /// <param name="chairSpeed">The current target speed for the chair motor.</param>
        /// <param name="objectDegree">The current target angle for object-follow mode.</param>
        /// <param name="chairAngle">The current target angle for the chair position.</param>
        /// <param name="rumbleChanged">Indicates if rumble parameters have changed since last retrieval.</param>
        /// <param name="rumblePower">The current target power for the rumble motor.</param>
        /// <param name="rumbleDurationMS">The current target duration for the rumble motor in milliseconds.</param>
        /// <returns>True if any parameters have changed since last retrieval; otherwise, false.</returns>
        public bool GetRotoAction(
            out bool motorChanged,
            out int chairSpeed,
            out int objectDegree,
            out int chairAngle,
            out bool rumbleChanged,
            out int rumblePower,
            out int rumbleDurationMS)
        {
            bool valueChanged = false;

            lock (_lockChairStatus)
            {
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

            return valueChanged;
        }
    }
}
