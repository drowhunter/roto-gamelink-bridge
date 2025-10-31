using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RotoGLBridge.Services
{
    /// <summary>
    /// Provides common mathematical helpers for working with angles in degrees.
    /// </summary>
    public class MathService
    {
        /// <summary>
        /// Normalizes an angle expressed in degrees to the range [0, 360).
        /// </summary>
        /// <param name="angle">The angle in degrees to normalize. May be any finite value (positive, negative, or greater than 360).</param>
        /// <returns>
        /// A value equivalent to the input angle but constrained to the half-open interval [0, 360).
        /// For example, <c>-90</c> becomes <c>270</c>, and <c>450</c> becomes <c>90</c>.
        /// </returns>
        /// <remarks>
        /// The method uses modulus arithmetic to reduce the magnitude and then shifts negative results into the
        /// positive range by adding 360. The returned value is always greater than or equal to 0 and less than 360.
        /// </remarks>
        public float NormalizeAngle(float angle)
        {
            angle = angle % 360;
            if (angle < 0)
            {
                angle += 360;
            }
            return angle;
        }

        /// <summary>
        /// Calculates the minimal signed angular difference from <paramref name="currentAngle"/> to <paramref name="targetAngle"/>.
        /// </summary>
        /// <param name="currentAngle">The starting angle in degrees.</param>
        /// <param name="targetAngle">The desired target angle in degrees.</param>
        /// <returns>
        /// The smallest signed difference (target - current) in degrees, normalized to the range [-180, 180].
        /// Positive values indicate a clockwise rotation from <paramref name="currentAngle"/> towards <paramref name="targetAngle"/>,
        /// while negative values indicate a counter-clockwise rotation.
        /// </returns>
        /// <remarks>
        /// The computed delta is first obtained by subtracting <paramref name="currentAngle"/> from <paramref name="targetAngle"/>.
        /// If the raw difference exceeds 180 degrees it is reduced by 360 degrees; if it is less than -180 degrees it is increased by 360 degrees.
        /// This ensures rotation always takes the shortest direction.
        /// </remarks>
        public float CalculateOffsetAngle(float currentAngle, float targetAngle)
        {
            float deltaAngle = targetAngle - currentAngle;
            // Normalize the delta to the range [-180, 180]
            if (deltaAngle > 180)
            {
                deltaAngle -= 360;
            }
            else if (deltaAngle < -180)
            {
                deltaAngle += 360;
            }
            return deltaAngle;
        }

    }
}
