
//*************************************************************************************
//   !!! Generated by the fmp-cli 1.67.0.  DO NOT EDIT!
//*************************************************************************************

using System.Collections.Generic;

namespace XTC.FMP.MOD.MediaCenter.LIB.Unity
{
    /// <summary>
    /// 内容类的基类
    /// </summary>
    public class MyContentBase
    {
        /// <summary>
        /// 包名
        /// </summary>
        public string bundle { get; set; } = "";

        /// <summary>
        /// 别名
        /// </summary>
        /// <remarks>
        /// 可用于首字母检索，以及显示在菜单中
        /// </remarks>
        public string alias { get; set; } = "";

        /// <summary>
        /// 别名的多国语言
        /// </summary>
        public Dictionary<string, string> alias_i18nS { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 标题
        /// </summary>
        public string title { get; set; } = "";

        /// <summary>
        /// 标题的多国语言
        /// </summary>
        public Dictionary<string, string> title_i18n { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 主题
        /// </summary>
        public string topic { get; set; } = "";

        /// <summary>
        /// 主题的多国语言
        /// </summary>
        public Dictionary<string, string> topic_i18nS { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 题注
        /// </summary>
        public string caption { get; set; } = "";

        /// <summary>
        /// 题注的多国语言
        /// </summary>
        public Dictionary<string, string> caption_i18nS { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 描述
        /// </summary>
        public string description { get; set; } = "";

        /// <summary>
        /// 描述的多国语言
        /// </summary>
        public Dictionary<string, string> description_i18nS { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 键值对
        /// </summary>
        public Dictionary<string, string> kvS { get; set; } = new Dictionary<string, string>();
    }
}

