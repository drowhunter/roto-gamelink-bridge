// Pseudocode / Plan:
// - Create a test class `MathServiceTests` in namespace `RotoGLBridge.Tests.Services`.
// - For access: assume tests reference the project assembly (internal `MathService` can be accessed via InternalsVisibleTo or made visible).
// - Instantiate `RotoGLBridge.Services.MathService` in each test.
// - Use xUnit Theory with multiple InlineData cases to cover:
//     - simple positive delta within [-180,180]
//     - wrap-around when delta > 180 (should subtract 360)
//     - wrap-around when delta < -180 (should add 360)
//     - boundary values like 180 and -180
//     - decimal/fractional inputs and full-rotation equivalents (e.g., +360 => 0)
// - For assertions use absolute tolerance to compare floats (Math.Abs(actual - expected) < epsilon).
//
// Test cases (currentAngle, targetAngle, expectedDelta):
// - (10, 20) -> 10
// - (350, 10) -> 20  // -340 + 360 = 20
// - (10, 350) -> -20 // 340 - 360 = -20
// - (0, 180) -> 180
// - (0, 181) -> -179
// - (0, -181) -> 179
// - (45.5, 405.5) -> 0  // 360 -> normalized to 0
// - (-90, 270) -> 0    // 360 -> 0

using System;
using Xunit;
using RotoGLBridge.Services;

namespace RotoGLBridge.Tests.Services
{
    public partial class MathServiceTests
    {
        private const float Epsilon = 1e-6f;

        [Theory]
        [InlineData(10f, 20f, 10f)]
        [InlineData(10f, 0f, -10f)]
        [InlineData(350f, 10f, 20f)]
        [InlineData(10f, 350f, -20f)]
        [InlineData(0f, 180f, 180f)]
        [InlineData(0f, 181f, -179f)]
        [InlineData(0f, -181f, 179f)]
        [InlineData(45.5f, 405.5f, 0f)]
        [InlineData(-90f, 270f, 0f)]
        public void CalculateDeltaAngle_ReturnsExpected(float currentAngle, float targetAngle, float expected)
        {
            // Arrange
            var svc = new MathService();

            // Act
            var actual = svc.CalculateDeltaAngle(currentAngle, targetAngle);

            // Assert
            Assert.True(Math.Abs(actual - expected) < Epsilon,
                $"CalculateDeltaAngle({currentAngle}, {targetAngle}) returned {actual} but expected {expected}");
        }

        [Theory]
        [InlineData(0f, 0f)]
        [InlineData(90f, 90f)]
        [InlineData(359.5f, 359.5f)]
        [InlineData(360f, 0f)]
        [InlineData(720f, 0f)]
        [InlineData(405.5f, 45.5f)]
        [InlineData(-1f, 359f)]
        [InlineData(-360f, 0f)]
        [InlineData(-405.5f, 314.5f)]
        [InlineData(359.9999f, 359.9999f)]
        public void NormalizeAngle_ReturnsExpected(float input, float expected)
        {
            const float Epsilon = 1e-6f;
            var svc = new MathService();
            var actual = svc.NormalizeAngle(input);
            Assert.True(Math.Abs(actual - expected) < Epsilon,
                $"NormalizeAngle({input}) returned {actual} but expected {expected}");
        }
    }
}