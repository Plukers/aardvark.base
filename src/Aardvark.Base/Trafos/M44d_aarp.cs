using System;
using System.Text;

namespace Aardvark.Base
{

    public partial struct M44d : ISquareMatrix<M44d, V4d, V3d, double>
    {
        #region Matrix Arithmetics

        /// <summary>
        /// Multiplacation of a <see cref="M44d"/> with a <see cref="Scale3d"/>.
        /// </summary>
        public static M44d Multiply(M44d matrix, Scale3d scale)
        {
            return new M44d(
                    matrix.M00 * scale.X,
                    matrix.M01 * scale.Y,
                    matrix.M02 * scale.Z,
                    matrix.M03,

                    matrix.M10 * scale.X,
                    matrix.M11 * scale.Y,
                    matrix.M12 * scale.Z,
                    matrix.M13,

                    matrix.M20 * scale.X,
                    matrix.M21 * scale.Y,
                    matrix.M22 * scale.Z,
                    matrix.M23,

                    matrix.M30 * scale.X,
                    matrix.M31 * scale.Y,
                    matrix.M32 * scale.Z,
                    matrix.M33
                    );
        }

        /// <summary>
        /// Multiplacation of a <see cref="M44d"/> with a <see cref="Shift3d"/>.
        /// </summary>
        public static M44d Multiply(M44d matrix, Shift3d shift)
        {

            return new M44d(
                    matrix.M00,
                    matrix.M01,
                    matrix.M02,
                    matrix.M00 * shift.X +
                    matrix.M01 * shift.Y +
                    matrix.M02 * shift.Z +
                    matrix.M03,

                    matrix.M10,
                    matrix.M11,
                    matrix.M12,
                    matrix.M10 * shift.X +
                    matrix.M11 * shift.Y +
                    matrix.M12 * shift.Z +
                    matrix.M13,

                    matrix.M20,
                    matrix.M21,
                    matrix.M22,
                    matrix.M20 * shift.X +
                    matrix.M21 * shift.Y +
                    matrix.M22 * shift.Z +
                    matrix.M23,

                    matrix.M30,
                    matrix.M31,
                    matrix.M32,
                    matrix.M33
                    );
        }

        /// <summary>
        /// A <see cref="M44d"/> is transformed to <see cref="M33d"/> by deleting specified row and column
        /// </summary>
        
        public M33d Minor(int rowToDelete, int columnToDelete)
        {
            M33d result = new M33d();
            int checked_row = 0;
            for (int actual_row = 0; actual_row < 4; actual_row++)
            {
                int checked_column = 0;

                if (actual_row != rowToDelete)
                {
                    for (int actual_column = 0; actual_column < 4; actual_column++)
                    {
                        if (actual_column != columnToDelete)
                        {
                            result[checked_row, checked_column] = this[actual_row, actual_column];
                            checked_column++;
                        }
                    }
                    checked_row++;
                }
            }
            return result;
        }

        /// <summary>
        /// Returns transpose of a <see cref="M44d"/>.
        /// </summary>
        public static M44d Transpose(M44d m)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Transforms a direction <see cref="V4d"/> by a <see cref="M44d"/>.
        /// </summary>
        public static V4d Transform(M44d m, V4d v)
        {
            return new V4d(
                m.M00 * v.X + m.M01 * v.Y + m.M02 * v.Z + m.M03 * v.W,
                m.M10 * v.X + m.M11 * v.Y + m.M12 * v.Z + m.M13 * v.W,
                m.M20 * v.X + m.M21 * v.Y + m.M22 * v.Z + m.M23 * v.W,
                m.M30 * v.X + m.M31 * v.Y + m.M32 * v.Z + m.M33 * v.W
                );
        }

