using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using RenderHeads.Media.AVProVideo;
using System.Collections;

namespace XTC.FMP.MOD.MediaCenter.LIB.Unity
{
    public class VideoViewer
    {
        public class UiReference
        {
            public GameObject pending;
            /// <summary>
            /// 视频浏览器的面板
            /// </summary>
            public Transform panel;
            /// <summary>
            /// 图片浏览器的工具栏
            /// </summary>
            public Transform toolbar;
            public Transform renderer;
            public Slider seeker;
            public Slider volume;
            public Text textTime;
            public Button btnPlay;
            public Button btnPause;

            public Button btnLoopNone;
            public Button btnLoopSingle;

            public MediaPlayer _mediaPlayer;
            public DisplayUGUI _displayUGUI;
        }
        public Action onRendererClick;

        private MonoBehaviour mono_ { get; set; }
        private UiReference uiReference_ = new UiReference();
        private ContentReader contentReader_;
        private FileReader fileReader_;
        private List<string> extensionS_ = new List<string>() { ".mp4", ".mkv" };

        private bool wasPlayingOnScrub_;
        private float videoSeekValue_;
        private float volumeAppearTimer_;
        private Coroutine coroutineUpdate_;
        private string loopMode_ = "none";

        public void Setup(MonoBehaviour _mono, GameObject _instanceRootUi, ContentReader _contentReader, FileReader _fileReader, MyConfig.Style _style)
        {
            mono_ = _mono;
            contentReader_ = _contentReader;
            fileReader_ = _fileReader;

            uiReference_.pending = _instanceRootUi.transform.Find("Viewer/pending").gameObject;
            uiReference_.panel = _instanceRootUi.transform.Find("Viewer/VideoViewer");
            uiReference_.renderer = _instanceRootUi.transform.Find("Viewer/VideoViewer/renderer");
            uiReference_._mediaPlayer = uiReference_.panel.gameObject.AddComponent<MediaPlayer>();
            uiReference_._displayUGUI = uiReference_.renderer.gameObject.AddComponent<DisplayUGUI>();
            uiReference_.toolbar = _instanceRootUi.transform.Find("Viewer/container/ToolBar/VideoViewer");
            uiReference_.textTime = uiReference_.toolbar.Find("textTime").GetComponent<Text>();

            uiReference_.renderer.GetComponent<Button>().onClick.AddListener(() =>
            {
                onRendererClick();
                uiReference_.toolbar.gameObject.SetActive(true);
            });
            uiReference_.toolbar.Find("btnClose").GetComponent<Button>().onClick.AddListener(() =>
            {
                uiReference_.toolbar.gameObject.SetActive(false);
            });

            uiReference_._mediaPlayer.m_VideoLocation = MediaPlayer.FileLocation.AbsolutePathOrURL;
            uiReference_._displayUGUI._mediaPlayer = uiReference_._mediaPlayer;

            uiReference_.seeker = uiReference_.toolbar.transform.Find("sdSeeker").GetComponent<UnityEngine.UI.Slider>();
            UnityEngine.EventSystems.EventTrigger eventTrigger = uiReference_.seeker.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            // 创建开始拖拽事件
            UnityEngine.EventSystems.EventTrigger.Entry entryBeginDrag = new UnityEngine.EventSystems.EventTrigger.Entry();
            entryBeginDrag.eventID = UnityEngine.EventSystems.EventTriggerType.BeginDrag;
            entryBeginDrag.callback.AddListener((_e) =>
            {
                onSeekerBeginDrag();
            });
            eventTrigger.triggers.Add(entryBeginDrag);

            // 创建结束拖拽事件
            UnityEngine.EventSystems.EventTrigger.Entry entryEndDrag = new UnityEngine.EventSystems.EventTrigger.Entry();
            entryEndDrag.eventID = UnityEngine.EventSystems.EventTriggerType.EndDrag;
            entryEndDrag.callback.AddListener((_e) =>
            {
                onSeekerEndDrag();
            });
            eventTrigger.triggers.Add(entryEndDrag);
            uiReference_.seeker.onValueChanged.AddListener((_value) =>
            {
                onSeekerDrag();
            });

            // 播放和暂停事件
            uiReference_.btnPlay = uiReference_.toolbar.transform.Find("btnPlay").GetComponent<Button>();
            uiReference_.btnPause = uiReference_.toolbar.transform.Find("btnPause").GetComponent<Button>();
            uiReference_.btnPlay.onClick.AddListener(() =>
            {
                play();
            });
            uiReference_.btnPause.onClick.AddListener(() =>
            {
                uiReference_.btnPause.gameObject.SetActive(false);
                uiReference_.btnPlay.gameObject.SetActive(true);
                uiReference_._mediaPlayer.Control.Pause();
            });
            uiReference_.btnPlay.gameObject.SetActive(true);
            uiReference_.btnPause.gameObject.SetActive(false);

            // 音量
            uiReference_.volume = uiReference_.toolbar.transform.Find("sdVolume").GetComponent<UnityEngine.UI.Slider>();
            uiReference_.volume.onValueChanged.AddListener((_value) =>
            {
                volumeAppearTimer_ = 0;
                uiReference_._mediaPlayer.Control.SetVolume(_value);
            });
            uiReference_.volume.gameObject.SetActive(false);
            Button btnVolume = uiReference_.toolbar.transform.Find("btnVolume").GetComponent<UnityEngine.UI.Button>();
            btnVolume.onClick.AddListener(() =>
            {
                if (uiReference_.volume.gameObject.activeSelf)
                {
                    volumeAppearTimer_ = 99f;
                }
                else
                {
                    mono_.StartCoroutine(popupVolume());
                }
            });
            uiReference_.volume.value = 1.0f;

            loopMode_ = _style.toolBar.videoLoop.mode;

            uiReference_.btnLoopNone = uiReference_.toolbar.transform.Find("btnLoopNone").GetComponent<UnityEngine.UI.Button>();
            uiReference_.btnLoopNone.gameObject.SetActive(_style.toolBar.videoLoop.visible && loopMode_ == "none");
            uiReference_.btnLoopSingle = uiReference_.toolbar.transform.Find("btnLoopSingle").GetComponent<UnityEngine.UI.Button>();
            uiReference_.btnLoopSingle.gameObject.SetActive(_style.toolBar.videoLoop.visible && loopMode_ == "single");

            uiReference_.btnLoopNone.onClick.AddListener(() =>
            {
                loopMode_ = "single";
                switchLoopMode();
                uiReference_._mediaPlayer.Control.SetLooping(true);
            });
            uiReference_.btnLoopSingle.onClick.AddListener(() =>
            {
                loopMode_ = "none";
                switchLoopMode();
                uiReference_._mediaPlayer.Control.SetLooping(false);
            });
        }

