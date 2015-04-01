using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aardvark.Base;

namespace Aardvark.Base
{
    /// <summary>
    /// Wrappers for the best (fastest) available implementation of the respective tensor operation.
    /// </summary>
    public static partial class TensorExtensions
    {

        #region Conversions (Matrix, Volume) to PixImage<T>

        public static PixImage<T> ToPixImage<T>(this Matrix<T> matrix)
        {
            return matrix.AsVolume().ToPixImage();
        }

        public static PixImage<T> ToPixImage<T>(this Volume<T> volume)
        {
            return new PixImage<T>(volume.ToImage());
        }

        public static PixImage<T> ToPixImage<T>(this Volume<T> volume, Col.Format format)
        {
            var ch = format.ChannelCount();
            if (ch > volume.Size.Z)
                throw new ArgumentException("volume has not enough channels for requested format");
            if (ch < volume.Size.Z)
                volume = volume.SubVolume(V3l.Zero, new V3l(volume.SX, volume.SY, ch)); 
            return new PixImage<T>(format, volume.ToImage());
        }

        public static PixImage<T> ToPixImage<T>(this Matrix<C3b> matrix)
        {
            var pixImage = new PixImage<T>(Col.FormatDefaultOf(typeof(T), 3), (V2i)matrix.Size);
            pixImage.GetMatrix<C3b>().Set(matrix);
            return pixImage;
        }

        public static PixImage<T> ToPixImage<T>(this Matrix<C3us> matrix)
        {
            var pixImage = new PixImage<T>(Col.FormatDefaultOf(typeof(T), 3), (V2i)matrix.Size);
            pixImage.GetMatrix<C3us>().Set(matrix);
            return pixImage;
        }

        public static PixImage<T> ToPixImage<T>(this Matrix<C3f> matrix)
        {
            var pixImage = new PixImage<T>(Col.FormatDefaultOf(typeof(T), 3), (V2i)matrix.Size);
            pixImage.GetMatrix<C3f>().Set(matrix);
            return pixImage;
        }

        public static PixImage<T> ToPixImage<T>(this Matrix<C4b> matrix)
        {
            var pixImage = new PixImage<T>(Col.FormatDefaultOf(typeof(T), 4), (V2i)matrix.Size);
            pixImage.GetMatrix<C4b>().Set(matrix);
            return pixImage;
        }

        public static PixImage<T> ToPixImage<T>(this Matrix<C4us> matrix)
        {
            var pixImage = new PixImage<T>(Col.FormatDefaultOf(typeof(T), 4), (V2i)matrix.Size);
            pixImage.GetMatrix<C4us>().Set(matrix);
            return pixImage;
        }

        public static PixImage<T> ToPixImage<T>(this Matrix<C4f> matrix)
        {
            var pixImage = new PixImage<T>(Col.FormatDefaultOf(typeof(T), 4), (V2i)matrix.Size);
            pixImage.GetMatrix<C4f>().Set(matrix);
            return pixImage;
        }

        public static PixImage<T> ToPixImage<T, TMatrixData>(this Matrix<TMatrixData, C3b> matrix)
        {
            var pixImage = new PixImage<T>(Col.FormatDefaultOf(typeof(T), 3), (V2i)matrix.Size);
            pixImage.GetMatrix<C3b>().Set(matrix);
            return pixImage;
        }

        public static PixImage<T> ToPixImage<T, TMatrixData>(this Matrix<TMatrixData, C3us> matrix)
        {
            var pixImage = new PixImage<T>(Col.FormatDefaultOf(typeof(T), 3), (V2i)matrix.Size);
            pixImage.GetMatrix<C3us>().Set(matrix);
            return pixImage;
        }

        public static PixImage<T> ToPixImage<T, TMatrixData>(this Matrix<TMatrixData, C3f> matrix)
        {
            var pixImage = new PixImage<T>(Col.FormatDefaultOf(typeof(T), 3), (V2i)matrix.Size);
            pixImage.GetMatrix<C3f>().Set(matrix);
            return pixImage;
        }