        /// <summary>
        /// Transforms a <see cref="V3d"/> position by the transpose of a <see cref="M44d"/>.
        /// Projective transform is performed.
        /// </summary>
        public static V3d TransposedTransformPosProj(M44d matrix, V3d position)
        {
            double s = 1 / (matrix.M03 * position.X
                           + matrix.M13 * position.Y
                           + matrix.M23 * position.Z + matrix.M33);
            return (TransposedTransformDir(matrix, position)) * s;
        }

        #endregion

        #region Coordinate-System Transforms

        /// <summary>
        /// Computes from a <see cref="V3d"/> point (origin) and
        /// a <see cref="V3d"/> normal the transformation matrix
        /// and its inverse.
        /// </summary>
        /// <param name="origin">The point which will become the new origin.</param>
        /// <param name="normal">The normal vector of the new ground plane.</param>
        /// <param name="local2global">A <see cref="M44d"/>The trafo from local to global system.</param>
        /// <param name="global2local">A <see cref="M44d"/>The trafofrom global to local system.</param>
        public static void NormalFrame(V3d origin, V3d normal,
            out M44d local2global, out M44d global2local
            )
        {
            V3d min;
            double x = Fun.Abs(normal.X);
            double y = Fun.Abs(normal.Y);
            double z = Fun.Abs(normal.Z);
            
            if (x < y)
            {
                if (x < z) { min = V3d.XAxis; } else { min = V3d.ZAxis; }
            }
            else
            {
                if (y < z) { min = V3d.YAxis; } else { min = V3d.ZAxis; }
            }

            V3d xVec = V3d.Cross(normal, min);
            xVec.Normalize(); // this is now guaranteed to be normal to the input normal
            V3d yVec = V3d.Cross(normal, xVec);
            yVec.Normalize();
            V3d zVec = normal;
            zVec.Normalize();

            local2global = new M44d(xVec.X, yVec.X, zVec.X, origin.X,
                                    xVec.Y, yVec.Y, zVec.Y, origin.Y,
                                    xVec.Z, yVec.Z, zVec.Z, origin.Z,
                                    0, 0, 0, 1);

            M44d mat = new M44d(xVec.X, xVec.Y, xVec.Z, 0,
                                yVec.X, yVec.Y, yVec.Z, 0,
                                zVec.X, zVec.Y, zVec.Z, 0,
                                0, 0, 0, 1);

            var shift = M44d.Translation(-origin);
            global2local = mat * shift;
        }


        /// <summary>
        /// Computes a Coordiante Frame Transformation (Basis) from current CS into
        /// the (X, Y, Z)-System at a given Origin.
        /// Note: you can use it, to transform from RH to LH and vice-versa, all depending
        /// how you will specifie your new basis-vectors.
        /// </summary>
        /// <param name="xVec">New X Vector</param>
        /// <param name="yVec">New Y Vector</param>
        /// <param name="zVec">New Z vector</param>
        /// <param name="oVec">New Origin.</param>
        /// <param name="viewTrafo"></param>
        /// <param name="viewTrafoInverse"></param>
        public static void CoordinateFrameTransform(V3d xVec, V3d yVec, V3d zVec, V3d oVec, 
            out M44d viewTrafo, out M44d viewTrafoInverse)
        {
            oVec = -oVec;
            viewTrafo = new M44d(
                xVec.X, xVec.Y, xVec.Z, xVec.X * oVec.X + xVec.Y * oVec.Y + xVec.Z * oVec.Z,
                yVec.X, yVec.Y, yVec.Z, yVec.X * oVec.X + yVec.Y * oVec.Y + yVec.Z * oVec.Z,
                zVec.X, zVec.Y, zVec.Z, zVec.X * oVec.X + zVec.Y * oVec.Y + zVec.Z * oVec.Z,
                0, 0, 0, 1
                );
            viewTrafoInverse = new M44d(
                xVec.X, yVec.X, zVec.X, -oVec.X,
                xVec.Y, yVec.Y, zVec.Y, -oVec.Y,
                xVec.Z, yVec.Z, zVec.Z, -oVec.Z,
                0, 0, 0, 1
                );
        }

