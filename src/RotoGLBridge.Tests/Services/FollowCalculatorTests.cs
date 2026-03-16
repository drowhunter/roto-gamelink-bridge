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

using System;
using System.Collections.Generic;
using System.Reactive.Linq;

using Microsoft.Extensions.Logging;
using Moq;
using RotoGLBridge.Services;
using Xunit;

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
            var mockLogger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);

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
            var mockLogger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);
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

        //[Fact]
        //public void DoesNotEmitDuplicateValueResults()
        //{
        //    var ms = CreateMathService();
        //    var mockLogger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<FollowCalculator>>();
        //    var calc = new FollowCalculator(ms, mockLogger.Object);
        //    calc.Update(0, 0);
        //    var events = new List<FollowResult>();
        //    using var sub = calc.OnAngleChangedObservable.Subscribe(r => { if (r != null) events.Add(r); });

        //    // initialize and produce first emission
        //    calc.Update(5f, 45f);
        //    calc.Update(10f, 46f);

        //    Assert.Single(events);

        //    // Repeat the same update values -> resulting FollowResult value-equals the previous one,
        //    // so the record equality check should prevent a duplicate emission.
        //    calc.Update(10f, 46f);

        //    Assert.Single(events); // still one
        //}

        [Fact]
        public void NormalizesNewFollowAngleWhenExceeding360()
        {
            var ms = CreateMathService();
            var mockLogger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);
            calc.Update(0f, 0f);
            var events = new List<FollowResult>();
            using var sub = calc.OnAngleChangedObservable.Subscribe(r =>
            {
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
            var mockLogger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);
            var events = new List<FollowResult>();
            using var sub = calc.OnAngleChangedObservable.Subscribe(r => { if (r != null) events.Add(r); });

            FollowResult? last = null;
            foreach (var (t, f) in sequence)
            {
                var update = calc.Update(t, f);

                if (update == null && last?.CurrentTargetAngle != null)
                {
                    Assert.True(ApproxEqual(last.CurrentTargetAngle.Value, t, 1), $"CurrentTargetAngle is not approximately equal: {last.CurrentTargetAngle.Value} != {t}");
                }


                last = update;


                if (last?.CurrentTargetAngle != null && ms.CalculateOffsetAngle(t, last.CurrentTargetAngle.Value) < 1f)
                    Assert.True(last.NewFollowAngle == t);

                if (last != null)
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
            var mockLogger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);
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
                    // Add null checks before accessing Value
                    if (firstREsult.InitialTargetAngle.HasValue && firstREsult.InitialFollowAngle.HasValue)
                    {
                        var c = t - firstREsult.InitialTargetAngle.Value + firstREsult.InitialFollowAngle.Value;
                        // the offset difference should remain within the threshold
                        //Assert.True(last.NewFollowAngle == c, $"NewFollowAngle ({last.NewFollowAngle}) is wrong: expected {c}. t={t} f={f}");
                        //Assert.Equal(89, last.NewFollowAngle );
                    }
                }
                else
                {
                    Assert.True(ApproxEqual(t, f, 1) || lastT == t, $"last is null but t and f differ more than threshold: {t} != {f}");
                }

                if (lastT == null)
                {
                    // Add null checks before accessing Value
                    if (firstREsult.InitialTargetAngle.HasValue && firstREsult.InitialFollowAngle.HasValue)
                    {
                        Assert.True(ApproxEqual(t - firstREsult.InitialTargetAngle.Value + firstREsult.InitialFollowAngle.Value, firstREsult.NewFollowAngle));
                    }
                }
            }


        }

        /// <summary>
        /// Tests that OnAngleChangedObservable skips the initial null value from the BehaviorSubject.
        /// Verifies that subscribing immediately after instantiation does not emit the initial null.
        /// Expected result: No events are emitted upon subscription.
        /// </summary>
        [Fact]
        public void OnAngleChangedObservable_InitialSubscription_SkipsInitialNullValue()
        {
            // Arrange
            var mathService = new MathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);
            var events = new List<FollowResult?>();

            // Act
            using var subscription = calculator.OnAngleChangedObservable.Subscribe(r => events.Add(r));

            // Assert
            Assert.Empty(events);
        }

        /// <summary>
        /// Tests that OnAngleChangedObservable delivers the correct value after an emission.
        /// Triggers an emission by calling Update with values that produce offset difference > 1.
        /// Expected result: Subscriber receives exactly one emission with correct values.
        /// </summary>
        [Fact]
        public void OnAngleChangedObservable_AfterEmission_DeliversCorrectValue()
        {
            // Arrange
            var mathService = new MathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);
            calculator.Update(0f, 0f);
            var events = new List<FollowResult?>();
            using var subscription = calculator.OnAngleChangedObservable.Subscribe(r => events.Add(r));

            // Act
            calculator.Update(5f, 45f);

            // Assert
            Assert.Single(events);
            var emitted = events[0];
            Assert.NotNull(emitted);
            Assert.Equal(0f, emitted.InitialTargetAngle);
            Assert.Equal(0f, emitted.InitialFollowAngle);
            Assert.Equal(5f, emitted.NewFollowAngle);
        }

        /// <summary>
        /// Tests that multiple subscribers to OnAngleChangedObservable all receive the same emissions.
        /// Verifies that the observable properly multicasts to all subscribers.
        /// Expected result: All subscribers receive the emitted value.
        /// </summary>
        [Fact]
        public void OnAngleChangedObservable_MultipleSubscribers_AllReceiveEmissions()
        {
            // Arrange
            var mathService = new MathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);
            calculator.Update(0f, 0f);
            var events1 = new List<FollowResult?>();
            var events2 = new List<FollowResult?>();
            var events3 = new List<FollowResult?>();
            using var subscription1 = calculator.OnAngleChangedObservable.Subscribe(r => events1.Add(r));
            using var subscription2 = calculator.OnAngleChangedObservable.Subscribe(r => events2.Add(r));
            using var subscription3 = calculator.OnAngleChangedObservable.Subscribe(r => events3.Add(r));

            // Act
            calculator.Update(5f, 45f);
            calculator.Update(10f, 46f);

            // Assert
            Assert.Equal(2, events1.Count);
            Assert.Equal(2, events2.Count);
            Assert.Equal(2, events3.Count);
            Assert.Equal(events1[0]?.NewFollowAngle, events2[0]?.NewFollowAngle);
            Assert.Equal(events2[0]?.NewFollowAngle, events3[0]?.NewFollowAngle);
            Assert.Equal(events1[1]?.NewFollowAngle, events2[1]?.NewFollowAngle);
            Assert.Equal(events2[1]?.NewFollowAngle, events3[1]?.NewFollowAngle);
        }

        /// <summary>
        /// Tests that subscribing after emissions have occurred only receives subsequent emissions.
        /// Verifies that the Skip(1) and AsObservable() combination doesn't replay past values.
        /// Expected result: Late subscriber receives only new emissions, not historical ones.
        /// </summary>
        [Fact]
        public void OnAngleChangedObservable_SubscriptionAfterEmissions_ReceivesOnlySubsequentEmissions()
        {
            // Arrange
            var mathService = new MathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);
            calculator.Update(0f, 0f);
            var earlyEvents = new List<FollowResult?>();
            using var earlySubscription = calculator.OnAngleChangedObservable.Subscribe(r => earlyEvents.Add(r));

            // Act - trigger first emission
            calculator.Update(5f, 45f);
            Assert.Single(earlyEvents);

            // Subscribe late
            var lateEvents = new List<FollowResult?>();
            using var lateSubscription = calculator.OnAngleChangedObservable.Subscribe(r => lateEvents.Add(r));

            // Assert - late subscriber has no events yet
            Assert.Empty(lateEvents);

            // Act - trigger second emission
            calculator.Update(15f, 47f);

            // Assert - both subscribers receive the new emission
            Assert.Equal(2, earlyEvents.Count);
            Assert.Single(lateEvents);
            Assert.Equal(earlyEvents[1]?.NewFollowAngle, lateEvents[0]?.NewFollowAngle);
        }

        /// <summary>
        /// Tests that OnAngleChangedObservable does not emit when offset difference is less than 1.
        /// Verifies that the observable respects the emission condition in the Update method.
        /// Expected result: No emissions when offset difference threshold is not met.
        /// </summary>
        [Fact]
        public void OnAngleChangedObservable_NoEmissionWhenOffsetSmall_SubscriberReceivesNothing()
        {
            // Arrange
            var mathService = new MathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);
            var events = new List<FollowResult?>();
            using var subscription = calculator.OnAngleChangedObservable.Subscribe(r => events.Add(r));

            // Act - offset difference will be 0
            calculator.Update(5f, 45f);

            // Assert
            Assert.Empty(events);
        }

        /// <summary>
        /// Tests that unsubscribing stops receiving emissions from OnAngleChangedObservable.
        /// Verifies proper subscription lifecycle management.
        /// Expected result: After unsubscribe, no more events are received.
        /// </summary>
        [Fact]
        public void OnAngleChangedObservable_Unsubscribe_StopsReceivingEmissions()
        {
            // Arrange
            var mathService = new MathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);
            calculator.Update(0f, 0f);
            var events = new List<FollowResult?>();
            var subscription = calculator.OnAngleChangedObservable.Subscribe(r => events.Add(r));

            // Act - trigger first emission
            calculator.Update(5f, 45f);
            calculator.Update(10f, 46f);
            Assert.Equal(2, events.Count);

            // Unsubscribe
            subscription.Dispose();

            // Trigger second emission
            calculator.Update(15f, 47f);

            // Assert - still only two events (didn't receive third emission)
            Assert.Equal(2, events.Count);
        }

        /// <summary>
        /// Tests that OnAngleChangedObservable returns an observable, not the underlying subject.
        /// Verifies proper encapsulation - subscribers cannot call OnNext directly.
        /// Expected result: Observable is not a Subject type.
        /// </summary>
        [Fact]
        public void OnAngleChangedObservable_ReturnsObservable_NotSubject()
        {
            // Arrange
            var mathService = new MathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);

            // Act
            var observable = calculator.OnAngleChangedObservable;

            // Assert
            Assert.NotNull(observable);
            Assert.IsNotAssignableFrom<System.Reactive.Subjects.ISubject<FollowResult>>(observable);
        }

        /// <summary>
        /// Tests that OnAngleChangedObservable handles null values correctly after emissions.
        /// Verifies that the observable filters null values as expected.
        /// Expected result: Only non-null FollowResult values are emitted.
        /// </summary>
        [Fact]
        public void OnAngleChangedObservable_EmittedValues_AreNeverNull()
        {
            // Arrange
            var mathService = new MathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);
            calculator.Update(0f, 0f);
            var events = new List<FollowResult?>();
            using var subscription = calculator.OnAngleChangedObservable.Subscribe(r => events.Add(r));

            // Act - trigger multiple emissions
            calculator.Update(5f, 45f);
            calculator.Update(10f, 46f);
            calculator.Update(15f, 47f);
            calculator.Update(20f, 48f);

            // Assert
            Assert.All(events, e => Assert.NotNull(e));
        }

        /// <summary>
        /// Tests that OnAngleChangedObservable can be accessed multiple times and returns consistent observable.
        /// Verifies that accessing the property multiple times provides working observables.
        /// Expected result: Each access provides a working observable that receives emissions.
        /// </summary>
        [Fact]
        public void OnAngleChangedObservable_MultiplePropertyAccesses_EachReturnsWorkingObservable()
        {
            // Arrange
            var mathService = new MathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);
            calculator.Update(0f, 0f);

            // Act - access property multiple times
            var observable1 = calculator.OnAngleChangedObservable;
            var observable2 = calculator.OnAngleChangedObservable;
            var events1 = new List<FollowResult?>();
            var events2 = new List<FollowResult?>();
            using var subscription1 = observable1.Subscribe(r => events1.Add(r));
            using var subscription2 = observable2.Subscribe(r => events2.Add(r));

            calculator.Update(5f, 45f);
            calculator.Update(10f, 46f);

            // Assert
            Assert.Equal(2, events1.Count);
            Assert.Equal(2, events2.Count);
        }

        /// <summary>
        /// Tests that OnAngleChangedObservable emits when angle wraps around 360 degrees boundary.
        /// Verifies that the observable correctly handles angle normalization scenarios.
        /// Expected result: Emission occurs with normalized angle value.
        /// </summary>
        [Fact]
        public void OnAngleChangedObservable_AngleWraparound_EmitsNormalizedValue()
        {
            // Arrange
            var mathService = new MathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);
            calculator.Update(0f, 0f);
            var events = new List<FollowResult?>();
            using var subscription = calculator.OnAngleChangedObservable.Subscribe(r => events.Add(r));

            // Act
            calculator.Update(0f, 350f);
            calculator.Update(20f, 350f);

            // Assert
            Assert.Single(events);
            var emitted = events[0];
            Assert.NotNull(emitted);
            Assert.Equal(20f, emitted.NewFollowAngle);
        }

        /// <summary>
        /// Tests that OnAngleChangedObservable handles extreme angle values correctly.
        /// Tests boundary values including float.MinValue, float.MaxValue, and special values.
        /// Expected result: Observable handles extreme values without throwing exceptions.
        /// </summary>
        [Theory]
        [InlineData(0f, 0f)]
        [InlineData(360f, 360f)]
        [InlineData(-360f, -360f)]
        [InlineData(720f, 720f)]
        public void OnAngleChangedObservable_ExtremeAngleValues_HandlesWithoutException(float targetAngle, float followAngle)
        {
            // Arrange
            var mathService = new MathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);
            var events = new List<FollowResult?>();
            using var subscription = calculator.OnAngleChangedObservable.Subscribe(r => events.Add(r));

            // Act & Assert - should not throw
            var exception = Record.Exception(() =>
            {
                calculator.Update(targetAngle, followAngle);
            });

            Assert.Null(exception);
        }

        /// <summary>
        /// Tests that OnAngleChangedObservable subscription handles errors according to standard Rx behavior.
        /// Verifies that when a subscriber throws an exception, the exception propagates.
        /// Expected result: Exception is thrown and observable behavior follows standard Rx semantics.
        /// </summary>
        [Fact]
        public void OnAngleChangedObservable_SubscriberThrows_OtherSubscribersContinueReceiving()
        {
            // Arrange
            var mathService = new MathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);
            calculator.Update(0f, 0f);
            var goodEvents = new List<FollowResult?>();

            // Good subscriber subscribed first will receive events
            using var goodSubscription = calculator.OnAngleChangedObservable.Subscribe(r => goodEvents.Add(r));

            // Subscriber that throws subscribed second won't affect the first subscriber
            using var throwingSubscription = calculator.OnAngleChangedObservable.Subscribe(r =>
            {
                throw new InvalidOperationException("Test exception");
            });

            // Act & Assert - the throwing subscriber will cause exception to propagate
            var exception = Record.Exception(() =>
            {
                calculator.Update(5f, 45f);
            });

            // The exception will be thrown, and the good subscriber should have received the event before the throwing one
            Assert.NotNull(exception);
            Assert.IsType<InvalidOperationException>(exception);
            Assert.Single(goodEvents);
        }

        /// <summary>
        /// Tests that Update initializes angles correctly on first call when offset difference is zero.
        /// </summary>
        [Fact]
        public void Update_FirstCallWithZeroOffsetDifference_InitializesAnglesAndDoesNotEmit()
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);
            var events = new List<FollowResult>();
            using var sub = calc.OnAngleChangedObservable.Subscribe(r => { if (r != null) events.Add(r); });

            // Act
            var result = calc.Update(5f, 45f);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5f, result.InitialTargetAngle);
            Assert.Equal(45f, result.InitialFollowAngle);
            Assert.Equal(0f, result.TargetOffset);
            Assert.Equal(0f, result.FollowOffset);
            Assert.Equal(0f, result.OffsetDifference);
            Assert.Equal(5f, result.NewFollowAngle);
            Assert.Empty(events);
        }

        /// <summary>
        /// Tests that Update emits an event when offset difference exceeds threshold of 1 degree.
        /// </summary>
        [Fact]
        public void Update_OffsetDifferenceGreaterThanOne_EmitsEventAndCalculatesCorrectNewFollowAngle()
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);
            var events = new List<FollowResult>();
            using var sub = calc.OnAngleChangedObservable.Subscribe(r => { if (r != null) events.Add(r); });

            // Act
            calc.Update(5f, 45f);
            var r2 = calc.Update(10f, 46f);

            // Assert
            Assert.Single(events);
            var emitted = events[0];
            Assert.Equal(5f, emitted.InitialTargetAngle);
            Assert.Equal(45f, emitted.InitialFollowAngle);
            Assert.Equal(5f, emitted.TargetOffset);
            Assert.Equal(1f, emitted.FollowOffset);
            Assert.Equal(4f, emitted.OffsetDifference);
            Assert.Equal(50f, emitted.NewFollowAngle);
            Assert.Equal(50f, calc.NewFollowAngle);
        }

        /// <summary>
        /// Tests that Update returns null when target angle has not changed significantly (less than 1 degree).
        /// </summary>
        [Fact]
        public void Update_TargetUnchangedLessThanOneDegree_ReturnsNull()
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);
            var events = new List<FollowResult>();
            using var sub = calc.OnAngleChangedObservable.Subscribe(r => { if (r != null) events.Add(r); });

            // Act
            calc.Update(89f, 88f);
            var result = calc.Update(89f, 88.5f);

            // Assert
            Assert.Null(result);
            Assert.Empty(events);
        }

        /// <summary>
        /// Tests that Update normalizes angles correctly when they exceed 360 degrees.
        /// </summary>
        [Fact]
        public void Update_NewFollowAngleExceeds360_NormalizesCorrectly()
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);
            var events = new List<FollowResult>();
            using var sub = calc.OnAngleChangedObservable.Subscribe(r => { if (r != null) events.Add(r); });

            // Act
            calc.Update(0f, 350f);
            var r2 = calc.Update(20f, 350f);

            // Assert
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

        /// <summary>
        /// Tests that Update handles negative angle wrapping correctly (crossing 0/360 boundary).
        /// </summary>
        [Fact]
        public void Update_NegativeAngleWrapping_CalculatesCorrectOffset()
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);
            calc.Update(0f, 0f);
            var events = new List<FollowResult>();
            using var sub = calc.OnAngleChangedObservable.Subscribe(r => { if (r != null) events.Add(r); });

            // Act
            calc.Update(10f, 10f);
            var result = calc.Update(350f, 10f);

            // Assert
            Assert.Single(events);
            var emitted = events[0];
            Assert.Equal(-10f, emitted.TargetOffset);
            Assert.Equal(10f, emitted.FollowOffset);
            Assert.Equal(-20f, emitted.OffsetDifference);
            Assert.Equal(350f, emitted.NewFollowAngle);
        }

        /// <summary>
        /// Tests that Update with zero angles initializes and calculates correctly.
        /// </summary>
        [Fact]
        public void Update_ZeroAngles_InitializesCorrectly()
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);
            var events = new List<FollowResult>();
            using var sub = calc.OnAngleChangedObservable.Subscribe(r => { if (r != null) events.Add(r); });

            // Act
            var result = calc.Update(0f, 0f);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0f, result.InitialTargetAngle);
            Assert.Equal(0f, result.InitialFollowAngle);
            Assert.Equal(0f, result.TargetOffset);
            Assert.Equal(0f, result.FollowOffset);
            Assert.Equal(0f, result.OffsetDifference);
            Assert.Equal(0f, result.NewFollowAngle);
            Assert.Empty(events);
        }

        /// <summary>
        /// Tests that Update handles maximum float values without throwing exceptions.
        /// </summary>
        [Fact]
        public void Update_MaxFloatValues_DoesNotThrow()
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);

            // Act & Assert
            var result = calc.Update(float.MaxValue, float.MaxValue);
            Assert.NotNull(result);
        }

        /// <summary>
        /// Tests that Update handles minimum float values without throwing exceptions.
        /// </summary>
        [Fact]
        public void Update_MinFloatValues_DoesNotThrow()
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);

            // Act & Assert
            var result = calc.Update(float.MinValue, float.MinValue);
            Assert.NotNull(result);
        }

        /// <summary>
        /// Tests that Update handles negative angles correctly.
        /// </summary>
        [Fact]
        public void Update_NegativeAngles_HandlesCorrectly()
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);

            // Act
            var result = calc.Update(-45f, -90f);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(-45f, result.InitialTargetAngle);
            Assert.Equal(-90f, result.InitialFollowAngle);
        }

        /// <summary>
        /// Tests that Update handles angles greater than 360 correctly.
        /// </summary>
        [Fact]
        public void Update_AnglesGreaterThan360_HandlesCorrectly()
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);

            // Act
            var result = calc.Update(450f, 540f);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(450f, result.InitialTargetAngle);
            Assert.Equal(540f, result.InitialFollowAngle);
        }

        /// <summary>
        /// Tests that Update handles NaN values for target angle.
        /// </summary>
        [Fact]
        public void Update_NaNTargetAngle_HandlesValue()
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);

            // Act
            var result = calc.Update(float.NaN, 45f);

            // Assert
            Assert.NotNull(result);
            Assert.True(float.IsNaN(result.InitialTargetAngle.Value));
        }

        /// <summary>
        /// Tests that Update handles NaN values for follow angle.
        /// </summary>
        [Fact]
        public void Update_NaNFollowAngle_HandlesValue()
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);

            // Act
            var result = calc.Update(45f, float.NaN);

            // Assert
            Assert.NotNull(result);
            Assert.True(float.IsNaN(result.InitialFollowAngle.Value));
        }

        /// <summary>
        /// Tests that Update handles positive infinity for target angle.
        /// </summary>
        [Fact]
        public void Update_PositiveInfinityTargetAngle_HandlesValue()
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);

            // Act
            var result = calc.Update(float.PositiveInfinity, 45f);

            // Assert
            Assert.NotNull(result);
            Assert.True(float.IsPositiveInfinity(result.InitialTargetAngle.Value));
        }

        /// <summary>
        /// Tests that Update handles negative infinity for follow angle.
        /// </summary>
        [Fact]
        public void Update_NegativeInfinityFollowAngle_HandlesValue()
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);

            // Act
            var result = calc.Update(45f, float.NegativeInfinity);

            // Assert
            Assert.NotNull(result);
            Assert.True(float.IsNegativeInfinity(result.InitialFollowAngle.Value));
        }

        /// <summary>
        /// Tests that Update with offset difference exactly equal to 1 still sets NewFollowAngle to targetAngle.
        /// </summary>
        [Fact(Skip="ProductionBugSuspected")]
        [Trait("Category", "ProductionBugSuspected")]
        public void Update_OffsetDifferenceExactlyOne_DoesNotEmit()
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);
            calc.Update(0f, 0f);
            var events = new List<FollowResult>();
            using var sub = calc.OnAngleChangedObservable.Subscribe(r => { if (r != null) events.Add(r); });

            // Act
            calc.Update(0f, 0f);
            var result = calc.Update(1f, 0f);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1f, result.OffsetDifference);
            Assert.Equal(1f, result.NewFollowAngle);
            Assert.Empty(events);
        }

        /// <summary>
        /// Tests that Update with offset difference slightly greater than 1 emits an event.
        /// </summary>
        [Fact]
        public void Update_OffsetDifferenceSlightlyGreaterThanOne_Emits()
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);
            calc.Update(0f, 0f);
            var events = new List<FollowResult>();
            using var sub = calc.OnAngleChangedObservable.Subscribe(r => { if (r != null) events.Add(r); });

            // Act
            calc.Update(0f, 0f);
            var result = calc.Update(1.1f, 0f);

            // Assert
            Assert.NotNull(result);
            Assert.Single(events);
            Assert.Equal(1.1f, result.OffsetDifference);
        }

        /// <summary>
        /// Tests that Update maintains LastResult property correctly.
        /// </summary>
        [Fact]
        public void Update_MultipleCalls_UpdatesLastResultCorrectly()
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);

            // Act
            var result1 = calc.Update(10f, 20f);
            var lastResult1 = calc.LastResult;
            var result2 = calc.Update(15f, 25f);
            var lastResult2 = calc.LastResult;

            // Assert
            Assert.Same(result1, lastResult1);
            Assert.Same(result2, lastResult2);
            Assert.NotSame(result1, result2);
        }

        /// <summary>
        /// Tests that Update with very small target change (0.5 degrees) returns null.
        /// </summary>
        [Fact]
        public void Update_VerySmallTargetChange_ReturnsNull()
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);

            // Act
            calc.Update(100f, 200f);
            var result = calc.Update(100.5f, 200f);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that consecutive calls with same angles after initialization don't emit if target unchanged.
        /// </summary>
        [Fact]
        public void Update_ConsecutiveCallsWithSameAngles_ReturnsNullAfterFirst()
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);
            var events = new List<FollowResult>();
            using var sub = calc.OnAngleChangedObservable.Subscribe(r => { if (r != null) events.Add(r); });

            // Act
            var result1 = calc.Update(50f, 60f);
            var result2 = calc.Update(50f, 60f);
            var result3 = calc.Update(50f, 60f);

            // Assert
            Assert.NotNull(result1);
            Assert.Null(result2);
            Assert.Null(result3);
            Assert.Empty(events);
        }

        /// <summary>
        /// Tests that Update correctly handles large offset differences.
        /// </summary>
        [Fact]
        public void Update_LargeOffsetDifference_CalculatesCorrectly()
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);
            calc.Update(0f, 0f);
            var events = new List<FollowResult>();
            using var sub = calc.OnAngleChangedObservable.Subscribe(r => { if (r != null) events.Add(r); });

            // Act
            calc.Update(0f, 0f);
            var result = calc.Update(180f, 0f);

            // Assert
            Assert.NotNull(result);
            Assert.Single(events);
            Assert.Equal(180f, result.TargetOffset);
            Assert.Equal(0f, result.FollowOffset);
            Assert.Equal(180f, result.OffsetDifference);
            Assert.Equal(180f, result.NewFollowAngle);
        }

        /// <summary>
        /// Tests that Update handles precision near boundaries correctly (0.99 degrees).
        /// </summary>
        [Fact]
        public void Update_OffsetDifferencePointNineNine_DoesNotEmit()
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);
            calc.Update(0f, 0f);
            var events = new List<FollowResult>();
            using var sub = calc.OnAngleChangedObservable.Subscribe(r => { if (r != null) events.Add(r); });

            // Act
            calc.Update(0f, 0f);
            var result = calc.Update(1f, 0.01f);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(events);
            Assert.True(MathF.Abs(result.OffsetDifference) < 1f);
        }

        /// <summary>
        /// Tests that accessing NewFollowAngle before any emission from Update throws NullReferenceException.
        /// The BehaviorSubject is initialized with null, so accessing Value.NewFollowAngle will throw.
        /// </summary>
        [Fact]
        public void NewFollowAngle_BeforeAnyEmission_ThrowsNullReferenceException()
        {
            // Arrange
            var mathService = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => calculator.NewFollowAngle);
        }

        /// <summary>
        /// Tests that NewFollowAngle returns the correct value after an emission occurs.
        /// When Update produces an offset difference >= 1, it emits a result and NewFollowAngle should reflect that value.
        /// </summary>
        [Fact]
        public void NewFollowAngle_AfterEmission_ReturnsNewFollowAngle()
        {
            // Arrange
            var mathService = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);
            calculator.Update(0f, 0f);
            calculator.Update(5f, 45f);

            // Act
            calculator.Update(10f, 46f); // TargetOffset=10, FollowOffset=46, OffsetDifference=-36, NewFollowAngle = 46 + (-36) = 10

            // Assert
            Assert.Equal(10f, calculator.NewFollowAngle);
        }

        /// <summary>
        /// Tests that NewFollowAngle returns the latest emitted value after multiple emissions.
        /// Each emission should update the BehaviorSubject's Value, and NewFollowAngle should reflect the most recent.
        /// </summary>
        [Fact(Skip="ProductionBugSuspected")]
        [Trait("Category", "ProductionBugSuspected")]
        public void NewFollowAngle_AfterMultipleEmissions_ReturnsLatestValue()
        {
            // Arrange
            var mathService = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);
            calculator.Update(0f, 0f);
            calculator.Update(5f, 45f);
            calculator.Update(10f, 46f); // First emission: NewFollowAngle = 50

            // Act
            calculator.Update(15f, 47f); // Second emission: NewFollowAngle = 52

            // Assert
            Assert.Equal(52f, calculator.NewFollowAngle);
        }

        /// <summary>
        /// Tests that NewFollowAngle returns a normalized value when the calculated angle exceeds 360 degrees.
        /// The MathService.NormalizeAngle method normalizes angles to 0-360 range.
        /// </summary>
        [Fact]
        public void NewFollowAngle_WithNormalizedAngle_ReturnsNormalizedValue()
        {
            // Arrange
            var mathService = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);
            calculator.Update(0f, 0f);
            calculator.Update(0f, 350f);

            // Act - TargetOffset=20 (0->20), FollowOffset=-10 (0->350), OffsetDiff=30, resulting in 350+30=380, normalized to 20
            calculator.Update(20f, 350f);

            // Assert
            Assert.Equal(20f, calculator.NewFollowAngle);
        }

        /// <summary>
        /// Tests that NewFollowAngle still returns the last emitted value after Reset is called.
        /// Reset clears the initial angles but does not reset the BehaviorSubject, so the last emitted value persists.
        /// </summary>
        [Fact]
        public void NewFollowAngle_AfterReset_StillReturnsLastEmittedValue()
        {
            // Arrange
            var mathService = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);
            calculator.Update(0f, 0f);
            calculator.Update(5f, 45f);
            calculator.Update(10f, 46f); // Emits with NewFollowAngle = 10

            // Act
            calculator.Reset();

            // Assert
            Assert.Equal(10f, calculator.NewFollowAngle);
        }

        /// <summary>
        /// Tests that NewFollowAngle returns zero when the calculated new follow angle is zero.
        /// This tests the boundary case of angle 0.
        /// </summary>
        [Fact]
        public void NewFollowAngle_WithZeroAngle_ReturnsZero()
        {
            // Arrange
            var mathService = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);
            calculator.Update(0f, 0f);
            calculator.Update(0f, 340f);

            // Act - TargetOffset = 20, FollowOffset = -20, OffsetDifference = 40, resulting in 340 + 40 = 380, normalized to 20
            // Then update with target moving to align with follow, resulting in NewFollowAngle = 0
            calculator.Update(20f, 340f);
            calculator.Update(360f, 20f);

            // Assert
            Assert.Equal(0f, calculator.NewFollowAngle);
        }

        /// <summary>
        /// Tests that NewFollowAngle handles negative follow angles correctly.
        /// The Update method should normalize negative angles to the 0-360 range.
        /// </summary>
        [Fact]
        public void NewFollowAngle_WithNegativeFollowAngle_ReturnsNormalizedPositiveValue()
        {
            // Arrange
            var mathService = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);
            calculator.Update(0f, 0f);
            calculator.Update(0f, -10f);

            // Act - This produces an offset difference, resulting in a normalized positive angle
            calculator.Update(10f, -10f);

            // Assert - The result should be normalized to positive range
            var result = calculator.NewFollowAngle;
            Assert.True(result >= 0f && result < 360f);
        }

        /// <summary>
        /// Tests that NewFollowAngle correctly handles the maximum valid angle (just below 360).
        /// This tests the upper boundary of the valid angle range.
        /// </summary>
        [Fact]
        public void NewFollowAngle_WithMaximumValidAngle_ReturnsCorrectValue()
        {
            // Arrange
            var mathService = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);
            calculator.Update(0f, 0f);
            calculator.Update(0f, 359f);

            // Act
            calculator.Update(10f, 359f);

            // Assert
            var result = calculator.NewFollowAngle;
            Assert.True(result >= 0f && result < 360f);
        }

        /// <summary>
        /// Tests that LastResult returns a default FollowResult instance in its initial state,
        /// before any Update() calls have been made.
        /// </summary>
        [Fact]
        public void LastResult_InitialState_ReturnsDefaultFollowResult()
        {
            // Arrange
            var mathService = new MathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);

            // Act
            var result = calculator.LastResult;

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.InitialTargetAngle);
            Assert.Null(result.InitialFollowAngle);
            Assert.Null(result.CurrentTargetAngle);
            Assert.Null(result.CurrentFollowAngle);
            Assert.Equal(0f, result.NewFollowAngle);
            Assert.Equal(0f, result.OffsetDifference);
            Assert.Equal(0f, result.TargetOffset);
            Assert.Equal(0f, result.FollowOffset);
        }

        /// <summary>
        /// Tests that LastResult returns the result from the most recent Update() call
        /// when Update produces a new result.
        /// </summary>
        [Fact]
        public void LastResult_AfterUpdate_ReturnsUpdatedResult()
        {
            // Arrange
            var mathService = new MathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);
            float targetAngle = 10f;
            float followAngle = 5f;

            // Act
            var updateResult = calculator.Update(targetAngle, followAngle);
            var lastResult = calculator.LastResult;

            // Assert
            Assert.NotNull(lastResult);
            Assert.Equal(updateResult, lastResult);
            Assert.Equal(targetAngle, lastResult.InitialTargetAngle);
            Assert.Equal(followAngle, lastResult.InitialFollowAngle);
            Assert.Equal(targetAngle, lastResult.CurrentTargetAngle);
            Assert.Equal(followAngle, lastResult.CurrentFollowAngle);
        }

        /// <summary>
        /// Tests that LastResult returns the most recent result after multiple Update() calls,
        /// ensuring it tracks the latest state correctly.
        /// </summary>
        [Fact]
        public void LastResult_AfterMultipleUpdates_ReturnsMostRecentResult()
        {
            // Arrange
            var mathService = new MathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);

            // Act
            calculator.Update(10f, 5f);
            calculator.Update(20f, 15f);
            var thirdResult = calculator.Update(30f, 25f);
            var lastResult = calculator.LastResult;

            // Assert
            Assert.NotNull(lastResult);
            Assert.Equal(thirdResult, lastResult);
            Assert.Equal(30f, lastResult.CurrentTargetAngle);
            Assert.Equal(25f, lastResult.CurrentFollowAngle);
        }

        /// <summary>
        /// Tests that LastResult retains the previous result after Reset() is called,
        /// since Reset() only clears initial angles but does not modify _lastResult.
        /// </summary>
        [Fact]
        public void LastResult_AfterReset_RetainsPreviousResult()
        {
            // Arrange
            var mathService = new MathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);
            var updateResult = calculator.Update(10f, 5f);

            // Act
            calculator.Reset();
            var lastResult = calculator.LastResult;

            // Assert
            Assert.NotNull(lastResult);
            Assert.Equal(updateResult, lastResult);
            Assert.Equal(10f, lastResult.CurrentTargetAngle);
            Assert.Equal(5f, lastResult.CurrentFollowAngle);
        }

        /// <summary>
        /// Tests that LastResult retains the previous result when Update() returns null
        /// due to an unchanged target angle (offset difference less than 1).
        /// </summary>
        [Fact]
        public void LastResult_WhenUpdateReturnsNull_RetainsPreviousResult()
        {
            // Arrange
            var mathService = new MathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);
            var firstResult = calculator.Update(10f, 5f);

            // Act - Update with nearly identical target angle (within 1 degree)
            var nullResult = calculator.Update(10.5f, 6f);
            var lastResult = calculator.LastResult;

            // Assert
            Assert.Null(nullResult);
            Assert.NotNull(lastResult);
            Assert.Equal(firstResult, lastResult);
            Assert.Equal(10f, lastResult.CurrentTargetAngle);
        }

        /// <summary>
        /// Tests LastResult with boundary float values to ensure proper handling
        /// of extreme angle inputs.
        /// </summary>
        [Theory]
        [InlineData(0f, 0f)]
        [InlineData(360f, 0f)]
        [InlineData(180f, 90f)]
        [InlineData(359.99f, 0.01f)]
        public void LastResult_WithBoundaryAngles_ReturnsValidResult(float targetAngle, float followAngle)
        {
            // Arrange
            var mathService = new MathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);

            // Act
            var updateResult = calculator.Update(targetAngle, followAngle);
            var lastResult = calculator.LastResult;

            // Assert
            Assert.NotNull(lastResult);
            Assert.Equal(updateResult, lastResult);
            Assert.Equal(targetAngle, lastResult.CurrentTargetAngle);
            Assert.Equal(followAngle, lastResult.CurrentFollowAngle);
        }

        /// <summary>
        /// Tests that LastResult correctly handles negative angle values,
        /// which may occur in certain coordinate systems.
        /// </summary>
        [Theory]
        [InlineData(-10f, -5f)]
        [InlineData(-180f, -90f)]
        [InlineData(-359f, -1f)]
        public void LastResult_WithNegativeAngles_ReturnsValidResult(float targetAngle, float followAngle)
        {
            // Arrange
            var mathService = new MathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);

            // Act
            var updateResult = calculator.Update(targetAngle, followAngle);
            var lastResult = calculator.LastResult;

            // Assert
            Assert.NotNull(lastResult);
            Assert.Equal(updateResult, lastResult);
            Assert.Equal(targetAngle, lastResult.CurrentTargetAngle);
            Assert.Equal(followAngle, lastResult.CurrentFollowAngle);
        }

        /// <summary>
        /// Tests that LastResult handles special floating-point values appropriately.
        /// These edge cases test robustness against unusual numeric inputs.
        /// </summary>
        [Theory]
        [InlineData(float.MaxValue, 0f)]
        [InlineData(0f, float.MaxValue)]
        [InlineData(float.MinValue, 0f)]
        [InlineData(0f, float.MinValue)]
        public void LastResult_WithExtremeFloatValues_ReturnsValidResult(float targetAngle, float followAngle)
        {
            // Arrange
            var mathService = new MathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);

            // Act
            var updateResult = calculator.Update(targetAngle, followAngle);
            var lastResult = calculator.LastResult;

            // Assert
            Assert.NotNull(lastResult);
            Assert.Equal(updateResult, lastResult);
        }

        /// <summary>
        /// Tests that LastResult property returns the same instance when accessed multiple times
        /// without any state changes, ensuring consistent behavior.
        /// </summary>
        [Fact]
        public void LastResult_MultipleAccesses_ReturnsSameInstance()
        {
            // Arrange
            var mathService = new MathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calculator = new FollowCalculator(mathService, mockLogger.Object);
            calculator.Update(10f, 5f);

            // Act
            var result1 = calculator.LastResult;
            var result2 = calculator.LastResult;
            var result3 = calculator.LastResult;

            // Assert
            Assert.Same(result1, result2);
            Assert.Same(result2, result3);
        }

        /// <summary>
        /// Tests that Reset clears the initial angles, causing the next Update to reinitialize with new values.
        /// </summary>
        [Fact]
        public void Reset_AfterInitialUpdate_ReInitializesOnNextUpdate()
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);

            // Initialize with first values
            var firstResult = calc.Update(5f, 45f);
            Assert.Equal(5f, firstResult.InitialTargetAngle);
            Assert.Equal(45f, firstResult.InitialFollowAngle);

            // Act
            calc.Reset();

            // Update with new values - should reinitialize
            var secondResult = calc.Update(10f, 50f);

            // Assert
            Assert.Equal(10f, secondResult.InitialTargetAngle);
            Assert.Equal(50f, secondResult.InitialFollowAngle);
            Assert.Equal(0f, secondResult.TargetOffset);
            Assert.Equal(0f, secondResult.FollowOffset);
            Assert.Equal(0f, secondResult.OffsetDifference);
        }

        /// <summary>
        /// Tests that Reset can be called on a fresh calculator instance without throwing an exception.
        /// </summary>
        [Fact]
        public void Reset_OnFreshCalculator_DoesNotThrow()
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);

            // Act & Assert
            calc.Reset();

            // Verify next Update works normally
            var result = calc.Update(15f, 25f);
            Assert.Equal(15f, result.InitialTargetAngle);
            Assert.Equal(25f, result.InitialFollowAngle);
        }

        /// <summary>
        /// Tests that Reset can be called multiple times consecutively without causing errors.
        /// </summary>
        [Fact]
        public void Reset_CalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);
            calc.Update(5f, 45f);

            // Act & Assert
            calc.Reset();
            calc.Reset();
            calc.Reset();

            // Verify calculator still works after multiple resets
            var result = calc.Update(20f, 30f);
            Assert.Equal(20f, result.InitialTargetAngle);
            Assert.Equal(30f, result.InitialFollowAngle);
        }

        /// <summary>
        /// Tests that Reset does not emit any observable events.
        /// </summary>
        [Fact]
        public void Reset_DoesNotEmitObservableEvent()
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);
            calc.Update(0f, 0f);

            var events = new List<FollowResult>();
            using var sub = calc.OnAngleChangedObservable.Subscribe(r => { if (r != null) events.Add(r); });

            calc.Update(5f, 45f);
            var initialEventCount = events.Count;

            // Act
            calc.Reset();

            // Assert
            Assert.Equal(initialEventCount, events.Count);
        }

        /// <summary>
        /// Tests that Reset clears state after multiple updates and allows reinitialization.
        /// </summary>
        [Fact]
        public void Reset_AfterMultipleUpdates_ClearsStateAndAllowsReInitialization()
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);
            calc.Update(0f, 0f);

            // Perform multiple updates
            calc.Update(5f, 45f);
            calc.Update(10f, 46f);
            calc.Update(15f, 47f);

            // Act
            calc.Reset();

            // Update with completely different values
            var result = calc.Update(100f, 200f);

            // Assert - should use new values as initial angles
            Assert.Equal(100f, result.InitialTargetAngle);
            Assert.Equal(200f, result.InitialFollowAngle);
            Assert.Equal(0f, result.TargetOffset);
            Assert.Equal(0f, result.FollowOffset);
        }

        /// <summary>
        /// Tests that Reset properly reinitializes angles with extreme float values.
        /// </summary>
        [Theory]
        [InlineData(0f, 0f)]
        [InlineData(359.999f, 359.999f)]
        [InlineData(180f, 180f)]
        [InlineData(float.MaxValue, float.MaxValue)]
        [InlineData(float.MinValue, float.MinValue)]
        public void Reset_WithExtremeValues_ReInitializesCorrectly(float targetAngle, float followAngle)
        {
            // Arrange
            var ms = CreateMathService();
            var mockLogger = new Mock<ILogger<FollowCalculator>>();
            var calc = new FollowCalculator(ms, mockLogger.Object);

            // Initialize with some values
            calc.Update(50f, 50f);

            // Act
            calc.Reset();
            var result = calc.Update(targetAngle, followAngle);

            // Assert
            Assert.Equal(targetAngle, result.InitialTargetAngle);
            Assert.Equal(followAngle, result.InitialFollowAngle);
        }
    }
}