        /// <summary>
        /// 当实例被打开时
        /// </summary>
        public void HandleInstanceOpened()
        {
            uiReference_.panel.gameObject.SetActive(false);
            uiReference_.toolbar.gameObject.SetActive(false);
        }

        public void OpenEntry(string _source, string _file)
        {
            uiReference_.pending.SetActive(true);
            uiReference_.panel.gameObject.SetActive(false);
            uiReference_.toolbar.gameObject.SetActive(false);

            string url = "";
            if (_source == "assloud://")
            {
                url = Path.Combine(contentReader_.AssetRootPath, contentReader_.ContentUri);
                url = Path.Combine(url, _file);
            }
            else if (_source == "file://")
            {
                url = _file;
            }

            uiReference_.pending.SetActive(false);
            uiReference_.panel.gameObject.SetActive(true);
            uiReference_._mediaPlayer.OpenVideoFromFile(MediaPlayer.FileLocation.AbsolutePathOrURL, url, false);
            play();
        }

        public void CloseEntry()
        {
            stop();
            uiReference_.panel.gameObject.SetActive(false);
            uiReference_.toolbar.gameObject.SetActive(false);
        }

        public bool IsExtensionMatch(string _extension)
        {
            return extensionS_.Contains(_extension.ToLower());
        }

