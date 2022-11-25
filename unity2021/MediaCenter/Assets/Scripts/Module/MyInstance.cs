using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LibMVCS = XTC.FMP.LIB.MVCS;
using XTC.FMP.MOD.MediaCenter.LIB.Proto;
using XTC.FMP.MOD.MediaCenter.LIB.MVCS;
using Newtonsoft.Json;
using System.Collections;

namespace XTC.FMP.MOD.MediaCenter.LIB.Unity
{
    /// <summary>
    /// 实例类
    /// </summary>
    public class MyInstance : MyInstanceBase
    {
        public class MetaSchema
        {
            public class Entry
            {
                public string _source = "";
                public Color _imageColor = Color.white;
                public string _text = "";
                public string thumbnail = "";
                public string file = "";
                public string summary = "";
            }
            public Entry[] entryS = new Entry[0];
        }

        public class UiReference
        {
            public Transform homePage;
            public Transform homeEntry;
            public Transform viewerPage;
            public Transform viewerEntry;
            public Button btnBack;
            public Button btnFold;
            public Transform summary;
            /// <summary>
            /// 浏览器的容器
            /// </summary>
            public RectTransform viewerContainer;
            public Button btnPrev;
            public Button btnNext;
            public RectTransform activeMark;

            public List<GameObject> homeEntryCloneS = new List<GameObject>();
            public List<GameObject> viewerEntryCloneS = new List<GameObject>();
        }

        private ContentReader contentReader_;
        private UiReference uiReference_ = new UiReference();
        private bool viewerContainerVisible = false;
        private MetaSchema metaSchema_;
        private int activeEntry_;
        private Dictionary<string, System.Action<string>> openHandlerS_ = new Dictionary<string, System.Action<string>>();

        private ImageViewer viewerImage_;
        private VideoViewer viewerVideo_;

        /// <summary>
        /// 本地文件对象池，管理从本地目录中加载到内存中的对象
        /// </summary>
        /// <remarks>
        /// 在实例打开(Open)时准备，在实例关闭(Close)时清理
        /// </remarks>
        private ObjectsPool fileObjectsPool_;
        private FileReader fileReader_;

        public MyInstance(string _uid, string _style, MyConfig _config, MyCatalog _catalog, LibMVCS.Logger _logger, Dictionary<string, LibMVCS.Any> _settings, MyEntryBase _entry, MonoBehaviour _mono, GameObject _rootAttachments)
            : base(_uid, _style, _config, _catalog, _logger, _settings, _entry, _mono, _rootAttachments)
        {
            openHandlerS_["assloud://"] = openResourceWithAssloud;
            openHandlerS_["file://"] = openResourceWithFile;
            fileObjectsPool_ = new ObjectsPool(this.uid + ".File", logger_);
        }

        /// <summary>
        /// 当被创建时
        /// </summary>
        /// <remarks>
        /// 可用于加载主题目录的数据
        /// </remarks>
        public void HandleCreated()
        {
            contentReader_ = new ContentReader(contentObjectsPool);
            fileReader_ = new FileReader(fileObjectsPool_);
            contentReader_.AssetRootPath = settings_["path.assets"].AsString();

            uiReference_.homePage = rootUI.transform.Find("Home");
            uiReference_.viewerPage = rootUI.transform.Find("Viewer");
            uiReference_.homeEntry = rootUI.transform.Find("Home/container/Viewport/Content/entry");
            uiReference_.viewerEntry = rootUI.transform.Find("Viewer/container/Viewport/Content/entry");
            uiReference_.activeMark = rootUI.transform.Find("Viewer/container/Viewport/Content/mark").GetComponent<RectTransform>();
            uiReference_.btnBack = rootUI.transform.Find("Viewer/container/btnBack").GetComponent<Button>();
            uiReference_.btnFold = rootUI.transform.Find("Viewer/container/btnFold").GetComponent<Button>();
            uiReference_.viewerContainer = rootUI.transform.Find("Viewer/container").GetComponent<RectTransform>();
            uiReference_.btnPrev = rootUI.transform.Find("Viewer/btnPrev").GetComponent<Button>();
            uiReference_.btnNext = rootUI.transform.Find("Viewer/btnNext").GetComponent<Button>();
            uiReference_.homeEntry.gameObject.SetActive(false);
            uiReference_.viewerEntry.gameObject.SetActive(false);
            uiReference_.summary = rootUI.transform.Find("Viewer/Summary");
            viewerImage_ = new ImageViewer();
            viewerImage_.Setup(rootUI, contentReader_, fileReader_);
            viewerVideo_ = new VideoViewer();
            viewerVideo_.Setup(mono_, rootUI, contentReader_, fileReader_);

            bindEvents();
        }

