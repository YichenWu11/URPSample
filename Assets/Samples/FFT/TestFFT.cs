using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

//[ExecuteInEditMode]
public class TestFFT : MonoBehaviour
{
    public Texture2D photo;
    public ComputeShader fft;

    private FFT _fft;

    void OnEnable()
    {
        _fft = new FFT(fft);
    }

    private void OnDisable()
    {
        _fft.Dispose();
    }

    // Update is called once per frame
    void Update()
    {
        _fft.Init(photo);
        Profiler.BeginSample("DFT");
        RenderTexture result = _fft.IDFT();
        Profiler.EndSample();
        GetComponent<Renderer>().sharedMaterial.mainTexture = result;
    }
}