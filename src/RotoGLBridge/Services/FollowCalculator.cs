using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace RotoGLBridge.Services
{
    public interface IFollowCalculator
    {
        FollowResult LastResult { get; }
        /// <summary>
        /// The new follow angle to apply in order to reduce the offset difference.(0 - 360)
        /// </summary>
        float NewFollowAngle { get; }

        /// <summary>
        /// Gets an observable sequence that notifies subscribers when the follow angle changes.
        /// </summary>
        IObservable<FollowResult> OnAngleChangedObservable { get; }

        /// <summary>
        /// Resets the setial target and follow angles to their default state.
        /// </summary>
        /// <remarks>This method clears any previously set values for the setial target and follow
        /// angles, setting them to null. It can be used to resetialize the angles before recalculating or reapplying
        /// new values.</remarks>
        void Reset();


        /// <summary>
        /// Updates the current target and follow angles. If the setial target angle is not set, both angles will be setialized with the provided values.
        /// </summary>
        /// <param name="targetAngle"></param>
        /// <param name="followAngle"></param>
        FollowResult Update(float targetAngle, float followAngle);
    
    }

    public record FollowResult
    {
        public float? InitialTargetAngle { get; set; }

        public float? InitialFollowAngle { get; set; }

        public float? CurrentTargetAngle { get; set; }

        public float? CurrentFollowAngle { get; set; }


        public float NewFollowAngle { get; set; }

        public float OffsetDifference { get; set; }

        public float TargetOffset { get; set; }

        public float FollowOffset { get; set; }

    }


    public class FollowCalculator(MathService mathService, ILogger<FollowCalculator> logger) : IFollowCalculator
    {
        private float? _initialTargetAngle = null;

        private float? _initialFollowAngle = null;

        private BehaviorSubject<FollowResult> _angleChangedSubject = new(null);

        private DateTime _lastUpdated = DateTime.MinValue;

        public IObservable<FollowResult> OnAngleChangedObservable => _angleChangedSubject.Skip(1).AsObservable();

        public float NewFollowAngle => _angleChangedSubject.Value.NewFollowAngle;

        public FollowResult LastResult => _lastResult;

        private DateTime _now = DateTime.Now;
        public void Reset()
        {
            _initialTargetAngle = null;
            _initialFollowAngle = null;
            _lastUpdated = _now;
        }

        private FollowResult _lastResult = new();

        public FollowResult Update(float targetAngle, float followAngle)
        {
            _now = DateTime.Now;
            if(_now - _lastUpdated > _antiJump )
            {
                logger.LogWarning($"Anti Jump: Resetting due to time gap: {_now - _lastUpdated}");
                Reset();
            }

            if (_initialTargetAngle == null)
            {
                _initialTargetAngle = targetAngle;
                _initialFollowAngle = followAngle;
            }

            bool targetUnchanged = (_lastResult?.CurrentTargetAngle != null && MathF.Abs(mathService.CalculateOffsetAngle(_lastResult.CurrentTargetAngle.Value , targetAngle)) < 1);
            if (targetUnchanged)
            {
                HandleUnchanged();
                return null;
            }

            FollowResult result = new()
            {
                InitialTargetAngle = _initialTargetAngle,
                InitialFollowAngle = _initialFollowAngle,
                CurrentTargetAngle = targetAngle,
                CurrentFollowAngle = followAngle,
                TargetOffset = mathService.CalculateOffsetAngle(_initialTargetAngle.Value, targetAngle),
                FollowOffset = mathService.CalculateOffsetAngle(_initialFollowAngle.Value, followAngle),               
                
            };
            

            result.OffsetDifference = mathService.CalculateOffsetAngle(result.FollowOffset, result.TargetOffset);

            if (MathF.Abs(result.OffsetDifference) < 1)
            {
                result.NewFollowAngle = targetAngle;
                HandleUnchanged();
            }
            else
            {
                result.NewFollowAngle = mathService.NormalizeAngle(followAngle + result.OffsetDifference);
                _angleChangedSubject.OnNext(result);
                _lastUpdated = _now;
            }
            
            if (_lastResult != result)
            {
                _lastResult = result;
            }

            return result;

        }

        TimeSpan _antiJump = TimeSpan.FromSeconds(1);

        private void HandleUnchanged()
        {
            //Anti Jump

            int elapsedMs = (int)(_now - _lastUpdated).TotalMilliseconds;
            if (_initialTargetAngle.HasValue && _initialFollowAngle.HasValue && elapsedMs > _antiJump.TotalMilliseconds)
            {
                logger.LogWarning($"AntiJump Reset: Elapsed {elapsedMs} ms > {_antiJump.TotalMilliseconds} ms.");
                Reset();
            }
            else
            {
                logger.LogWarning($"AntiJump: Elapsed : {elapsedMs} ms.");
            }
        }


    }
}