        /// <summary>
        /// 当被删除时
        /// </summary>
        public void HandleDeleted()
        {
            contentReader_ = null;
            viewerImage_ = null;
            viewerVideo_ = null;
        }

        /// <summary>
        /// 当被打开时
        /// </summary>
        /// <remarks>
        /// 可用于加载内容目录的数据
        /// </remarks>
        public void HandleOpened(string _source, string _uri)
        {
            uiReference_.homePage.gameObject.SetActive(true);
            uiReference_.viewerPage.gameObject.SetActive(false);
            rootUI.gameObject.SetActive(true);
            viewerContainerVisible = false;

            fileObjectsPool_.Prepare();

            viewerImage_.HandleInstanceOpened();
            viewerVideo_.HandleInstanceOpened();
            switchViewerContainerVisible();
            System.Action<string> handler;
            if (openHandlerS_.TryGetValue(_source, out handler))
            {
                handler(_uri);
            }
            else
            {
                logger_.Error("none handler to open the source: {0}", _source);
            }
        }

        /// <summary>
        /// 当被关闭时
        /// </summary>
        public void HandleClosed()
        {
            rootUI.gameObject.SetActive(false);
            foreach (var obj in uiReference_.homeEntryCloneS)
            {
                GameObject.DestroyImmediate(obj);
            }
            uiReference_.homeEntryCloneS.Clear();
            foreach (var obj in uiReference_.viewerEntryCloneS)
            {
                GameObject.DestroyImmediate(obj);
            }
            uiReference_.viewerEntryCloneS.Clear();

            fileObjectsPool_.Dispose();
        }

        private void bindEvents()
        {
            uiReference_.btnBack.onClick.AddListener(() =>
            {
                uiReference_.homePage.gameObject.SetActive(true);
                uiReference_.viewerPage.gameObject.SetActive(false);
            });
            uiReference_.btnFold.onClick.AddListener(() =>
            {
                viewerContainerVisible = !viewerContainerVisible;
                switchViewerContainerVisible();
            });
            uiReference_.btnPrev.onClick.AddListener(openPrevEntry);
            uiReference_.btnNext.onClick.AddListener(openNextEntry);
            viewerImage_.onRendererClick = () =>
            {
                viewerContainerVisible = false;
                switchViewerContainerVisible();
            };
            viewerVideo_.onRendererClick = () =>
            {
                viewerContainerVisible = false;
                switchViewerContainerVisible();
            };
        }

        private void openResourceWithAssloud(string _uri)
        {
            contentReader_.ContentUri = _uri;
            contentReader_.LoadText("meta.json", (_bytes) =>
            {
                metaSchema_ = JsonConvert.DeserializeObject<MetaSchema>(System.Text.Encoding.UTF8.GetString(_bytes));
                foreach (var entry in metaSchema_.entryS)
                {
                    entry._source = "assloud://";
                    entry._text = "";
                    entry._imageColor = Color.white;
                }
                parseMeta(metaSchema_);
            }, () =>
            {

            });
        }

        private void openResourceWithFile(string _uri)
        {

            List<MetaSchema.Entry> entryS = new List<MetaSchema.Entry>();
            foreach (var file in Directory.GetFiles(_uri))
            {
                string extension = Path.GetExtension(file);
                if (!viewerImage_.IsExtensionMatch(extension) && !viewerVideo_.IsExtensionMatch(extension))
                {
                    continue;
                }
                var entry = new MetaSchema.Entry();
                entryS.Add(entry);
                entry.summary = "";
                entry.thumbnail = "";
                entry._text = Path.GetFileName(file);
                entry._source = "file://";
                entry._imageColor = Color.gray;
                entry.file = file;
            }
            metaSchema_ = new MetaSchema();
            metaSchema_.entryS = entryS.ToArray();
            parseMeta(metaSchema_);
        }