        /// <summary>
        ///  Provides perspective projection matrix in terms of the vertical field of view angle a and the aspect ratio r.
        /// </summary>
        public static M44d PerspectiveProjectionTransformRH(double a, double r, double n, double f)
        {
            //D / r     0      0      0
            //  0       D      0      0
            //  0       0      A      B
            //  0       0      -1     0
            double D = 1 / Fun.Tan(a / 2);
            double A = f / (n - f);
            double B = f * n / (n - f);

            M44d P = new M44d(
                D / r, 0, 0, 0,
                0, D, 0, 0,
                0, 0, A,  B,
                0, 0, -1, 0);
            return P;
        }

        /// <summary>
        ///  Provides perspective projection matrix. 
        ///  The parameters describe the dimensions of the view volume.
        /// </summary>
        public static M44d PerspectiveProjectionTransformRH(V2d size, double n, double f)
        {
            double w = size.X;
            double h = size.Y;
            // Fx      0      0      0
            //  0      Fy     0      0
            //  0      0      A      B
            //  0      0      -1     0
            double Fx = 2 * n / w;
            double Fy = 2 * n / h;
            double A = f / (n - f);
            double B = n * f / (n - f);
            M44d P = new M44d(
                Fx, 0, 0, 0,
                0, Fy, 0, 0,
                0, 0, A,  B,
                0, 0, -1, 0);
            return P;
        }

        /// <summary>
        /// Builds a customized, right-handed perspective Off-Center projection matrix.
        /// </summary>
        public static M44d PerspectiveProjectionTransformRH(double l, double r, double t, double b, double n, double f)
        {
            // Fx      0      Sx     0
            //  0      Fy     Sy     0
            //  0      0      A      B
            //  0      0      -1     0
            double Fx = 2 * n / (r - l);
            double Fy = 2 * n / (t - b);
            double Sx = (l + r) / (r - l);
            double Sy = (t + b) / (t - b);
            double A = f / (n - f);
            double B = n * f / (n - f);

            M44d P = new M44d(
                Fx, 0, Sx, 0,
                0, Fy, Sy, 0,
                0, 0,  A, B,
                0, 0, -1, 0);
            return P;
        }



        /// <summary>
        /// Builds a customized, left-handed perspective Off-Center projection matrix.
        /// </summary>
        public static M44d PerspectiveProjectionTransformLH(double l, double r, double t, double b, double n, double f)
        {
            //  Fx     0      0     0
            //  0      Fy     0     0
            //  Sx     Sy     A     1
            //  0      0      B     0
            double Fx = 2 * n / (r - l);
            double Fy = 2 * n / (t - b);
            double Sx = (l + r) / (l - r);
            double Sy = (t + b) / (b - t);
            double A = f / (f - n);
            double B = n * f / (n - f);

            M44d P = new M44d(
                Fx, 0, 0, 0,
                0, Fy, 0, 0,
                Sx, Sy, A, 1,
                0, 0, B, 0);
            return P;
        }
        #endregion

        #region Operators

        /// <summary>
        /// Calculates the product of a <see cref="M44d"/> with a <see cref="Shift3d"/>.
        /// </summary>
        public static M44d operator *(M44d matrix, Shift3d shift)
        {
            return M44d.Multiply(matrix, shift);
        }

        /// <summary>
        /// Calculates the product of a <see cref="M44d"/> with a <see cref="Scale3d"/>.
        /// </summary>
        public static M44d operator *(M44d matrix, Scale3d scale)
        {
            return M44d.Multiply(matrix, scale);
        }

        #endregion

        #region Static creators

        /// <summary>
        /// Creates new Identity with 3 double values for translation.
        /// </summary>
        /// <returns>Translation matrix.</returns>
        public static M44d Translation(double dx, double dy, double dz)
        {
            return new M44d(1, 0, 0, dx,
                            0, 1, 0, dy,
                            0, 0, 1, dz,
                            0, 0, 0, 1);
        }

