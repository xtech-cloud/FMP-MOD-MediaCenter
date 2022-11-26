
using System.Xml.Serialization;

namespace XTC.FMP.MOD.MediaCenter.LIB.Unity
{
    /// <summary>
    /// 配置类
    /// </summary>
    public class MyConfig : MyConfigBase
    {
        public class Summary
        {
            [XmlAttribute("beginDelay")]
            public float beginDelay { get; set; } = 5;
            [XmlAttribute("endDelay")]
            public float endDelay { get; set; } = 5;
            [XmlAttribute("speed")]
            public float speed { get; set; } = 30;
        }

        public class Style
        {
            [XmlAttribute("name")]
            public string name { get; set; } = "";
            [XmlAttribute("primaryColor")]
            public string primaryColor { get; set; } = "";
            [XmlElement("Summary")]
            public Summary summary { get; set; } = new Summary();
        }


        [XmlArray("Styles"), XmlArrayItem("Style")]
        public Style[] styles { get; set; } = new Style[0];
    }
}

