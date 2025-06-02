namespace Sharpie.Helpers.Filters
{
    public static class Maths
    {
        /// <summary>
        /// Apply the sign of y to x if y is less than 0
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static float CopyNegSign(float x, float y)
        {
            return x * (y < 0 ? -1 : 1);
        }

        /// <summary>
        /// Apply the sign of y to x
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static float CopySign(float x, float y)
        {
            return x * MathF.Sign(y);
        }

        public static float DeadZone(float x, float deadZone, float minY, float maxY)
        {
            if (Deadband(x, deadZone) == 0)
                return 0;

            return CopyNegSign(EnsureMapRange(MathF.Abs(x), deadZone, 1, MathF.Abs(minY), MathF.Abs(maxY)), x);
        }


        public static float Deadband(float x, float deadZone, float minY, float maxY)
        {
            var scaled = EnsureMapRange(x, minY, maxY, -1, 1);
            var y = 0f;

            if (MathF.Abs(scaled) > deadZone)
                y = EnsureMapRange(MathF.Abs(scaled), deadZone, 1, 0, 1) * MathF.Sign(x);

            return EnsureMapRange(y, -1, 1, minY, maxY);
        }

        public static float Deadband(float x, float deadZone)
        {
            if (MathF.Abs(x) >= MathF.Abs(deadZone))
                return x;

            return 0f;
        }

        public static float MapRange(float x, float xMin, float xMax, float yMin, float yMax)
        {
            return yMin + (yMax - yMin) * (x - xMin) / (xMax - xMin);
        }

        public static float EnsureMapRange(float x, float xMin, float xMax, float yMin, float yMax)
        {
            return MathF.Max(MathF.Min(MapRange(x, xMin, xMax, yMin, yMax), MathF.Max(yMin, yMax)), MathF.Min(yMin, yMax));
        }
    }
}
