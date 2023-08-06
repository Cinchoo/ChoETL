using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoCSVReaderTest
{
    // COPY (and cleanup for fiddle) from the Unity Source Code https://github.com/Unity-Technologies/UnityCsReference/blob/master/Runtime/Export/Math/Vector3.cs
    public partial struct Vector3 : IEquatable<Vector3>
    {
        // *Undocumented*
        public const float kEpsilon = 0.00001F;
        // *Undocumented*
        public const float kEpsilonNormalSqrt = 1e-15F;
        // X component of the vector.
        public float x;

        // Y component of the vector.
        public float y;
        // Z component of the vector.
        public float z;
        // Access the x, y, z components using [0], [1], [2] respectively.
        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return x;
                    case 1:
                        return y;
                    case 2:
                        return z;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector3 index!");
                }
            }

            set
            {
                switch (index)
                {
                    case 0:
                        x = value;
                        break;
                    case 1:
                        y = value;
                        break;
                    case 2:
                        z = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector3 index!");
                }
            }
        }

        // Creates a new vector with given x, y, z components.
        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        // used to allow Vector3s to be used as keys in hash tables
        public override int GetHashCode()
        {
            return x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2);
        }

        // also required for being able to use Vector3s as keys in hash tables
        public override bool Equals(object other)
        {
            if (!(other is Vector3))
                return false;
            return Equals((Vector3)other);
        }

        public bool Equals(Vector3 other)
        {
            return x == other.x && y == other.y && z == other.z;
        }

        // *undoc* --- we have normalized property now
        public static Vector3 Normalize(Vector3 value)
        {
            float mag = Magnitude(value);
            if (mag > kEpsilon)
                return value / mag;
            else
                return zero;
        }

        // Makes this vector have a ::ref::magnitude of 1.
        public void Normalize()
        {
            float mag = Magnitude(this);
            if (mag > kEpsilon)
                this = this / mag;
            else
                this = zero;
        }

        // Returns this vector with a ::ref::magnitude of 1 (RO).
        public Vector3 normalized
        {
            get
            {
                return Vector3.Normalize(this);
            }
        }

        // Dot Product of two vectors.
        public static float Dot(Vector3 lhs, Vector3 rhs)
        {
            return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;
        }

        // *undoc* --- there's a property now
        public static float Magnitude(Vector3 vector)
        {
            return (float)Math.Sqrt(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z);
        }

        // Returns the length of this vector (RO).
        public float magnitude
        {
            get
            {
                return (float)Math.Sqrt(x * x + y * y + z * z);
            }
        }

        // *undoc* --- there's a property now
        public static float SqrMagnitude(Vector3 vector)
        {
            return vector.x * vector.x + vector.y * vector.y + vector.z * vector.z;
        }

        // Returns the squared length of this vector (RO).
        public float sqrMagnitude
        {
            get
            {
                return x * x + y * y + z * z;
            }
        }

        static readonly Vector3 zeroVector = new Vector3(0F, 0F, 0F);
        static readonly Vector3 oneVector = new Vector3(1F, 1F, 1F);
        static readonly Vector3 upVector = new Vector3(0F, 1F, 0F);
        static readonly Vector3 downVector = new Vector3(0F, -1F, 0F);
        static readonly Vector3 leftVector = new Vector3(-1F, 0F, 0F);
        static readonly Vector3 rightVector = new Vector3(1F, 0F, 0F);
        static readonly Vector3 forwardVector = new Vector3(0F, 0F, 1F);
        static readonly Vector3 backVector = new Vector3(0F, 0F, -1F);
        static readonly Vector3 positiveInfinityVector = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        static readonly Vector3 negativeInfinityVector = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        // Shorthand for writing @@Vector3(0, 0, 0)@@
        public static Vector3 zero
        {
            get
            {
                return zeroVector;
            }
        }

        // Shorthand for writing @@Vector3(1, 1, 1)@@
        public static Vector3 one
        {
            get
            {
                return oneVector;
            }
        }

        // Shorthand for writing @@Vector3(0, 0, 1)@@
        public static Vector3 forward
        {
            get
            {
                return forwardVector;
            }
        }

        public static Vector3 back
        {
            get
            {
                return backVector;
            }
        }

        // Shorthand for writing @@Vector3(0, 1, 0)@@
        public static Vector3 up
        {
            get
            {
                return upVector;
            }
        }

        public static Vector3 down
        {
            get
            {
                return downVector;
            }
        }

        public static Vector3 left
        {
            get
            {
                return leftVector;
            }
        }

        // Shorthand for writing @@Vector3(1, 0, 0)@@
        public static Vector3 right
        {
            get
            {
                return rightVector;
            }
        }

        // Shorthand for writing @@Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity)@@
        public static Vector3 positiveInfinity
        {
            get
            {
                return positiveInfinityVector;
            }
        }

        // Shorthand for writing @@Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity)@@
        public static Vector3 negativeInfinity
        {
            get
            {
                return negativeInfinityVector;
            }
        }

        // Adds two vectors.
        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        // Subtracts one vector from another.
        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        // Negates a vector.
        public static Vector3 operator -(Vector3 a)
        {
            return new Vector3(-a.x, -a.y, -a.z);
        }

        // Multiplies a vector by a number.
        public static Vector3 operator *(Vector3 a, float d)
        {
            return new Vector3(a.x * d, a.y * d, a.z * d);
        }

        // Multiplies a vector by a number.
        public static Vector3 operator *(float d, Vector3 a)
        {
            return new Vector3(a.x * d, a.y * d, a.z * d);
        }

        // Divides a vector by a number.
        public static Vector3 operator /(Vector3 a, float d)
        {
            return new Vector3(a.x / d, a.y / d, a.z / d);
        }

        // Returns true if the vectors are equal.
        public static bool operator ==(Vector3 lhs, Vector3 rhs)
        {
            // Returns false in the presence of NaN values.
            float diff_x = lhs.x - rhs.x;
            float diff_y = lhs.y - rhs.y;
            float diff_z = lhs.z - rhs.z;
            float sqrmag = diff_x * diff_x + diff_y * diff_y + diff_z * diff_z;
            return sqrmag < kEpsilon * kEpsilon;
        }

        // Returns true if vectors are different.
        public static bool operator !=(Vector3 lhs, Vector3 rhs)
        {
            // Returns true in the presence of NaN values.
            return !(lhs == rhs);
        }
    }
}
