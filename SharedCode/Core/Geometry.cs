using System;

namespace SharedCode.Core
{
    public static class Geometry
    {
        public static bool LineToCircleCollision(
            Vector2 lineStart,
            Vector2 lineEnd,
            Vector2 circleCenter,
            float radius)
        {
            if (PointToCircleCollision(lineStart, circleCenter, radius) ||
                PointToCircleCollision(lineEnd, circleCenter, radius))
                return true;

            float length = Distance(lineStart, lineEnd);
            float dot = ((circleCenter.X - lineStart.X) * (lineEnd.X - lineStart.X) +
                         (circleCenter.Y - lineStart.Y) * (lineEnd.Y - lineStart.Y)) / MathF.Pow(length, 2);

            var closest = new Vector2
            {
                X = lineStart.X + (dot * (lineEnd.X - lineStart.X)),
                Y = lineStart.Y + (dot * (lineEnd.Y - lineStart.Y))
            };

            if (!LineToPointCollision(lineStart, lineEnd, closest))
                return false;

            if (Distance(closest, circleCenter) <= radius)
                return true;

            return false;
        }

        public static bool PointToCircleCollision(
            Vector2 point,
            Vector2 circleCenter,
            float radius)
        {
            if (Distance(point, circleCenter) <= radius)
                return true;

            return false;
        }

        public static bool LineToPointCollision(
            Vector2 lineStart,
            Vector2 lineEnd,
            Vector2 point)
        {
            float distanceFromStart = Distance(point, lineStart);
            float distanceFromEnd = Distance(point, lineEnd);
            float lineLength = Distance(lineStart, lineEnd);

            const float buffer = 0.1f;

            if (distanceFromStart + distanceFromEnd >= lineLength - buffer
                && distanceFromStart + distanceFromEnd <= lineLength + buffer)
            {
                return true;
            }

            return false;
        }

        public static float Distance(Vector2 a, Vector2 b)
        {
            float distanceX = a.X - b.X;
            float distanceY = a.Y - b.Y;
            return MathF.Sqrt(distanceX * distanceX + distanceY * distanceY);
        }
    }
}