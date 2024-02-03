using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TAA
{
    internal static class Jitter
    {
        static internal float GetHalton(int index, int radix)
        {
            float result = 0.0f;
            float fraction = 1.0f / radix;
            while (index > 0)
            {
                result += (index % radix) * fraction;

                index /= radix;
                fraction /= radix;
            }

            return result;
        }

        // get [-0.5, 0.5] jitter vector2
        static internal Vector2 CalculateJitter(int frameIndex)
        {
            float jitterX = GetHalton((frameIndex & 1023) + 1, 2) - 0.5f;
            float jitterY = GetHalton((frameIndex & 1023) + 1, 3) - 0.5f;

            return new Vector2(jitterX, jitterY);
        }

        static internal Matrix4x4 CalculateJitterProjectionMatrix(ref CameraData cameraData, float jitterScale = 1.0f)
        {
            Matrix4x4 mat = cameraData.GetProjectionMatrix();

            int taaFrameIndex = Time.frameCount;

            float actualWidth = cameraData.camera.pixelWidth;
            float actualHeight = cameraData.camera.pixelHeight;

            Vector2 jitter = CalculateJitter(taaFrameIndex) * jitterScale;

            mat.m02 += jitter.x * (2.0f / actualWidth);
            mat.m12 += jitter.y * (2.0f / actualHeight);

            return mat;
        }
    }
}