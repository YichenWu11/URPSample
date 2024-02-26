using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class StreamLodMesh : MonoBehaviour
{
    class SharedAssetBundle
    {
        internal bool isLoading = false;
        internal AssetBundle ab = null;
        internal int refCount = 0;
    }

    static Dictionary<string, SharedAssetBundle> sharedAssets = new Dictionary<string, SharedAssetBundle>();
    public string abName;
    LODGroup lODGroup;
    LOD[] lods;
    bool existLod0 = false;
    Renderer[] rendererLods0;
    Renderer[] rendererLods1;
    Renderer[] rendererLods0_1;
    SharedAssetBundle sab;

    void Start()
    {
        lODGroup = GetComponent<LODGroup>();

        lods = lODGroup.GetLODs();
        rendererLods0 = lods[0].renderers;
        rendererLods1 = lods[1].renderers;
        rendererLods0_1 = new Renderer[rendererLods0.Length + rendererLods1.Length];
        rendererLods0.CopyTo(rendererLods0_1, 0);
        rendererLods1.CopyTo(rendererLods0_1, rendererLods0.Length);
        lods[0].renderers = rendererLods0_1;
        for (int i = 0, len = rendererLods0.Length; i < len; i++)
        {
            rendererLods0[i].GetComponent<MeshFilter>().sharedMesh = rendererLods1[i].GetComponent<MeshFilter>().sharedMesh;
        }
        lODGroup.SetLODs(lods);
        StartCoroutine(loop());
    }

    IEnumerator loop()
    {
        float stepTime = 0.1f;

        while (true)
        {
            yield return new WaitForSeconds(stepTime);
            if (Camera.current == null)
            {
                yield return 0;
                continue;
            }
            float dis = Vector3.Distance(Camera.current.transform.position, transform.position);
            stepTime = Mathf.Clamp(dis * 0.01f, 0.05f, 10);

            if (rendererLods0[0].isVisible)
            {
                if (!existLod0)
                    yield return StartCoroutine(loading());
            }
            else
            {
                if (existLod0)
                {
                    unload();
                }
            }
        }
    }

    private void unload()
    {

        existLod0 = false;
        for (int i = 0, len = rendererLods0.Length; i < len; i++)
        {

            rendererLods0[i].GetComponent<MeshFilter>().sharedMesh = rendererLods1[i].GetComponent<MeshFilter>().sharedMesh;

        }

        sab.refCount--;

        if (sab.refCount == 0)
        {
            sab.ab.Unload(true);
            sharedAssets.Remove(abName);
        }
        lods[0].renderers = rendererLods0_1;
        lODGroup.SetLODs(lods);

    }

    private IEnumerator loading()
    {

        if (sharedAssets.TryGetValue(abName, out sab))
        {

            sab.refCount++;
            //如果已经正在加载 等加载完毕
            while (sab.isLoading)
            {
                yield return 0;
            }
        }
        else
        {
            //如果不存在 也不在加载中 创建一个开始加载
            sab = new SharedAssetBundle() { isLoading = true, refCount = 1 };
            sharedAssets.Add(abName, sab);
            var rq_ab = AssetBundle.LoadFromFileAsync(@"E:\temp\" + abName);
            yield return rq_ab;
            sab.ab = rq_ab.assetBundle;
            sab.isLoading = false;
        }

        // var rq_as = sab.ab.LoadAssetAsync<MeshData>(abName);
        // yield return rq_as;
        // var meshs = (rq_as.asset as MeshData).lod0Meshs;
        // for (int i = 0, len = rendererLods0.Length; i < len; i++)
        // {
        //     rendererLods0[i].GetComponent<MeshFilter>().sharedMesh = meshs[i];
        // }

        // lods[0].renderers = rendererLods0;
        // lODGroup.SetLODs(lods);
        // existLod0 = true;
    }
}