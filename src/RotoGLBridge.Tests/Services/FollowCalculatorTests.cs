// PSEUDOCODE / PLAN:
// - Create xUnit test class `FollowCalculatorTests` in namespace `RotoGLBridge.Tests.Services`.
// - For each test, instantiate MathService and FollowCalculator.
// - Subscribe to `NewFollowAngleChanged` and collect non-null emissions in a list.
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

        [Fact]
        public void DoesNotEmitWhenOffsetDifferenceIsZero()
        {
            var ms = CreateMathService();
            var calc = new FollowCalculator(ms);

            var events = new List<FollowResult>();
            using var sub = calc.NewFollowAngleChanged.Subscribe(r => { if (r != null) events.Add(r); });

            // First update initializes initial angles and produces no emission (offset diff == 0)
            var result = calc.Update(5f, 45f);

            Assert.Equal(5f, result.InitialTargetAngle);
            Assert.Equal(45f, result.InitialFollowAngle);
            Assert.Equal(0f, result.TargetOffset);
            Assert.Equal(0f, result.FollowOffset);
            Assert.Equal(0f, result.OffsetDifference);
            Assert.Equal(45f, result.NewFollowAngle);

            Assert.Empty(events);
        }

        [Fact]
        public void EmitsWhenOffsetDifferenceGreaterThanOne()
        {
            var ms = CreateMathService();
            var calc = new FollowCalculator(ms);

            var events = new List<FollowResult>();
            using var sub = calc.NewFollowAngleChanged.Subscribe(r => { if (r != null) events.Add(r); });

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

            var events = new List<FollowResult>();
            using var sub = calc.NewFollowAngleChanged.Subscribe(r => { if (r != null) events.Add(r); });

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

            var events = new List<FollowResult>();
            using var sub = calc.NewFollowAngleChanged.Subscribe(r => { if (r != null) events.Add(r); });

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
    }
}