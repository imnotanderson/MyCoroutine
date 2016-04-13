using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;


public static class MyCoroutine
{
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
   class MyCoroutineMono : MonoBehaviour
    {
        public static MyCoroutineMono instance
        {
            get
            {
                if (m == null)
                {
                    var go = new GameObject("[MY_COROUTINE]");
                    m = go.AddComponent<MyCoroutineMono>();
                    DontDestroyOnLoad(go);
                }
                return m;
            }
        }
        static MyCoroutineMono m;

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
                if (go == null || go.ToString()=="null")
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
            }

            bool CheckGo()
            {
                return go && go.activeInHierarchy;
            }
        }
        List<Data> dataList = new List<Data>();

        void Update()
        {
            for (int i = 0; i < dataList.Count; i++)
            {
                var data = dataList[i];
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
                enabled = false;
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
            dataList.Add(da);
            enabled = true;
            return id;
        }

        public void StopMyCoroutineById(int id)
        {
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
            for (int i = 0; i < dataList.Count; i++)
            {
                var data = dataList[i];
                if (data.go == go)
                {
                    data.die = true;
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
