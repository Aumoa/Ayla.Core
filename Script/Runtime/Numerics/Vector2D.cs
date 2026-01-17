#nullable enable

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Ayla
{
    [Serializable]
    public struct Vector2D : IFormattable, IEquatable<Vector2D>
    {
        public double X;
        public double Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2D(double x, double y)
        {
            X = x;
            Y = y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly string ToString()
        {
            return ToString(null, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly bool Equals(object obj)
        {
            if (obj is Vector2D v)
            {
                return Equals(v);
            }

            return false;
        }

        public readonly string ToString(string? format, IFormatProvider? formatProvider = null)
        {
            if (string.IsNullOrEmpty(format))
            {
                format = "F2";
            }

            if (formatProvider == null)
            {
                formatProvider = CultureInfo.InvariantCulture.NumberFormat;
            }

            return string.Format(formatProvider, "({0}, {1})", X.ToString(format, formatProvider), Y.ToString(format, formatProvider));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly bool IEquatable<Vector2D>.Equals(Vector2D rhs)
        {
            return Equals(rhs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(in Vector2D rhs)
        {
            return X == rhs.X && Y == rhs.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(double newX, double newY)
        {
            X = newX;
            Y = newY;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Normalize()
        {
            double lengthSquared = LengthSquared;
            if (lengthSquared > 0.0)
            {
                double invLength = 1.0 / Math.Sqrt(lengthSquared);
                X *= invLength;
                Y *= invLength;
            }
        }

        public readonly Vector2D Normalized
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Vector2D result = this;
                result.Normalize();
                return result;
            }
        }

        public readonly double LengthSquared
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => X * X + Y * Y;
        }

        public readonly double Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Math.Sqrt(LengthSquared);
        }

        public double this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => index switch
            {
                0 => X,
                1 => Y,
                _ => throw new IndexOutOfRangeException("Invalid Vector2D index!")
            };
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                switch (index)
                {
                    case 0:
                        X = value;
                        break;
                    case 1:
                        Y = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector2D index!");
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2D Lerp(in Vector2D a, in Vector2D b, double t)
        {
            t = Math.Clamp(t, 0.0, 1.0);
            return new Vector2D
            {
                X = a.X + (b.X - a.X) * t,
                Y = a.Y + (b.Y - a.Y) * t
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2D LerpUnclamped(in Vector2D a, in Vector2D b, double t)
        {
            return new Vector2D
            {
                X = a.X + (b.X - a.X) * t,
                Y = a.Y + (b.Y - a.Y) * t
            };
        }

        public static Vector2D MoveTowards(in Vector2D current, in Vector2D target, double maxDistanceDelta)
        {
            double dx = target.X - current.X;
            double dy = target.Y - current.Y;
            double sqrm = dx * dx + dy * dy;
            if (sqrm == 0 || (maxDistanceDelta >= 0 && sqrm <= maxDistanceDelta * maxDistanceDelta))
            {
                return target;
            }

            double m = Math.Sqrt(sqrm);
            double invm = 1.0 / m;
            return new Vector2D(current.X + dx * invm * maxDistanceDelta, current.Y + dy * invm * maxDistanceDelta);
        }

        /// <summary>
        /// Reflects a vector off the surface defined by a normal.
        /// </summary>
        /// <param name="inDirection"> The direction vector towards the surface. </param>
        /// <param name="inNormal"> The normal vector that defines the surface. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2D Reflect(in Vector2D inDirection, in Vector2D inNormal)
        {
            double num = -2 * Dot(inNormal, inDirection);
            return new Vector2D(num * inNormal.X + inDirection.X, num * inNormal.Y + inDirection.Y);
        }

        /// <summary>
        /// Returns the 2D vector perpendicular to this 2D vector. The result is always rotated
        /// 90-degrees in a counter-clockwise direction for a 2D coordinate system where
        /// the positive Y axis goes up.
        /// </summary>
        /// <param name="inDirection"> The input direction. </param>
        /// <returns> The perpendicular direction. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2D Perpendicular(in Vector2D inDirection)
        {
            return new Vector2D(0 - -inDirection.Y, inDirection.X);
        }

        /// <summary>
        /// Dot Product of two vectors.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Dot(in Vector2D lhs, in Vector2D rhs)
        {
            return lhs.X * rhs.X + lhs.Y * rhs.Y;
        }

        /// <summary>
        /// Gets the unsigned angle in degrees between from and to.
        /// </summary>
        /// <param name="from"> The vector from which the angular difference is measured. </param>
        /// <param name="to"> The vector to which the angular difference is measured. </param>
        /// <returns> The unsigned angle in degrees between the two vectors. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Angle(in Vector2D from, in Vector2D to)
        {
            double num = Math.Sqrt(from.LengthSquared * to.LengthSquared);
            if (num < 0)
            {
                return 0;
            }

            double num2 = Math.Clamp(Dot(from, to) / num, -1, 1);
            return Math.Acos(num2) * 57.29578;
        }

        /// <summary>
        /// Gets the signed angle in degrees between from and to.
        /// </summary>
        /// <param name="from"> The vector from which the angular difference is measured. </param>
        /// <param name="to"> The vector to which the angular difference is measured. </param>
        /// <returns> The signed angle in degrees between the two vectors. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SignedAngle(in Vector2D from, in Vector2D to)
        {
            double angle = Angle(from, to);
            double sign = Math.Sign(from.X * to.Y - from.Y * to.X);
            return angle * sign;
        }

        /// <summary>
        /// Returns the distance between a and b.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Distance(in Vector2D lhs, in Vector2D rhs)
        {
            double dx = lhs.X - rhs.X;
            double dy = lhs.Y - rhs.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Returns a vector that is made from the smallest components of two vectors.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2D Min(in Vector2D lhs, in Vector2D rhs)
        {
            return new Vector2D { X = Math.Min(lhs.X, rhs.X), Y = Math.Min(lhs.Y, rhs.Y) };
        }

        /// <summary>
        /// Returns a vector that is made from the largest components of two vectors.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2D Max(in Vector2D lhs, in Vector2D rhs)
        {
            return new Vector2D { X = Math.Max(lhs.X, rhs.X), Y = Math.Max(lhs.Y, rhs.Y) };
        }

        /// <summary>
        /// Returns a vector that is made from the largest components of two vectors.
        /// </summary>
        public static Vector2D SmoothDamp(Vector2D current, Vector2D target, ref Vector2D currentVelocity, double smoothTime, double maxSpeed, double deltaTime)
        {
            smoothTime = Math.Max(0.0001, smoothTime);
            double dampingSpeed = 2 / smoothTime;
            double dampingFactor = dampingSpeed * deltaTime;
            double smoothingRate = 1 / (1 + dampingFactor + 0.48 * dampingFactor * dampingFactor + 0.235 * dampingFactor * dampingFactor * dampingFactor);
            double dx = current.X - target.X;
            double dy = current.Y - target.Y;
            Vector2D adjustedTarget = target;
            double maxDeltaPosition = maxSpeed * smoothTime;
            double maxDeltaSquared = maxDeltaPosition * maxDeltaPosition;
            double distanceSquared = dx * dx + dy * dy;
            if (distanceSquared > maxDeltaSquared)
            {
                double distance = (double)Math.Sqrt(distanceSquared);
                dx = dx / distance * maxDeltaPosition;
                dy = dy / distance * maxDeltaPosition;
            }

            target.X = current.X - dx;
            target.Y = current.Y - dy;
            double velocityChangeX = (currentVelocity.X + dampingSpeed * dx) * deltaTime;
            double velocityChangeY = (currentVelocity.Y + dampingSpeed * dy) * deltaTime;
            currentVelocity.X = (currentVelocity.X - dampingSpeed * velocityChangeX) * smoothingRate;
            currentVelocity.Y = (currentVelocity.Y - dampingSpeed * velocityChangeY) * smoothingRate;
            double newPositionX = target.X + (dx + velocityChangeX) * smoothingRate;
            double newPositionY = target.Y + (dy + velocityChangeY) * smoothingRate;
            double targetDeltaX = adjustedTarget.X - current.X;
            double targetDeltaY = adjustedTarget.Y - current.Y;
            double newDeltaX = newPositionX - adjustedTarget.X;
            double newDeltaY = newPositionY - adjustedTarget.Y;
            if (targetDeltaX * newDeltaX + targetDeltaY * newDeltaY > 0)
            {
                newPositionX = adjustedTarget.X;
                newPositionY = adjustedTarget.Y;
                currentVelocity.X = (newPositionX - adjustedTarget.X) / deltaTime;
                currentVelocity.Y = (newPositionY - adjustedTarget.Y) / deltaTime;
            }

            return new Vector2D(newPositionX, newPositionY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2D(in Vector2 value)
            => new() { X = value.x, Y = value.y };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector2(in Vector2D value)
            => new((float)value.X, (float)value.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2D operator +(in Vector2D lhs, in Vector2D rhs)
            => new() { X = lhs.X + rhs.X, Y = lhs.Y + rhs.Y };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2D operator -(in Vector2D lhs, in Vector2D rhs)
            => new() { X = lhs.X - rhs.X, Y = lhs.Y - rhs.Y };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2D operator *(in Vector2D lhs, in Vector2D rhs)
            => new() { X = lhs.X * rhs.X, Y = lhs.Y * rhs.Y };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2D operator *(in Vector2D lhs, double rhs)
            => new() { X = lhs.X * rhs, Y = lhs.Y * rhs };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2D operator *(double lhs, in Vector2D rhs)
            => new() { X = lhs * rhs.X, Y = lhs * rhs.Y };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2D operator /(in Vector2D lhs, in Vector2D rhs)
            => new() { X = lhs.X / rhs.X, Y = lhs.Y / rhs.Y };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2D operator /(in Vector2D lhs, double rhs)
            => new() { X = lhs.X / rhs, Y = lhs.Y / rhs };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2D operator /(double lhs, in Vector2D rhs)
            => new() { X = lhs / rhs.X, Y = lhs / rhs.Y };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2D operator -(in Vector2D value)
            => new() { X = -value.X, Y = -value.Y };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Vector2D lhs, in Vector2D rhs)
        {
            double dx = lhs.X - rhs.X;
            double dy = lhs.Y - rhs.Y;
            return dx * dx + dy * dy < double.Epsilon;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Vector2D lhs, in Vector2D rhs)
        {
            double dx = lhs.X - rhs.X;
            double dy = lhs.Y - rhs.Y;
            return dx * dx + dy * dy >= double.Epsilon;
        }

        public static Vector2D Zero
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new() { X = 0.0, Y = 0.0 };
        }

        public static Vector2D One
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new() { X = 1.0, Y = 1.0 };
        }

        public static Vector2D Up
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new() { X = 0.0, Y = 1.0 };
        }

        public static Vector2D Down
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new() { X = 0.0, Y = -1.0 };
        }

        public static Vector2D Left
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new() { X = -1.0, Y = 0.0 };
        }

        public static Vector2D Right
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new() { X = 1.0, Y = 0.0 };
        }

        public static Vector2D PositiveInfinity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new() { X = double.PositiveInfinity, Y = double.PositiveInfinity };
        }

        public static Vector2D NegativeInfinity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new() { X = double.NegativeInfinity, Y = double.NegativeInfinity };
        }
    }
}
