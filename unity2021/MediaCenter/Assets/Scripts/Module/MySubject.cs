
namespace XTC.FMP.MOD.MediaCenter.LIB.Unity
{
    public class MySubject : MySubjectBase
    {
        /// <summary>
        /// 嵌入
        /// </summary>
        /// <remarks>
        /// 创建后挂载到slot中，并打开内容
        /// </remarks>
        /// <example>
        /// var data = new Dictionary<string, object>();
        /// data["uid"] = "default";
        /// data["style"] = "default";
        /// data["slot"] = a instance of UnityEngine.GameObejct;
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
        /// data["uri"] = "bundle/content/_resource/1.mc";
        /// model.Publish(/XTC/MediaCenter/Inlay, data);
        /// </example>
        public const string Refresh = "/XTC/MediaCenter/Inlay";
    }
}
