using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace RotoGLBridge.Services
{
    public interface IFollowTargetCalculator
    {
        IObservable<float> FollowAngleObservable { get; }

        float FollowAngle { get; }

        float Update(float targetAngle, float followAngle);

        void Reset();
    }

    public class FollowTargetCalculator(MathService mathService) : IFollowTargetCalculator
    {
        private float? _initialTargetAngle = null;

        private float? _initialFollowAngle = null;

        private BehaviorSubject<float> FollowAngleSubject = new BehaviorSubject<float>(0f);

        public IObservable<float> FollowAngleObservable => FollowAngleSubject.AsObservable();

        public float FollowAngle => FollowAngleSubject.Value;


        private void Initialize(float targetAngle, float followAngle)
        {
            _initialFollowAngle = followAngle;
            _initialTargetAngle = targetAngle;
        }

        public void Reset()
        {
            _initialTargetAngle = null;
            _initialFollowAngle = null;
        }

        public float Update(float targetAngle, float followAngle)
        {
            if (_initialTargetAngle == null)
            {
                Initialize(targetAngle, followAngle);
                FollowAngleSubject.OnNext(followAngle);
                return followAngle;
            }


            // Calculate how much the target has moved since initialization
            float targetDelta = mathService.CalculateDeltaAngle(_initialTargetAngle.Value, targetAngle);

            float followDelta = mathService.CalculateDeltaAngle(_initialFollowAngle.Value, followAngle);


            var deltaDifference = GetDeltaDifference(targetDelta, followDelta);

            if (Math.Abs(deltaDifference) > 1)
            {
                // Update the follow angle to reduce the difference
                var n = mathService.NormalizeAngle(followAngle + deltaDifference);

                FollowAngleSubject.OnNext(n);

                return n;
            }
            else
            {
                // TODO: If not difference track for an amount of time and then Reset.
            }

            FollowAngleSubject.OnNext(followAngle);
            return followAngle;

        }

        internal float GetDeltaDifference(float targetDelta, float followDelta)
        {
            float deltaDifference = mathService.CalculateDeltaAngle(followDelta, targetDelta);

            return deltaDifference;
        }
    }
}
