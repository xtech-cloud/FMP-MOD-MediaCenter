
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
        }

        private void handleInlay(LibMVCS.Model.Status _status, object _data)
        {
            getLogger().Debug("handle inlay  with data: {1}", MyEntryBase.ModuleName, JsonConvert.SerializeObject(_data));

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
            runtime.CreateInstanceAsync(uid, style, (_instance) =>
            {
                _instance.rootUI.transform.SetParent(slot.transform);
                _instance.rootUI.transform.localPosition = Vector3.zero;
                _instance.rootUI.transform.localRotation = Quaternion.identity;
                _instance.rootUI.transform.localScale = Vector3.one;
                RectTransform rt = _instance.rootUI.GetComponent<RectTransform>();
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
            });
        }
    }
}