        private void parseMeta(MetaSchema _meta)
        {
            for (int i = 0; i < _meta.entryS.Length; ++i)
            {
                var index = i;
                var entry = _meta.entryS[i];
                string extension = System.IO.Path.GetExtension(entry.file);
                var cloneHomeEntry = GameObject.Instantiate(uiReference_.homeEntry.gameObject, uiReference_.homeEntry.parent);
                uiReference_.homeEntryCloneS.Add(cloneHomeEntry);
                cloneHomeEntry.name = entry.file;
                cloneHomeEntry.gameObject.SetActive(true);
                cloneHomeEntry.transform.Find("text").gameObject.SetActive(!string.IsNullOrEmpty(entry._text));
                cloneHomeEntry.transform.Find("text").GetComponent<Text>().text = entry._text;
                cloneHomeEntry.transform.Find("mark-image").gameObject.SetActive(viewerImage_.IsExtensionMatch(extension));
                cloneHomeEntry.transform.Find("mark-video").gameObject.SetActive(viewerVideo_.IsExtensionMatch(extension));
                cloneHomeEntry.GetComponent<RawImage>().color = entry._imageColor;
                var cloneViewerEntry = GameObject.Instantiate(uiReference_.homeEntry.gameObject, uiReference_.viewerEntry.parent);
                uiReference_.viewerEntryCloneS.Add(cloneViewerEntry);
                cloneViewerEntry.name = entry.file;
                cloneViewerEntry.gameObject.SetActive(true);
                cloneViewerEntry.transform.Find("text").gameObject.SetActive(!string.IsNullOrEmpty(entry._text));
                cloneViewerEntry.transform.Find("text").GetComponent<Text>().text = entry._text;
                cloneViewerEntry.GetComponent<RawImage>().color = entry._imageColor;
                cloneHomeEntry.GetComponent<Button>().onClick.AddListener(() =>
                {
                    activeEntry_ = index;
                    onHomeEntryClick();
                });
                cloneViewerEntry.GetComponent<Button>().onClick.AddListener(() =>
                {
                    activeEntry_ = index;
                    onViewerEntryClick();
                });
                if (!string.IsNullOrEmpty(entry.thumbnail))
                {
                    contentReader_.LoadTexture(entry.thumbnail, (_texture) =>
                    {
                        cloneHomeEntry.GetComponent<RawImage>().texture = _texture;
                        cloneViewerEntry.GetComponent<RawImage>().texture = _texture;
                    }, () =>
                    {

                    });
                }
            }
            uiReference_.activeMark.SetAsLastSibling();
        }

        private void onHomeEntryClick()
        {
            uiReference_.homePage.gameObject.SetActive(false);
            uiReference_.viewerPage.gameObject.SetActive(true);
            viewerContainerVisible = false;
            switchViewerContainerVisible();
            openEntry();
        }

        private void onViewerEntryClick()
        {
            openEntry();
        }


        private void switchViewerContainerVisible()
        {
            if (viewerContainerVisible)
            {
                uiReference_.viewerContainer.anchoredPosition = new Vector2(0, 0);
            }
            else
            {
                uiReference_.viewerContainer.anchoredPosition = new Vector2(0, -uiReference_.viewerContainer.sizeDelta.y);
            }
        }

        private void openEntry()
        {
            mono_.StartCoroutine(markActive());
            var entry = metaSchema_.entryS[activeEntry_];
            uiReference_.summary.Find("text").GetComponent<Text>().text = entry.summary;
            uiReference_.summary.gameObject.SetActive(!string.IsNullOrEmpty(entry.summary));
            string extension = System.IO.Path.GetExtension(entry.file);
            viewerImage_.CloseEntry();
            viewerVideo_.CloseEntry();
            if (viewerImage_.IsExtensionMatch(extension))
            {
                viewerImage_.OpenEntry(entry._source, entry.file);
            }
            else if (viewerVideo_.IsExtensionMatch(extension))
            {
                viewerVideo_.OpenEntry(entry._source, entry.file);
            }
        }


        private void openPrevEntry()
        {
            activeEntry_ -= 1;
            if (activeEntry_ < 0)
                activeEntry_ = 0;
            openEntry();
        }

        private void openNextEntry()
        {
            activeEntry_ += 1;
            if (activeEntry_ >= metaSchema_.entryS.Length)
                activeEntry_ = metaSchema_.entryS.Length - 1;
            openEntry();
        }

        private IEnumerator markActive()
        {
            yield return new WaitForEndOfFrame();
            uiReference_.activeMark.anchoredPosition = uiReference_.viewerEntry.parent.GetChild(activeEntry_ + 1).GetComponent<RectTransform>().anchoredPosition;
        }
    }
}
