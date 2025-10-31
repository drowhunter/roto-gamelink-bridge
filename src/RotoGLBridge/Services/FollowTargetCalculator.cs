using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace RotoGLBridge.Services
{
    public interface IFollowTargetCalculator
    {
        /// <summary>
        /// The current follow angle.
        /// </summary>
        float? CurrentFollowAngle { get; }

        /// <summary>
        /// The current target angle.
        /// </summary>
        float? CurrentTargetAngle { get; }


        /// <summary>
        /// The initial follow angle when tracking started.
        /// </summary>
        float? InitialFollowAngle { get; }


        /// <summary>
        /// The initial target angle when tracking started.
        /// </summary>
        float? InitialTargetAngle { get; }

        

        /// <summary>
        /// The difference between the initial target angle and the current follow angle. (-180 to 180)
        /// </summary>
        float TargetOffset { get; }

        /// <summary>
        /// The difference between the initial follow angle and the current follow angle. (-180 to 180)
        /// </summary>
        float FollowOffset { get; }

        /// <summary>
        /// The difference between the target offset and the follow offset.
        /// </summary>
        float OffsetDifference { get; }


        /// <summary>
        /// The new follow angle to apply in order to reduce the offset difference.(0 - 360)
        /// </summary>
        float NewFollowAngle { get; }

        /// <summary>
        /// Resets the initial target and follow angles to their default state.
        /// </summary>
        /// <remarks>This method clears any previously set values for the initial target and follow
        /// angles, setting them to null. It can be used to reinitialize the angles before recalculating or reapplying
        /// new values.</remarks>
        void Reset();


        /// <summary>
        /// Updates the current target and follow angles. If the initial target angle is not set, both angles will be initialized with the provided values.
        /// </summary>
        /// <param name="targetAngle"></param>
        /// <param name="followAngle"></param>
        void Update(float targetAngle, float followAngle);
    }



    public class FollowTargetCalculator(MathService mathService) : IFollowTargetCalculator
    {
        private float? _initialTargetAngle = null;
        private float? _initialFollowAngle = null;
        private float? _currentTargetAngle = null;
        private float? _currentFollowAngle = null;

        
        public float? InitialFollowAngle => _initialFollowAngle;

        public float? InitialTargetAngle => _initialTargetAngle;

        public float? CurrentTargetAngle => _currentTargetAngle;

        public float? CurrentFollowAngle => _currentFollowAngle;




        private BehaviorSubject<float> _newFollowAngleSubject = new BehaviorSubject<float>(0f);

        public IObservable<float> NewFollowAngleObservable => _newFollowAngleSubject.AsObservable();


        public float TargetOffset
        {
            get
            {
                if (InitialTargetAngle == null || CurrentTargetAngle== null)
                {
                    return 0;
                }
                return mathService.CalculateDeltaAngle(_initialTargetAngle.Value, CurrentTargetAngle.Value);
            }
        }

        
        public float FollowOffset
        {
            get
            {
                if (InitialFollowAngle == null || CurrentFollowAngle == null)
                {
                    return 0;
                }
                return mathService.CalculateDeltaAngle(_initialFollowAngle.Value, CurrentFollowAngle.Value);
            }
        }

        
        public float OffsetDifference
        {
            get
            {
                float d = mathService.CalculateDeltaAngle(FollowOffset, TargetOffset);

                return d;
            }
        }


        public float NewFollowAngle => _newFollowAngleSubject.Value;

        public void Reset()
        {
            _initialTargetAngle = null;
            _initialFollowAngle = null;
        }


        public void Update(float targetAngle, float followAngle)
        {
            _currentTargetAngle = targetAngle;
            _currentFollowAngle = followAngle;

            if (_initialTargetAngle == null)
            {
                _initialTargetAngle = targetAngle;
                _initialFollowAngle = followAngle;

                _newFollowAngleSubject.OnNext(followAngle);
            }


            if (Math.Abs(OffsetDifference) > 1)
            {
                // Update the follow angle to reduce the difference
                var n = mathService.NormalizeAngle(followAngle + OffsetDifference);

                _newFollowAngleSubject.OnNext(n);

                return;
            }
            else
            {
                // TODO: If not difference track for an amount of time and then Reset.
            }

            //_newFollowAngleSubject.OnNext(followAngle);
            return;

        }


    }
}
