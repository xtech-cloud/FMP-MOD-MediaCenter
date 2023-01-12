
namespace XTC.FMP.MOD.MediaCenter.LIB.Unity
{
    public class MySubject : MySubjectBase
    {
        /// <summary>
        /// 嵌入
        /// </summary>
        /// <remarks>
        /// 创建后挂载到slot中
        /// </remarks>
        /// <example>
        /// var data = new Dictionary<string, object>();
        /// data["uid"] = "default";
        /// data["style"] = "default";
        /// data["uiSlot"] = a instance of UnityEngine.GameObejct;
        /// data["worldSlot"] = a instance of UnityEngine.GameObejct;
        /// model.Publish(/XTC/MediaCenter/Inlay, data);
        /// </example>
        public const string Inlay = "/XTC/MediaCenter/Inlay";

        /// <summary>
        /// 刷新内容
        /// </summary>
        /// <example>
        /// var data = new Dictionary<string, object>();
        /// data["uid"] = "default";
        /// data["source"] = "assloud://";
        /// data["uri"] = "bundle/_resource/1.mc";
        /// model.Publish(/XTC/MediaCenter/Inlay, data);
        /// </example>
        public const string Refresh = "/XTC/MediaCenter/Refresh";
    }
}
