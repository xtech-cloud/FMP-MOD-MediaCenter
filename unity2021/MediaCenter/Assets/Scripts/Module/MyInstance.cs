using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LibMVCS = XTC.FMP.LIB.MVCS;
using XTC.FMP.MOD.MediaCenter.LIB.Proto;
using XTC.FMP.MOD.MediaCenter.LIB.MVCS;
using Newtonsoft.Json;
using System.Collections;
using System;

namespace XTC.FMP.MOD.MediaCenter.LIB.Unity
{
    /// <summary>
    /// 实例类
    /// </summary>
    public class MyInstance : MyInstanceBase
    {
        public enum Filter
        {
            ALL,
            IMAGE,
            VIDEO,
            DOCUMENT
        }

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
            public RawImage pageHomeBackground;
            public Transform homePage;
            public Transform homeEntry;
            public Transform viewerPage;
            public Transform viewerEntry;
            public Button btnBack;
            public Button btnFold;
            public ScrollRect summary;
            /// <summary>
            /// 浏览器的容器
            /// </summary>
            public RectTransform viewerContainer;
            public Button btnPrev;
            public Button btnNext;
            public RectTransform activeMark;

            public Toggle tgTabAll;
            public Toggle tgTabImage;
            public Toggle tgTabVideo;
            public Toggle tgTabDocument;

            public Button btnVideoLoop;

            public List<GameObject> homeEntryCloneS = new List<GameObject>();
            public List<GameObject> viewerEntryCloneS = new List<GameObject>();
        }

        private ContentReader contentReader_;
        private UiReference uiReference_ = new UiReference();
        private bool viewerContainerVisible = false;
        private MetaSchema metaSchema_;
        private MetaSchema.Entry activeEntry_;
        private List<MetaSchema.Entry> filterEntryS = new List<MetaSchema.Entry>();
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

        private Coroutine coroutineScrollSummary_;
        private float summaryBeginDelayTimer_;
        private float summaryEndDelayTimer_;
        private bool isOpened_ = false;

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

            uiReference_.pageHomeBackground = rootUI.transform.Find("bg").GetComponent<RawImage>();
            uiReference_.pageHomeBackground.gameObject.SetActive(style_.pageHomeBackground.visible);
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
            uiReference_.summary = rootUI.transform.Find("Viewer/Summary").GetComponent<ScrollRect>();
            uiReference_.tgTabAll = rootUI.transform.Find("Home/tabbar/tgAll").GetComponent<Toggle>();
            uiReference_.tgTabImage = rootUI.transform.Find("Home/tabbar/tgImage").GetComponent<Toggle>();
            uiReference_.tgTabVideo = rootUI.transform.Find("Home/tabbar/tgVideo").GetComponent<Toggle>();
            uiReference_.tgTabDocument = rootUI.transform.Find("Home/tabbar/tgDocument").GetComponent<Toggle>();
            viewerImage_ = new ImageViewer();
            viewerImage_.Setup(rootUI, contentReader_, fileReader_);
            viewerImage_.maxZoomIn = style_.toolBar.imageZoom.maxIn;
            viewerVideo_ = new VideoViewer();
            viewerVideo_.Setup(mono_, rootUI, contentReader_, fileReader_, style_);

