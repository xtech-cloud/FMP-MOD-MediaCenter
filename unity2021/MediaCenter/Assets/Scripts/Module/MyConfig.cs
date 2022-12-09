
using System.Xml.Serialization;

namespace XTC.FMP.MOD.MediaCenter.LIB.Unity
{
    /// <summary>
    /// 配置类
    /// </summary>
    public class MyConfig : MyConfigBase
    {
        public class Padding
        {
            [XmlAttribute("left")]
            public int left { get; set; } = 36;
            [XmlAttribute("right")]
            public int right { get; set; } = 36;
            [XmlAttribute("top")]
            public int top { get; set; } = 36;
            [XmlAttribute("bottom")]
            public int bottom { get; set; } = 36;
        }

        public class CellSize
        {
            [XmlAttribute("width")]
            public int width { get; set; } = 256;
            [XmlAttribute("height")]
            public int height { get; set; } = 256;
        }

        public class Spacing
        {
            [XmlAttribute("x")]
            public int x { get; set; } = 16;
            [XmlAttribute("y")]
            public int y { get; set; } = 16;
        }

        public class HomeContainer
        {
            [XmlAttribute("row")]
            public int row { get; set; } = 2;
            [XmlElement("Padding")]
            public Padding padding { get; set; } = new Padding();
            [XmlElement("CellSize")]
            public CellSize cellSize { get; set; } = new CellSize();
            [XmlElement("Spacing")]
            public Spacing spacing { get; set; } = new Spacing();
        }

        public class ViewerContainer
        {
            [XmlElement("Padding")]
            public Padding padding { get; set; } = new Padding();
            [XmlElement("CellSize")]
            public CellSize cellSize { get; set; } = new CellSize();
            [XmlElement("Spacing")]
            public Spacing spacing { get; set; } = new Spacing();
        }

        public class Summary
        {
            [XmlAttribute("beginDelay")]
            public float beginDelay { get; set; } = 5;
            [XmlAttribute("endDelay")]
            public float endDelay { get; set; } = 5;
            [XmlAttribute("speed")]
            public float speed { get; set; } = 30;
        }

        public class VideoProgress
        {
            [XmlAttribute("width")]
            public int width { get; set; } = 100;
        }

        public class VideoLoop
        {
            [XmlAttribute("mode")]
            public string mode { get; set; } = "none";
            [XmlAttribute("visible")]
            public bool visible { get; set; } = false;
        }

        public class ToolBar
        {
            [XmlElement("VideoProgress")]
            public VideoProgress videoProgress { get; set; } = new VideoProgress();
            [XmlElement("VideoLoop")]
            public VideoLoop videoLoop { get; set; } = new VideoLoop();
        }

        public class Background
        {
            [XmlAttribute("visible")]
            public bool visible { get; set; } = true;
            [XmlAttribute("image")]
            public string image { get; set; } = "";
        }

        public class Style
        {
            [XmlAttribute("name")]
            public string name { get; set; } = "";
            [XmlAttribute("primaryColor")]
            public string primaryColor { get; set; } = "";
            [XmlElement("PageHomeBackground")]
            public Background pageHomeBackground { get; set; } = new Background();
            [XmlElement("Summary")]
            public Summary summary { get; set; } = new Summary();
            [XmlElement("HomeContainer")]
            public HomeContainer homeContainer { get; set; } = new HomeContainer();
            [XmlElement("ViewerContainer")]
            public ViewerContainer viewerContainer { get; set; } = new ViewerContainer();

            [XmlElement("ToolBar")]
            public ToolBar toolBar { get; set; } = new ToolBar();
        }


        [XmlArray("Styles"), XmlArrayItem("Style")]
        public Style[] styles { get; set; } = new Style[0];
    }
}