        /// <summary>
        /// Creates new Identity with V3d vector for translation.
        /// </summary>
        /// <returns>Translation matrix.</returns>
        public static M44d Translation(V3d v)
        {
            return new M44d(1, 0, 0, v.X,
                            0, 1, 0, v.Y,
                            0, 0, 1, v.Z,
                            0, 0, 0, 1);
        }

        /// <summary>
        /// Creates new Identity <see cref="M44d"/> with a <see cref="Shift3d"/> for translation.
        /// </summary>
        /// <returns>Translation matrix.</returns>
        public static M44d Translation(Shift3d s)
        {
            return new M44d(1, 0, 0, s.X,
                            0, 1, 0, s.Y,
                            0, 0, 1, s.Z,
                            0, 0, 0, 1);
        }

        /// <summary>
        /// Creates new Identity with scalar value for uniform-scaling.
        /// </summary>
        /// <returns>Uniform-scaling matrix.</returns>
        public static M44d Scale(double s)
        {
            return new M44d(s, 0, 0, 0,
                            0, s, 0, 0,
                            0, 0, s, 0,
                            0, 0, 0, 1);
        }

        /// <summary>
        /// Creates new Identity with 3 scalar values for scaling.
        /// </summary>
        /// <returns>Scaling matrix.</returns>
        public static M44d Scale(double sx, double sy, double sz)
        {
            return new M44d(sx, 0, 0, 0,
                            0, sy, 0, 0,
                            0, 0, sz, 0,
                            0, 0, 0, 1);
        }

        /// <summary>
        /// Creates new Identity with V3d for scaling.
        /// </summary>
        /// <returns>Scaling matrix.</returns>
        public static M44d Scale(V3d v)
        {
            return new M44d(v.X, 0, 0, 0,
                            0, v.Y, 0, 0,
                            0, 0, v.Z, 0,
                            0, 0, 0, 1);
        }

        /// <summary>
        /// Creates new Identity <see cref="M44d"/> with <see cref="Scale3d"/> for scaling.
        /// </summary>
        /// <param name="scale"></param>
        /// <returns>A <see cref="M44d"/> scaling matrix.</returns>
        public static M44d Scale(Scale3d scale)
        {
            return new M44d(scale.X, 0, 0, 0,
                            0, scale.Y, 0, 0,
                            0, 0, scale.Z, 0,
                            0, 0, 0, 1);
        }

        /// <summary>
        /// Creates rotation matrix from axis and angle.
        /// </summary>
        public static M44d Rotation(V3d axis, double angleInRadians)
        {
            return (M44d)(new Rot3d(axis, angleInRadians));
        }

        /// <summary>
        /// Creates rotation matrix from yaw, pitch, and roll. 
        /// </summary>
        public static M44d Rotation(
            double yawInRadians, double pitchInRadians, double rollInRadians
            )
        {
            return (M44d)(new Rot3d(
                yawInRadians,pitchInRadians, rollInRadians
                ));
        }

        /// <summary>
        /// Creates rotation matrix from yaw, pitch, and roll Vector.
        /// </summary>
        public static M44d Rotation(V3d yaw_pitch_roll_inRadians)
        {
            return (M44d)(new Rot3d(
                yaw_pitch_roll_inRadians.X,
                yaw_pitch_roll_inRadians.Y,
                yaw_pitch_roll_inRadians.Z));
        }

        /// <summary>
        /// Creates rotation matrix which rotates one vector into another.
        /// </summary>
        public static M44d Rotation(V3d from, V3d into)
        {
            return (M44d)(new Rot3d(from, into));
        }

        /// <summary>
        /// Creates rotational matrix from quaternion.
        /// </summary>
        /// <returns>Rotational matrix.</returns>
        public static M44d Rotation(Rot3d r)
        {
            return (M44d)r;
        }

