using Xunit;
using RotoGLBridge.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RotoGLBridge.Tests.Services
{
    
    public struct ScenarioStep
    {
        public float TargetAngle;
        public float FollowAngle;
        public float ExpectedInitialTargetAngle;
        public float ExpectedInitialFollowAngle ;

        public float ExpectedTargetOffset ;
        public float ExpectedFollowOffset;

        public float ExpectedOffsetDifference ;

        public float ExpectedNewFollowAngle;

        public ScenarioStep(float target, float follow, float xInitTarg, float xInitFoll, float xTargOff, float xFollOff, float xOffDiff, float xNewFoll)
        {
            TargetAngle = target;
            FollowAngle = follow;
            ExpectedInitialTargetAngle = xInitTarg;
            ExpectedInitialFollowAngle = xInitFoll;
            ExpectedTargetOffset = xTargOff;
            ExpectedFollowOffset = xFollOff;
            ExpectedOffsetDifference = xOffDiff;
            ExpectedNewFollowAngle = xNewFoll;
        }
    }


    public class FollowTargetCalculatorTests
    {
        IFollowTargetCalculator _followTargetCalculator;

        

        public FollowTargetCalculatorTests()
        {
            var ms = new MathService();

            _followTargetCalculator = new FollowTargetCalculator(ms);
        }

        [Fact]
        public void Clockwise_follow_should_work()
        {
            var steps = new List<ScenarioStep>
            {
                new (5f,   45f,   5f,  45f,   0,   0,   0,  45f),
                new (10f,  46f,   5f,  45f,   5,   1,   4,  50f),
                new (20f,  50f,   5f,  45f,  15,   5,  10,  60f),
                new (25f,  52f,   5f,  45f,  20,   7,  13,  65f),
                  
            };

            foreach (var step in steps)
            {
                RunTest(step);
            }
        }

        [Fact]
        public void Counterclockwise_follow_should_work()
        {
            var steps = new List<ScenarioStep>
            {
                new (5f,   45f,   5f,  45f,   0,   0,   0,  45f),
                new (355f, 40f,   5f,  45f, -10,  -5,  -5,  35f),
                new (350f, 38f,   5f,  45f, -15,  -7,  -8,  30f),

            };

            foreach (var step in steps)
            {
                RunTest(step);
            }
        }

        [Fact]
        public void Clockwise_follow_crossing_180_should_work()
        {
            var steps = new List<ScenarioStep>
            {
                new (5f,   45f,   5f,  45f,   0,   0,   0,  45f),
                new (10f,  46f,   5f,  45f,   5,   1,   4,  50f),
                new (170f, 90f,   5f,  45f, 165,  45, 120, 210f),
                new (190f, 170f,  5f,  45f, -175, 125,  60, 230f),


            };

            foreach (var step in steps)
            {
                RunTest(step);
            }
        }


        public void RunTest(ScenarioStep step)
        {
            
            _followTargetCalculator.Update(step.TargetAngle, step.FollowAngle);

            Assert.Equal(step.ExpectedInitialTargetAngle, _followTargetCalculator.InitialTargetAngle);
            Assert.Equal(step.ExpectedInitialFollowAngle, _followTargetCalculator.InitialFollowAngle);
            Assert.Equal(step.ExpectedTargetOffset, _followTargetCalculator.TargetOffset);
            Assert.Equal(step.ExpectedFollowOffset, _followTargetCalculator.FollowOffset);
            Assert.Equal(step.ExpectedOffsetDifference, _followTargetCalculator.OffsetDifference);
            Assert.Equal(step.ExpectedNewFollowAngle, _followTargetCalculator.NewFollowAngle);



        }


    }
}
