
using System;
using LibMVCS = XTC.FMP.LIB.MVCS;
using XTC.FMP.MOD.MediaCenter.LIB.Bridge;
using XTC.FMP.MOD.MediaCenter.LIB.MVCS;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace XTC.FMP.MOD.MediaCenter.LIB.Unity
{
    /// <summary>
    /// 虚拟视图，用于处理消息订阅
    /// </summary>
    public class DummyView : DummyViewBase
    {
        public DummyView(string _uid) : base(_uid)
        {
        }

        protected override void setup()
        {
            base.setup();
            addSubscriber(MySubject.Inlay, handleInlay);
            addSubscriber(MySubject.Refresh, handleRefresh);
        }

        private void handleInlay(LibMVCS.Model.Status _status, object _data)
        {
            getLogger().Debug("handle inlay of {0}", MyEntryBase.ModuleName);

            string uid = "";
            string style = "";
            GameObject slot = null;
            try
            {
                Dictionary<string, object> data = _data as Dictionary<string, object>;
                uid = (string)data["uid"];
                style = (string)data["style"];
                slot = data["slot"] as GameObject;
            }
            catch (Exception ex)
            {
                getLogger().Exception(ex);
            }
            getLogger().Debug("uid is {0}, style is {1}, slot is {2}", uid, style, slot.ToString());
            runtime.CreateInstanceAsync(uid, style, (_instance) =>
            {
                _instance.rootUI.transform.SetParent(slot.transform);
                _instance.rootUI.transform.localPosition = Vector3.zero;
                _instance.rootUI.transform.localRotation = Quaternion.identity;
                _instance.rootUI.transform.localScale = Vector3.one;
                RectTransform rt = _instance.rootUI.GetComponent<RectTransform>();
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
                _instance.rootUI.SetActive(true);
            });
        }

        private void handleRefresh(LibMVCS.Model.Status _status, object _data)
        {
            getLogger().Debug("handle refresh of {0} with data: {1}", MyEntryBase.ModuleName, JsonConvert.SerializeObject(_data));

            string uid = "";
            string source = "";
            string uri = "";
            try
            {
                Dictionary<string, object> data = _data as Dictionary<string, object>;
                uid = data["uid"] as string;
                source = data["source"] as string;
                uri = data["uri"] as string;
            }
            catch (Exception ex)
            {
                getLogger().Exception(ex);
            }
            runtime.OpenInstanceAsync(uid, source, uri, 0);
        }
    }
}