        public static PixImage<T> ToPixImage<T, TMatrixData>(this Matrix<TMatrixData, C4b> matrix)
        {
            var pixImage = new PixImage<T>(Col.FormatDefaultOf(typeof(T), 4), (V2i)matrix.Size);
            pixImage.GetMatrix<C4b>().Set(matrix);
            return pixImage;
        }

        public static PixImage<T> ToPixImage<T, TMatrixData>(this Matrix<TMatrixData, C4us> matrix)
        {
            var pixImage = new PixImage<T>(Col.FormatDefaultOf(typeof(T), 4), (V2i)matrix.Size);
            pixImage.GetMatrix<C4us>().Set(matrix);
            return pixImage;
        }

        public static PixImage<T> ToPixImage<T, TMatrixData>(this Matrix<TMatrixData, C4f> matrix)
        {
            var pixImage = new PixImage<T>(Col.FormatDefaultOf(typeof(T), 4), (V2i)matrix.Size);
            pixImage.GetMatrix<C4f>().Set(matrix);
            return pixImage;
        }

        public static PixImage<T> ToPixImage<T>(this IMatrix<C3b> matrix)
        {
            if (matrix is Matrix<byte, C3b>)
                return ((Matrix<byte, C3b>)matrix).ToPixImage<T, byte>(); ;

            var pixImage = new PixImage<T>(Col.FormatDefaultOf(typeof(T), 3), (V2i)matrix.Dim);
            pixImage.GetMatrix<C3b>().Set(matrix);
            return pixImage;
        }

        public static PixImage<T> ToPixImage<T>(this IMatrix<C3us> matrix)
        {
            if (matrix is Matrix<ushort, C3us>)
                return ((Matrix<ushort, C3us>)matrix).ToPixImage<T, ushort>(); ;

            var pixImage = new PixImage<T>(Col.FormatDefaultOf(typeof(T), 3), (V2i)matrix.Dim);
            pixImage.GetMatrix<C3us>().Set(matrix);
            return pixImage;
        }

        public static PixImage<T> ToPixImage<T>(this IMatrix<C3f> matrix)
        {
            if (matrix is Matrix<float, C3f>)
                return ((Matrix<float, C3f>)matrix).ToPixImage<T, float>(); ;

            var pixImage = new PixImage<T>(Col.FormatDefaultOf(typeof(T), 3), (V2i)matrix.Dim);
            pixImage.GetMatrix<C3f>().Set(matrix);
            return pixImage;
        }

        public static PixImage<T> ToPixImage<T>(this IMatrix<C4b> matrix)
        {
            if (matrix is Matrix<byte, C4b>)
                return ((Matrix<byte, C4b>)matrix).ToPixImage<T, byte>(); ;

            var pixImage = new PixImage<T>(Col.FormatDefaultOf(typeof(T), 4), (V2i)matrix.Dim);
            pixImage.GetMatrix<C4b>().Set(matrix);
            return pixImage;
        }

        public static PixImage<T> ToPixImage<T>(this IMatrix<C4us> matrix)
        {
            if (matrix is Matrix<ushort, C4us>)
                return ((Matrix<ushort, C4us>)matrix).ToPixImage<T, ushort>(); ;

            var pixImage = new PixImage<T>(Col.FormatDefaultOf(typeof(T), 4), (V2i)matrix.Dim);
            pixImage.GetMatrix<C4us>().Set(matrix);
            return pixImage;
        }

        public static PixImage<T> ToPixImage<T>(this IMatrix<C4f> matrix)
        {
            if (matrix is Matrix<float, C4f>)
                return ((Matrix<float, C4f>)matrix).ToPixImage<T, float>(); ;

            var pixImage = new PixImage<T>(Col.FormatDefaultOf(typeof(T), 4), (V2i)matrix.Dim);
            pixImage.GetMatrix<C4f>().Set(matrix);
            return pixImage;
        }

        #endregion