        /// <summary>
        /// Creates new rotational matrix for "double value"-radians around X-Axis.
        /// </summary>
        /// <returns>Rotational matrix.</returns>
        public static M44d RotationX(double angleRadians)
        {
            double cos = Fun.Cos(angleRadians);
            double sin = Fun.Sin(angleRadians);
            return new M44d(1, 0, 0, 0,
                            0, cos, -sin, 0,
                            0, sin, cos, 0,
                            0, 0, 0, 1);
        }

        /// <summary>
        /// Creates new rotational matrix for "double value"-radians around Y-Axis.
        /// </summary>
        /// <returns>Rotational matrix.</returns>
        public static M44d RotationY(double angleRadians)
        {
            double cos = Fun.Cos(angleRadians);
            double sin = Fun.Sin(angleRadians);
            return new M44d(cos, 0, -sin, 0,
                            0, 1, 0, 0,
                            sin, 0, cos, 0,
                            0, 0, 0, 1);
        }

        /// <summary>
        /// Creates new rotational matrix for "double value"-radians around Z-Axis.
        /// </summary>
        /// <returns>Rotational matrix.</returns>
        public static M44d RotationZ(double angleRadians)
        {
            double cos = Fun.Cos(angleRadians);
            double sin = Fun.Sin(angleRadians);
            return new M44d(cos, -sin, 0, 0,
                            sin, cos, 0, 0,
                            0, 0, 1, 0,
                            0, 0, 0, 1);
        }

        public static M44d ShearXY(double factorX, double factorY)
        {
            return new M44d(1, 0, factorX, 0,
                            0, 1, factorY, 0,
                            0, 0, 1,       0,
                            0, 0, 0,       1);
        }

        public static M44d ShearXZ(double factorX, double factorZ)
        {
            return new M44d(1, factorX, 0, 0,
                            0, 1,       0, 0,
                            0, factorZ, 1, 0,
                            0, 0,       0, 1);
        }

        public static M44d ShearYZ(double factorY, double factorZ)
        {
            return new M44d(1,       0, 0, 0,
                            factorY, 1, 0, 0,
                            factorZ, 0, 1, 0,
                            0,       0, 0, 1);
        }


        /// <summary>
        /// Returns the matrix that transforms from the coordinate system
        /// specified by the basis into the world cordinate system.
        /// </summary>
        public static M44d FromBasis(V3d xAxis, V3d yAxis, V3d zAxis, V3d orign)
        {
            return new M44d(
                xAxis.X, yAxis.X, zAxis.X, orign.X,
                xAxis.Y, yAxis.Y, zAxis.Y, orign.Y,
                xAxis.Z, yAxis.Z, zAxis.Z, orign.Z,
                0, 0, 0, 1);
        }

        #endregion

        #region ITransform<V3d> implementation.

        /// <summary>
        /// Converts this matrix to its adjoint.
        /// </summary>
        /// <returns>This.</returns>
        public M44d Adjoin()
        {
            return this = Adjoint;
        }

        /// <summary>
        /// Returns adjoint of this matrix.
        /// </summary>
        public M44d Adjoint
        {
            get
            {
                M44d result = new M44d();
                for (int row = 0; row < 4; row++)
                {
                    for (int col = 0; col < 4; col++)
                    {
                        if (((col + row) % 2) == 0)
                            result[col, row] = Minor(row, col).Det;
                        else
                            result[col, row] = -Minor(row, col).Det;
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Transforms vector v.
        /// </summary>
        public V4d Transform(V4d v)
        {
            return this * v;
        }

        /// <summary>
        /// Transforms direction vector v (p.w is presumed 0)
        /// with the inverse of this transform.
        /// </summary>
        public V3d InvTransformDir(V3d v)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Transforms point p (p.w is presumed 1.0) with the inverse of this transform.
        /// No projective transform is performed.
        /// </summary>
        public V3d InvTransformPos(V3d p)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Transforms point p (p.w is presumed 1.0) with the inverse of this transform.
        /// Projective transform is performed.
        /// </summary>
        public V3d InvTransformPosProj(V3d p)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
