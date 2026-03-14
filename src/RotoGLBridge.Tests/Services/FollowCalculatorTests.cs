// PSEUDOCODE / PLAN:
// - Create xUnit test class `FollowCalculatorTests` in namespace `RotoGLBridge.Tests.Services`.
// - For each test, instantiate MathService and FollowCalculator.
// - Subscribe to `OnAngleChangedObservable` and collect non-null emissions in a list.
// - Test cases:
//   1) `DoesNotEmitWhenOffsetDifferenceIsZero`
//      - Call Update once with (5,45). Offset difference = 0 -> no emission.
//      - Assert collected events count == 0 and returned result values are as expected.
//   2) `EmitsWhenOffsetDifferenceGreaterThanOne`
//      - Call Update(5,45) then Update(10,46).
//      - Expect one emission with NewFollowAngle = 50 and calculator.NewFollowAngle == 50.
//   3) `DoesNotEmitDuplicateValueResults`
//      - Call Update(5,45), Update(10,46) -> one emission.
//      - Call Update(10,46) again -> same result values, expect no additional emission (record equality prevents duplicate).
//   4) `NormalizesNewFollowAngleWhenExceeding360`
//      - Call Update(0,350) then Update(20,350).
//      - Expect emission with NewFollowAngle normalized to 10 (350 + 20 -> 370 -> 10).
//
// - Implement assertions for returned FollowResult properties (InitialTargetAngle, InitialFollowAngle, TargetOffset, FollowOffset, OffsetDifference, NewFollowAngle).
//
// Notes:
// - BehaviorSubject initially holds null; filter out nulls when collecting events.
// - Use floating point comparisons with exact values from MathService behaviour (inputs selected to produce integer results).

using Xunit;
using RotoGLBridge.Services;
using System;
using System.Collections.Generic;

namespace RotoGLBridge.Tests.Services
{
    public class FollowCalculatorTests
    {
        private MathService CreateMathService() => new MathService();

        private readonly (float target, float follow)[] sequence;

        public FollowCalculatorTests()
        {
            // glyaw = targetAngle, roto.Yaw = followAngle
            sequence =
            [
                (89f, 88.33008f),
                (89f, 88.50586f),
                (89f, 88.68164f),
                (89f, 88.76953f),
                (89f, 88.59375f),
                (89f, 88.33008f),
                (89f, 88.1543f),
                (89f, 87.9785156f),
                // Entering SetObjectFollowDegree with degree: 89, speed: 50
                (89f, 88.06641f),
                // Entering SetObjectFollowDegree with degree: 88, speed: 50
                (89f, 88.24219f),
                (89f, 88.68164f),
                (89f, 88.59375f),
                (89f, 88.41797f),
                (89f, 87.9785156f),
                // Entering SetObjectFollowDegree with degree: 89, speed: 50
                (89f, 88.24219f),
                // Entering SetObjectFollowDegree with degree: 88, speed: 50
                (89f, 88.41797f),
                (89f, 88.59375f),
                (89f, 88.50586f),
                (89f, 88.33008f),
                (89f, 88.1543f),
                (89f, 87.9785156f),
                // Entering SetObjectFollowDegree with degree: 89, speed: 50
                (89f, 88.1543f),
                // Entering SetObjectFollowDegree with degree: 88, speed: 50
                (89f, 88.33008f),
                (89f, 88.50586f),
            ];
        }

        [Fact]
        public void DoesNotEmitWhenOffsetDifferenceIsZero()
        {
            var ms = CreateMathService();
            var calc = new FollowCalculator(ms);

            var events = new List<FollowResult>();
            using var sub = calc.OnAngleChangedObservable.Subscribe(r => { if (r != null) events.Add(r); });

            // First update initializes initial angles and produces no emission (offset diff == 0)
            var result = calc.Update(5f, 45f);

            Assert.Equal(5f, result.InitialTargetAngle);
            Assert.Equal(45f, result.InitialFollowAngle);
            Assert.Equal(0f, result.TargetOffset);
            Assert.Equal(0f, result.FollowOffset);
            Assert.Equal(0f, result.OffsetDifference);
            Assert.Equal(5f, result.NewFollowAngle);

            Assert.Empty(events);
        }

        [Fact]
        public void EmitsWhenOffsetDifferenceGreaterThanOne()
        {
            var ms = CreateMathService();
            var calc = new FollowCalculator(ms);
            calc.Update(0f, 0f);
            var events = new List<FollowResult>();
            using var sub = calc.OnAngleChangedObservable.Subscribe(r => { if (r != null) events.Add(r); });

            // initialize
            var r1 = calc.Update(5f, 45f);

            // this update produces TargetOffset=5, FollowOffset=1 => OffsetDifference=4 (>1)
            var r2 = calc.Update(10f, 46f);

            // one event should have been emitted
            Assert.Single(events);
            var emitted = events[0];

            Assert.Equal(5f, emitted.InitialTargetAngle);
            Assert.Equal(45f, emitted.InitialFollowAngle);
            Assert.Equal(5f, emitted.TargetOffset);
            Assert.Equal(1f, emitted.FollowOffset);
            Assert.Equal(4f, emitted.OffsetDifference);
            Assert.Equal(50f, emitted.NewFollowAngle);

            // property NewFollowAngle should reflect last emitted value
            Assert.Equal(50f, calc.NewFollowAngle);
        }

