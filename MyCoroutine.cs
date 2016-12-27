using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;

public static class MyCoroutine
{
    public static void Init(int maxFrameIntervalMs) {
        MAX_FRAME_INTERVAL_MS = maxFrameIntervalMs;
        MyCoroutineMono.Init();
    }

    public static int MAX_FRAME_INTERVAL_MS = 0;
    static IEnumerator DelayAction(float delayTime, Action action)
    {
        yield return delayTime;
        action();
    }

    public static int StartDelayAction(this GameObject go, float delayTime, Action action)
    {
        return go.StartContinueCoroutine(DelayAction(delayTime, action));
    }

    public static int StartContinueCoroutine(this GameObject go, IEnumerator ie)
    {
        if (GameMain.IsPlaying == false) return 0;
        return MyCoroutineMono.instance.StartMyCoroutine(go, ie);
    }

    public static void StopContinueCoroutineById(this GameObject go, int id)
    {
        if (GameMain.IsPlaying == false) return;
        MyCoroutineMono.instance.StopMyCoroutineById(id);
    }

    public static void StopContinueCoroutineByGo(this GameObject go)
    {
        if (GameMain.IsPlaying == false) return;
        MyCoroutineMono.instance.StopMyCoroutineByGo(go);
    }

    #region mono
    static FieldInfo _WaitForSencondField = null;
    static FieldInfo WaitForSencondField
    {
        get
        {
            if (_WaitForSencondField == null)
            {
                _WaitForSencondField = typeof(WaitForSeconds).GetField("m_Seconds", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            return _WaitForSencondField;
        }
    }

    class MyCoroutineMono : MonoBehaviour
    {
        public static MyCoroutineMono instance
        {
            get
            {
                if (isInit == false)
                {
                    Init();
                }
                return m;
            }
        }
        static MyCoroutineMono m;
        static bool isInit;
        static object mLock = new object();

        public static void Init() {
            if (!GameMain.IsPlaying)
            {
                return;
            }
            lock (mLock)
            {
                if (isInit == true) return;
                var go = new GameObject("[MY_COROUTINE]");
                m = go.AddComponent<MyCoroutineMono>();
                isInit = true;
                DontDestroyOnLoad(go);
            }
        }
        public bool printLog = false;
        Stopwatch sw = new Stopwatch();
        int coroutineId = 0;
        int coroutineCount = 0;

        struct Data
        {
            public bool die;
            public GameObject go;
            public IEnumerator ie;
            public float delayTime;
            public int id;

            public void Upt()
            {
                if (go == null || go.ToString() == "null")
                {
                    die = true;
                    return;
                }
                if (CheckGo() == false)
                {
                    return;
                }
                if (delayTime >= 0)
                {
                    delayTime -= Time.deltaTime;
                }
                if (delayTime >= 0) return;
                if (ie.MoveNext() == false)
                {
                    die = true;
                    return;
                }
                object obj = ie.Current;
                if (obj == null) return;
                if (obj is float)
                {
                    delayTime = (float)obj;
                }
                else if (obj is int)
                {
                    delayTime = (int)obj;
                }
                else if (obj is WaitForSeconds)
                {
                    delayTime = (float)WaitForSencondField.GetValue(obj);
                }
            }

            bool CheckGo()
            {
                return go && go.activeInHierarchy;
            }
        }
        List<Data> dataBufferList = new List<Data>();
        List<Data> dataList = new List<Data>();
        bool checkBuffer = false;
        bool checkData = false;

        void Update()
        {
            if (checkBuffer == false && checkData == false) return;
            if (dataBufferList.Count > 0)
            {
                lock (dataBufferList)
                {
                    dataList.AddRange(dataBufferList);
                    dataBufferList.Clear();
                    checkBuffer = false;
                }
                checkData = true;
            }
            sw.Reset();
            sw.Start();

            for (int i = 0; i < dataList.Count; i++)
            {
                if (MAX_FRAME_INTERVAL_MS>0 && sw.ElapsedMilliseconds > MAX_FRAME_INTERVAL_MS)
                {
                    if (printLog)
                    {
                        UnityEngine.Debug.Log("=========>sw.ElapsedMilliseconds > MAX_FRAME_INTERVAL_MS: loopCount:" + i + "/" + dataList.Count);
                    }
                    break;
                }
                var data = dataList[i];
                if (data.die)
                {
                    continue;
                }
                data.Upt();
                dataList[i] = data;
            }
            for (int i = 0; i < dataList.Count; i++)
            {
                if (dataList[i].die)
                {
                    dataList.RemoveAt(i);
                    i--;
                }
            }

            if (dataList.Count == 0)
            {
                checkData = false;
            }
            coroutineCount = dataList.Count;
        }

        public int StartMyCoroutine(GameObject go, IEnumerator ie)
        {
            int id = getId();
            Data da = new Data();
            da.ie = ie;
            da.go = go;
            da.id = id;
            lock (dataBufferList)
            {
                dataBufferList.Add(da);
                checkBuffer = true;
            }
            return id;
        }

        public void StopMyCoroutineById(int id)
        {
            lock (dataBufferList)
            {
                dataList.AddRange(dataBufferList);
                dataBufferList.Clear();
            }
            for (int i = 0; i < dataList.Count; i++)
            {
                var data = dataList[i];
                if (data.id == id)
                {
                    data.die = true;
                    dataList[i] = data;
                    return;
                }
            }
        }

        public void StopMyCoroutineByGo(GameObject go)
        {
            lock (dataBufferList)
            {
                dataList.AddRange(dataBufferList);
                dataBufferList.Clear();
            }
            for (int i = 0; i < dataList.Count; i++)
            {
                var data = dataList[i];
                if (data.go == go)
                {
                    data.die = true;
                    dataList[i] = data;
                }
            }
        }

        int getId()
        {
            return coroutineId++;
        }

        public int GetCoroutineCount()
        {
            return coroutineCount;
        }
        #endregion
    }
}
