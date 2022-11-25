using System;
using System.IO;
using UnityEngine;

namespace XTC.FMP.MOD.MediaCenter.LIB.Unity
{
    /// <summary>
    /// 内容读取器
    /// </summary>
    public class FileReader
    {
        protected ObjectsPool fileObjectsPool_ { get; private set; }

        public FileReader(ObjectsPool _fileObjectsPool)
        {
            fileObjectsPool_ = _fileObjectsPool;
        }

        /// <summary>
        /// 加载纹理
        /// </summary>
        /// <param name="_file">文件相对路径，相对于包含format.json的资源文件夹</param>
        public void LoadTexture(string _file, Action<Texture2D> _onFinish, Action _onError)
        {
            fileObjectsPool_.LoadTexture(_file, null, _onFinish, _onError);
        }
    }
}