        private void play()
        {
            uiReference_.btnPlay.gameObject.SetActive(false);
            uiReference_.btnPause.gameObject.SetActive(true);
            uiReference_._mediaPlayer.Control.Play();
            if (null != coroutineUpdate_)
            {
                mono_.StopCoroutine(coroutineUpdate_);
            }
            coroutineUpdate_ = mono_.StartCoroutine(update());
        }

        private void stop()
        {
            if (null != coroutineUpdate_)
            {
                mono_.StopCoroutine(coroutineUpdate_);
            }
            if (null != uiReference_._mediaPlayer.Control)
            {
                uiReference_._mediaPlayer.Control.Stop();
                uiReference_._mediaPlayer.Control.Rewind();
            }
            uiReference_.btnPlay.gameObject.SetActive(true);
            uiReference_.btnPause.gameObject.SetActive(false);
        }

        private void onSeekerBeginDrag()
        {
            wasPlayingOnScrub_ = uiReference_._mediaPlayer.Control.IsPlaying();
            if (wasPlayingOnScrub_)
            {
                uiReference_._mediaPlayer.Control.Pause();
            }
            onSeekerDrag();
        }

        private void onSeekerEndDrag()
        {
            if (wasPlayingOnScrub_)
            {
                uiReference_._mediaPlayer.Control.Play();
                wasPlayingOnScrub_ = false;
            }
        }

        private void onSeekerDrag()
        {
            if (uiReference_.seeker.value != videoSeekValue_)
            {
                uiReference_._mediaPlayer.Control.Seek(uiReference_.seeker.value * uiReference_._mediaPlayer.Info.GetDurationMs());
            }
        }

        private IEnumerator update()
        {
            while (true)
            {
                yield return new UnityEngine.WaitForEndOfFrame();
                if (uiReference_._mediaPlayer.Info != null && uiReference_._mediaPlayer.Info.GetDurationMs() > 0f)
                {
                    float time = uiReference_._mediaPlayer.Control.GetCurrentTimeMs();
                    float duration = uiReference_._mediaPlayer.Info.GetDurationMs();
                    float d = Mathf.Clamp(time / duration, 0.0f, 1.0f);
                    videoSeekValue_ = d;
                    uiReference_.seeker.value = d;

                    int leftMS = (int)(uiReference_._mediaPlayer.Info.GetDurationMs() - uiReference_._mediaPlayer.Control.GetCurrentTimeMs());
                    int left = leftMS <= 0 ? 0 : leftMS / 1000 + 1;
                    uiReference_.textTime.text = string.Format("{0:D2}:{1:D2}:{2:D2}", left / (60 * 60), left / 60, left % 60);
                    if (leftMS <= 0)
                    {
                        uiReference_.textTime.text = "00:00:00";
                        if (!uiReference_._mediaPlayer.Control.IsLooping())
                        {
                            break;
                        }
                    }
                }
            }
            stop();
        }


        private IEnumerator popupVolume()
        {
            uiReference_.volume.gameObject.SetActive(true);
            volumeAppearTimer_ = 0;
            while (volumeAppearTimer_ < 2)
            {
                yield return new UnityEngine.WaitForEndOfFrame();
                volumeAppearTimer_ += UnityEngine.Time.deltaTime;
            }
            uiReference_.volume.gameObject.SetActive(false);
        }

        private void switchLoopMode()
        {
            uiReference_.btnLoopNone.gameObject.SetActive(loopMode_ == "none");
            uiReference_.btnLoopSingle.gameObject.SetActive(loopMode_ == "single");
        }
    }
}
