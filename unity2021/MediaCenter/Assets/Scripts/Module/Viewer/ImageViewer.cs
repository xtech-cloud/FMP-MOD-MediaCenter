using System;
using UnityEngine;
using UnityEngine.UI;

namespace XTC.FMP.MOD.MediaCenter.LIB.Unity
{
    public class ImageViewer
    {
        public class UiReference
        {
            /// <summary>
            /// 图片浏览器的面板
            /// </summary>
            public Transform panel;
            /// <summary>
            /// 图片浏览器的渲染元素
            /// </summary>
            public RawImage renderer;
            /// <summary>
            /// 图片浏览器的工具栏
            /// </summary>
            public Transform toolbar;

            public Button btnZoomOut;
            public Button btnZoomIn;
        }
        public Action onRendererClick;

        private UiReference uiReference_ = new UiReference();
        private ContentReader contentReader_;
        private Vector2 originSizeDelta_;
        private float scale_;

        public void Setup(GameObject _instanceRootUi, ContentReader _contentReader)
        {
            contentReader_ = _contentReader;

            uiReference_.panel = _instanceRootUi.transform.Find("Viewer/ImageViewer");
            uiReference_.renderer = _instanceRootUi.transform.Find("Viewer/ImageViewer/Viewport/Content").GetComponent<RawImage>();
            uiReference_.toolbar = _instanceRootUi.transform.Find("Viewer/container/ToolBar/ImageViewer");

            uiReference_.renderer.GetComponent<Button>().onClick.AddListener(() =>
            {
                onRendererClick();
                uiReference_.toolbar.gameObject.SetActive(true);
            });
            uiReference_.toolbar.Find("btnClose").GetComponent<Button>().onClick.AddListener(() =>
            {
                uiReference_.toolbar.gameObject.SetActive(false);
            });

            uiReference_.btnZoomIn = uiReference_.toolbar.Find("btnZoomIn").GetComponent<Button>();
            uiReference_.btnZoomIn.onClick.AddListener(() =>
            {
                scale_ += 0.2f;
                uiReference_.renderer.rectTransform.sizeDelta = originSizeDelta_ * scale_;
            });
            uiReference_.btnZoomOut = uiReference_.toolbar.Find("btnZoomOut").GetComponent<Button>();
            uiReference_.btnZoomOut.onClick.AddListener(() =>
            {
                scale_ -= 0.2f;
                if (scale_ < 1)
                    scale_ = 1;
                uiReference_.renderer.rectTransform.sizeDelta = originSizeDelta_ * scale_;
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

        public void OpenEntry(string _file)
        {
            uiReference_.panel.gameObject.SetActive(true);
            uiReference_.toolbar.gameObject.SetActive(false);
            contentReader_.LoadTexture(_file, (_texture) =>
            {
                uiReference_.renderer.texture = _texture;
                uiReference_.renderer.SetNativeSize();
                fitImage();
            }, () =>
            {

            });
        }

        public bool IsExtensionMatch(string _extension)
        {
            return _extension.ToLower() == ".jpg";
        }

        private void fitImage()
        {
            var rtParent = uiReference_.renderer.transform.parent.GetComponent<RectTransform>();
            // 容器的宽高比
            float ratioParent = rtParent.rect.size.x / rtParent.rect.size.y;
            var rtImage = uiReference_.renderer.rectTransform;
            //图片和容器的宽度差值
            float differenceX = rtImage.rect.size.x - rtParent.rect.size.x;
            //图片和容器的高度差值
            float differenceY = rtImage.rect.size.y - rtParent.rect.size.y;
            // 将高度的差值换算成和宽度的比例尺一致
            differenceY *= ratioParent;
            float fitWidth = rtImage.rect.width;
            float fitHeight = rtImage.rect.height;
            if (differenceX > 0 || differenceY > 0)
            {
                if (differenceX > differenceY)
                {
                    fitWidth = rtParent.rect.size.x;
                    fitHeight = rtImage.rect.size.y / rtImage.rect.size.x * fitWidth;
                }
                else
                {
                    fitHeight = rtParent.rect.size.y;
                    fitWidth = rtImage.rect.size.x / rtImage.rect.size.y * fitHeight;
                }
            }
            originSizeDelta_ = new Vector2(fitWidth, fitHeight);
            rtImage.sizeDelta = originSizeDelta_;
            scale_ = 1.0f;
        }
    }
}