        [Fact]
        public void DoesNotEmitDuplicateValueResults()
        {
            var ms = CreateMathService();
            var calc = new FollowCalculator(ms);
            calc.Update(0, 0);
            var events = new List<FollowResult>();
            using var sub = calc.OnAngleChangedObservable.Subscribe(r => { if (r != null) events.Add(r); });

            // initialize and produce first emission
            calc.Update(5f, 45f);
            calc.Update(10f, 46f);

            Assert.Single(events);

            // Repeat the same update values -> resulting FollowResult value-equals the previous one,
            // so the record equality check should prevent a duplicate emission.
            calc.Update(10f, 46f);

            Assert.Single(events); // still one
        }

        [Fact]
        public void NormalizesNewFollowAngleWhenExceeding360()
        {
            var ms = CreateMathService();
            var calc = new FollowCalculator(ms);
            calc.Update(0f, 0f);
            var events = new List<FollowResult>();
            using var sub = calc.OnAngleChangedObservable.Subscribe(r => { 
                if (r != null) 
                    events.Add(r); 
            });

            // initialize with followAngle 350
            calc.Update(0f, 350f);

            // target moved to 20 -> target offset 20, follow offset 0 => offset difference 20
            // new follow angle = 350 + 20 = 370 -> normalized to 10
            var r2 = calc.Update(20f, 350f);

            Assert.Single(events);
            var emitted = events[0];

            Assert.Equal(0f, emitted.InitialTargetAngle);
            Assert.Equal(350f, emitted.InitialFollowAngle);
            Assert.Equal(20f, emitted.TargetOffset);
            Assert.Equal(0f, emitted.FollowOffset);
            Assert.Equal(20f, emitted.OffsetDifference);
            Assert.Equal(10f, emitted.NewFollowAngle);

            Assert.Equal(10f, calc.NewFollowAngle);
        }

        private bool ApproxEqual(float a, float b, float tolerance = 0.0001f)
        {
            return MathF.Abs(a - b) <= tolerance;
        }

        [Fact]
        public void Sequence_WithSmallJitterAroundTarget_DoesNotEmit()
        {
            

            var ms = CreateMathService();
            var calc = new FollowCalculator(ms);
            var events = new List<FollowResult>();
            using var sub = calc.OnAngleChangedObservable.Subscribe(r => { if (r != null) events.Add(r); });

            FollowResult? last = null;
            foreach (var (t, f) in sequence)
            {
                var update = calc.Update(t, f);

                if(update == null && last?.CurrentTargetAngle != null)
                {
                    Assert.True(ApproxEqual(last.CurrentTargetAngle.Value, t, 1), $"CurrentTargetAngle is not approximately equal: {last.CurrentTargetAngle.Value} != {t}");
                }
                
                
                last = update;
                

                if (last?.CurrentTargetAngle != null && ms.CalculateOffsetAngle(t, last.CurrentTargetAngle.Value) < 1f)
                    Assert.True(last.NewFollowAngle == t);

                if(last != null)
                    // the offset difference should remain within the threshold
                    Assert.True(Math.Abs(last.OffsetDifference) <= 1f, $"OffsetDifference exceeded threshold: {last?.OffsetDifference}");
            }

            // No emissions expected because threshold is strictly > 1
            Assert.Empty(events);

            if (last != null)
            {
                // NewFollowAngle from the last calculation should equal the last provided follow angle
                //Assert.NotNull(last);
                Assert.Equal(sequence[^1].follow, last.NewFollowAngle);
            }
        }

        [Fact]
        public void Sequence_WithSmallJitterAroundTarget_DoesNotChange()
        {


            var ms = CreateMathService();
            var calc = new FollowCalculator(ms);
            var events = new List<FollowResult>();

            var firstREsult = calc.Update(44, 90);
            

            FollowResult last = null;
            float? lastT = null;
            foreach (var (t, f) in sequence)
            {

                last = calc.Update(t, f);

                

                if (last != null)
                {
                    lastT = t;
                    var c = t - firstREsult.InitialTargetAngle + firstREsult.InitialFollowAngle;
                    // the offset difference should remain within the threshold
                    //Assert.True(last.NewFollowAngle == c, $"NewFollowAngle ({last.NewFollowAngle}) is wrong: expected {c}. t={t} f={f}");
                    //Assert.Equal(89, last.NewFollowAngle );
                }
                else
                {
                    Assert.True(ApproxEqual(t,f, 1) || lastT == t , $"last is null but t and f differ more than threshold: {t} != {f}");
                }
                
                if(lastT == null)
                {
                    Assert.True(ApproxEqual(t - firstREsult.InitialTargetAngle.Value + firstREsult.InitialFollowAngle.Value, firstREsult.NewFollowAngle));
                }
            }

           
        }
    }
}