            applyStyle();
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
            if (isOpened_)
                return;
            isOpened_ = true;
            fileObjectsPool_.Prepare();
            viewerImage_.HandleInstanceOpened();
            viewerVideo_.HandleInstanceOpened();
            Refresh(_source, _uri);
        }

        /// <summary>
        /// 当被关闭时
        /// </summary>
        public void HandleClosed()
        {
            isOpened_ = false;
            rootUI.gameObject.SetActive(false);
            Clean();

            fileObjectsPool_.Dispose();

            if (null != coroutineScrollSummary_)
            {
                mono_.StopCoroutine(coroutineScrollSummary_);
                coroutineScrollSummary_ = null;
            }
        }

        public void Refresh(string _source, string _uri)
        {
            uiReference_.homePage.gameObject.SetActive(true);
            uiReference_.viewerPage.gameObject.SetActive(false);
            rootUI.gameObject.SetActive(true);
            viewerContainerVisible = false;

            Clean();

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

            coroutineScrollSummary_ = mono_.StartCoroutine(scrollSummary());
        }

        public void Clean()
        {
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
        }

        private void applyStyle()
        {
            Color primaryColor;
            if (!ColorUtility.TryParseHtmlString(style_.primaryColor, out primaryColor))
            {
                primaryColor = Color.white;
            }
            rootUI.transform.Find("Viewer/pending").GetComponent<RawImage>().color = primaryColor;
            uiReference_.tgTabAll.transform.Find("Background/Checkmark").GetComponent<RawImage>().color = primaryColor;
            uiReference_.tgTabImage.transform.Find("Background/Checkmark").GetComponent<RawImage>().color = primaryColor;
            uiReference_.tgTabVideo.transform.Find("Background/Checkmark").GetComponent<RawImage>().color = primaryColor;
            uiReference_.tgTabDocument.transform.Find("Background/Checkmark").GetComponent<RawImage>().color = primaryColor;
            uiReference_.viewerPage.Find("container/ToolBar/VideoViewer/sdSeeker/Fill Area/Fill").GetComponent<Image>().color = primaryColor;
            uiReference_.viewerPage.Find("container/ToolBar/VideoViewer/sdSeeker/Handle Slide Area/Handle").GetComponent<Image>().color = primaryColor;
            uiReference_.viewerPage.Find("container/ToolBar/VideoViewer/sdVolume/Fill Area/Fill").GetComponent<Image>().color = primaryColor;
            {
                var rectTransform = uiReference_.viewerPage.Find("container/ToolBar/VideoViewer/sdSeeker").GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(style_.toolBar.videoProgress.width, rectTransform.sizeDelta.y);
            }

            var glgHomeContainer = uiReference_.homeEntry.parent.GetComponent<GridLayoutGroup>();
            glgHomeContainer.padding.left = style_.homeContainer.padding.left;
            glgHomeContainer.padding.right = style_.homeContainer.padding.right;
            glgHomeContainer.padding.top = style_.homeContainer.padding.top;
            glgHomeContainer.padding.bottom = style_.homeContainer.padding.bottom;
            glgHomeContainer.cellSize = new Vector2(style_.homeContainer.cellSize.width, style_.homeContainer.cellSize.height);
            glgHomeContainer.spacing = new Vector2(style_.homeContainer.spacing.x, style_.homeContainer.spacing.y);
            glgHomeContainer.constraintCount = style_.homeContainer.row;

            var glgViewerContainer = uiReference_.viewerEntry.parent.GetComponent<GridLayoutGroup>();
            glgViewerContainer.padding.left = style_.viewerContainer.padding.left;
            glgViewerContainer.padding.right = style_.viewerContainer.padding.right;
            glgViewerContainer.padding.top = style_.viewerContainer.padding.top;
            glgViewerContainer.padding.bottom = style_.viewerContainer.padding.bottom;
            glgViewerContainer.cellSize = new Vector2(style_.viewerContainer.cellSize.width, style_.viewerContainer.cellSize.height);
            glgViewerContainer.spacing = new Vector2(style_.viewerContainer.spacing.x, style_.viewerContainer.spacing.y);
            var sizeDeltaViewerContainer = uiReference_.viewerContainer.sizeDelta;
            sizeDeltaViewerContainer.y = style_.viewerContainer.padding.top + style_.viewerContainer.padding.bottom + style_.viewerContainer.cellSize.height;
            uiReference_.viewerContainer.sizeDelta = sizeDeltaViewerContainer;

            Action<string, RawImage> loadTheme = (_image, _target) =>
            {
                if (!string.IsNullOrEmpty(_image))
                {
                    loadTextureFromTheme(_image, (_texture) =>
                    {
                        _target.texture = _texture;
                    }, () => { });
                }
            };
            loadTheme(style_.pageHomeBackground.image, uiReference_.pageHomeBackground);
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
            uiReference_.tgTabAll.onValueChanged.AddListener((_toggled) =>
            {
                if (!_toggled)
                    return;
                filterEntry(Filter.ALL);
            });
            uiReference_.tgTabImage.onValueChanged.AddListener((_toggled) =>
            {
                if (!_toggled)
                    return;
                filterEntry(Filter.IMAGE);
            });
            uiReference_.tgTabVideo.onValueChanged.AddListener((_toggled) =>
            {
                if (!_toggled)
                    return;
                filterEntry(Filter.VIDEO);
            });
            uiReference_.tgTabDocument.onValueChanged.AddListener((_toggled) =>
            {
                if (!_toggled)
                    return;
                filterEntry(Filter.DOCUMENT);
            });
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
            int imageCount = 0;
            int videoCount = 0;
            int documentCount = 0;
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
                    activeEntry_ = entry;
                    onHomeEntryClick();
                });
                cloneViewerEntry.GetComponent<Button>().onClick.AddListener(() =>
                {
                    activeEntry_ = entry;
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

                if (viewerImage_.IsExtensionMatch(extension))
                    imageCount += 1;
                else if (viewerVideo_.IsExtensionMatch(extension))
                    videoCount += 1;
            }
            uiReference_.activeMark.SetAsLastSibling();
            filterEntryS.Clear();
            filterEntryS.AddRange(metaSchema_.entryS);

            System.Action<int, string, GameObject> setTabVisible = (_entryCount, _visible, _target) =>
            {
                if (_visible == "auto")
                    _target.SetActive(_entryCount > 0);
                else if (_visible == "alwaysShow")
                    _target.SetActive(true);
                else
                    _target.SetActive(false);
            };

            setTabVisible(imageCount, style_.pageTabbar.image.visible, uiReference_.tgTabImage.gameObject);
            setTabVisible(videoCount, style_.pageTabbar.video.visible, uiReference_.tgTabVideo.gameObject);
            setTabVisible(documentCount, style_.pageTabbar.document.visible, uiReference_.tgTabDocument.gameObject);
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
            int index = filterEntryS.IndexOf(activeEntry_);
            uiReference_.btnPrev.interactable = index > 0;
            uiReference_.btnNext.interactable = index >= 0 && index < filterEntryS.Count - 1;

            uiReference_.summary.transform.Find("Viewport/Content/text").GetComponent<Text>().text = activeEntry_.summary;
            uiReference_.summary.gameObject.SetActive(!string.IsNullOrEmpty(activeEntry_.summary));
            summaryBeginDelayTimer_ = 0f;
            summaryEndDelayTimer_ = 0f;

            string extension = System.IO.Path.GetExtension(activeEntry_.file);
            viewerImage_.CloseEntry();
            viewerVideo_.CloseEntry();
            if (viewerImage_.IsExtensionMatch(extension))
            {
                viewerImage_.OpenEntry(activeEntry_._source, activeEntry_.file);
            }
            else if (viewerVideo_.IsExtensionMatch(extension))
            {
                viewerVideo_.OpenEntry(activeEntry_._source, activeEntry_.file);
            }
        }


        private void openPrevEntry()
        {
            int index = filterEntryS.IndexOf(activeEntry_);
            if (index < 0)
                return;
            index -= 1;
            if (index < 0)
                index = 0;
            activeEntry_ = filterEntryS[index];
            openEntry();
        }

        private void openNextEntry()
        {
            int index = filterEntryS.IndexOf(activeEntry_);
            if (index < 0)
                return;

            index += 1;
            if (index >= filterEntryS.Count)
                index = filterEntryS.Count - 1;
            activeEntry_ = filterEntryS[index];
            openEntry();
        }

        private IEnumerator markActive()
        {
            yield return new WaitForEndOfFrame();
            uiReference_.activeMark.anchoredPosition = uiReference_.viewerEntry.parent.Find(activeEntry_.file).GetComponent<RectTransform>().anchoredPosition;
        }

        private void filterEntry(Filter _filter)
        {
            filterEntryS.Clear();
            foreach (var entry in metaSchema_.entryS)
            {
                string extension = Path.GetExtension(entry.file);
                bool visible = false;
                if (viewerImage_.IsExtensionMatch(extension))
                {
                    visible = Filter.ALL == _filter || Filter.IMAGE == _filter;
                }
                else if (viewerVideo_.IsExtensionMatch(extension))
                {
                    visible = Filter.ALL == _filter || Filter.VIDEO == _filter;
                }
                var homeEntry = uiReference_.homeEntryCloneS.Find((_item) =>
                {
                    return _item.name == entry.file;
                });
                if (null != homeEntry)
                {
                    homeEntry.SetActive(visible);
                }
                var viewerEntry = uiReference_.viewerEntryCloneS.Find((_item) =>
                {
                    return _item.name == entry.file;
                });
                if (null != viewerEntry)
                {
                    viewerEntry.SetActive(visible);
                }
                if (visible)
                {
                    filterEntryS.Add(entry);
                }
            }
        }

        private IEnumerator scrollSummary()
        {
            RectTransform rectTransform = uiReference_.summary.GetComponent<RectTransform>();
            while (true)
            {
                yield return new WaitForEndOfFrame();
                summaryBeginDelayTimer_ += Time.deltaTime;
                if (summaryBeginDelayTimer_ < style_.summary.beginDelay)
                {
                    uiReference_.summary.horizontalNormalizedPosition = 0;
                    continue;
                }
                float duration = rectTransform.rect.width / style_.summary.speed;
                float offset = Time.deltaTime / duration;
                float value = uiReference_.summary.horizontalNormalizedPosition;
                value += offset;
                if (value > 1)
                {
                    summaryEndDelayTimer_ += Time.deltaTime;
                    if (summaryEndDelayTimer_ < style_.summary.endDelay)
                    {
                        continue;
                    }
                    value = 0;
                    summaryBeginDelayTimer_ = 0f;
                    summaryEndDelayTimer_ = 0f;
                }

                uiReference_.summary.horizontalNormalizedPosition = value;
            }

        }
    }
}