        #region Conversions Volume to Volume (byte, ushort, uint, float, double) [CSharp (internal)]

        // All the following conversions are internal only, since we do not
        // know about the channel order at this level. Only PixImages know
        // about channel order.

        internal static Volume<ushort> ToUShortColor(this Volume<byte> volume)
        {
            return volume.CopyImageWindow(Col.UShortFromByte);
        }

        internal static Volume<uint> ToUIntColor(this Volume<byte> volume)
        {
            return volume.CopyImageWindow(Col.UIntFromByte);
        }

        internal static Volume<float> ToFloatColor(this Volume<byte> volume)
        {
            return volume.CopyImageWindow(Col.FloatFromByte);
        }

        internal static Volume<double> ToDoubleColor(this Volume<byte> volume)
        {
            return volume.CopyImageWindow(Col.DoubleFromByte);
        }

        internal static Volume<byte> ToByteColor(this Volume<ushort> volume)
        {
            return volume.CopyImageWindow(Col.ByteFromUShort);
        }

        internal static Volume<uint> ToUIntColor(this Volume<ushort> volume)
        {
            return volume.CopyImageWindow(Col.UIntFromUShort);
        }

        internal static Volume<float> ToFloatColor(this Volume<ushort> volume)
        {
            return volume.CopyImageWindow(Col.FloatFromUShort);
        }

        internal static Volume<double> ToDoubleColor(this Volume<ushort> volume)
        {
            return volume.CopyImageWindow(Col.DoubleFromUShort);
        }

        internal static Volume<byte> ToByteColor(this Volume<uint> volume)
        {
            return volume.CopyImageWindow(Col.ByteFromUInt);
        }

        internal static Volume<ushort> ToUShortColor(this Volume<uint> volume)
        {
            return volume.CopyImageWindow(Col.UShortFromUInt);
        }

        internal static Volume<float> ToFloatColor(this Volume<uint> volume)
        {
            return volume.CopyImageWindow(Col.FloatFromUInt);
        }

        internal static Volume<double> ToDoubleColor(this Volume<uint> volume)
        {
            return volume.CopyImageWindow(Col.DoubleFromUInt);
        }

        internal static Volume<byte> ToByteColor(this Volume<float> volume)
        {
            return volume.CopyImageWindow(Col.ByteFromFloat);
        }

        internal static Volume<ushort> ToUShortColor(this Volume<float> volume)
        {
            return volume.CopyImageWindow(Col.UShortFromFloat);
        }

        internal static Volume<uint> ToUIntColor(this Volume<float> volume)
        {
            return volume.CopyImageWindow(Col.UIntFromFloat);
        }

        internal static Volume<double> ToDoubleColor(this Volume<float> volume)
        {
            return volume.CopyImageWindow(Col.DoubleFromFloat);
        }

        internal static Volume<byte> ToByteColor(this Volume<double> volume)
        {
            return volume.CopyImageWindow(Col.ByteFromDouble);
        }

        internal static Volume<ushort> ToUShortColor(this Volume<double> volume)
        {
            return volume.CopyImageWindow(Col.UShortFromDouble);
        }

        internal static Volume<uint> ToUIntColor(this Volume<double> volume)
        {
            return volume.CopyImageWindow(Col.UIntFromDouble);
        }

        internal static Volume<float> ToFloatColor(this Volume<double> volume)
        {
            return volume.CopyImageWindow(Col.FloatFromDouble);
        }
        
        #endregion

        #region Conversions Tensor4 to Tensor4 (byte, ushort, uint, float, double) [CSharp (internal)]

        // All the following conversions are internal only, since we do not
        // know about the channel order at this level. Only PixVolumes know
        // about channel order.

        internal static Tensor4<ushort> ToUShortColor(this Tensor4<byte> tensor4)
        {
            return tensor4.CopyImageWindow(Col.UShortFromByte);
        }

        internal static Tensor4<uint> ToUIntColor(this Tensor4<byte> tensor4)
        {
            return tensor4.CopyImageWindow(Col.UIntFromByte);
        }

