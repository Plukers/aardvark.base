﻿using System;

namespace Aardvark.Base
{
    public interface IPixImage
    {
        PixFormat PixFormat { get; }
    }

    public interface IPixImage2d : IPixImage
    {
        V2i Size { get; }
        Array Data { get; }
    }

    public interface IPixImage3d : IPixImage
    {
        V3i Size { get; }
        Array Data { get; }
    }

    public interface IPixMipMap2d
    {
        int LevelCount { get; }
        IPixImage2d this[int level] { get; }
    }

    public interface IPixCube
    {
        IPixMipMap2d this[CubeSide side] { get; }
    }

    public class PixMipMap2d : IPixMipMap2d
    {
        protected IPixImage2d[] m_imageArray;

        #region Constructor

        public PixMipMap2d(IPixImage2d[] imageArray)
        {
            m_imageArray = imageArray;
        }

        #endregion

        #region IPixMipMap2d

        public virtual int LevelCount
        {
            get { return m_imageArray.Length; }
        }

        public virtual IPixImage2d this[int level]
        {
            get { return m_imageArray[level]; }
        }

        #endregion
    }

    public class PixCube : IPixCube
    {
        public IPixMipMap2d[] MipMapArray;

        #region Constructor

        public PixCube(IPixMipMap2d[] mipMapArray)
        {
            MipMapArray = mipMapArray;
        }

        #endregion

        #region IPixCube

        public IPixMipMap2d this[CubeSide side]
        {
            get { return MipMapArray[(int)side]; }
        }

        #endregion
    }
}