        internal static Tensor4<float> ToFloatColor(this Tensor4<byte> tensor4)
        {
            return tensor4.CopyImageWindow(Col.FloatFromByte);
        }

        internal static Tensor4<double> ToDoubleColor(this Tensor4<byte> tensor4)
        {
            return tensor4.CopyImageWindow(Col.DoubleFromByte);
        }

        internal static Tensor4<byte> ToByteColor(this Tensor4<ushort> tensor4)
        {
            return tensor4.CopyImageWindow(Col.ByteFromUShort);
        }

        internal static Tensor4<uint> ToUIntColor(this Tensor4<ushort> tensor4)
        {
            return tensor4.CopyImageWindow(Col.UIntFromUShort);
        }

        internal static Tensor4<float> ToFloatColor(this Tensor4<ushort> tensor4)
        {
            return tensor4.CopyImageWindow(Col.FloatFromUShort);
        }

        internal static Tensor4<double> ToDoubleColor(this Tensor4<ushort> tensor4)
        {
            return tensor4.CopyImageWindow(Col.DoubleFromUShort);
        }

        internal static Tensor4<byte> ToByteColor(this Tensor4<uint> tensor4)
        {
            return tensor4.CopyImageWindow(Col.ByteFromUInt);
        }

        internal static Tensor4<ushort> ToUShortColor(this Tensor4<uint> tensor4)
        {
            return tensor4.CopyImageWindow(Col.UShortFromUInt);
        }

        internal static Tensor4<float> ToFloatColor(this Tensor4<uint> tensor4)
        {
            return tensor4.CopyImageWindow(Col.FloatFromUInt);
        }

        internal static Tensor4<double> ToDoubleColor(this Tensor4<uint> tensor4)
        {
            return tensor4.CopyImageWindow(Col.DoubleFromUInt);
        }

        internal static Tensor4<byte> ToByteColor(this Tensor4<float> tensor4)
        {
            return tensor4.CopyImageWindow(Col.ByteFromFloat);
        }

        internal static Tensor4<ushort> ToUShortColor(this Tensor4<float> tensor4)
        {
            return tensor4.CopyImageWindow(Col.UShortFromFloat);
        }

        internal static Tensor4<uint> ToUIntColor(this Tensor4<float> tensor4)
        {
            return tensor4.CopyImageWindow(Col.UIntFromFloat);
        }

        internal static Tensor4<double> ToDoubleColor(this Tensor4<float> tensor4)
        {
            return tensor4.CopyImageWindow(Col.DoubleFromFloat);
        }

        internal static Tensor4<byte> ToByteColor(this Tensor4<double> tensor4)
        {
            return tensor4.CopyImageWindow(Col.ByteFromDouble);
        }

        internal static Tensor4<ushort> ToUShortColor(this Tensor4<double> tensor4)
        {
            return tensor4.CopyImageWindow(Col.UShortFromDouble);
        }

        internal static Tensor4<uint> ToUIntColor(this Tensor4<double> tensor4)
        {
            return tensor4.CopyImageWindow(Col.UIntFromDouble);
        }

        internal static Tensor4<float> ToFloatColor(this Tensor4<double> tensor4)
        {
            return tensor4.CopyImageWindow(Col.FloatFromDouble);
        }

        #endregion

        #region Get/Set Matrix Rows/Cols

        public static Vector<T> GetRow<T>(this Matrix<T> m, int i)
        {
            return m.SubXVector(i);
        }

        public static Vector<T> GetCol<T>(this Matrix<T> m, int i)
        {
            return m.SubYVector(i);
        }

        public static void SetRow<T>(this Matrix<T> m, int i, ref Vector<T> data)
        {
            m.SubXVector(i).Set(data);
        }

        public static void SetCol<T>(this Matrix<T> m, int i, ref Vector<T> data)
        {
            m.SubYVector(i).Set(data);
        }

        #endregion


    }
}