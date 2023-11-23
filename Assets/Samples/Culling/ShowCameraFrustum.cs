using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class ShowCameraFrustum : MonoBehaviour
{
    public Camera cam;
    public bool isShow = true;

    Vector3[] m_nearCorners = new Vector3[4];
    Vector3[] m_farCorners = new Vector3[4];

    private void OnValidate()
    {
        if (cam == null)
            cam = GetComponent<Camera>();

        CalculateCorners();
    }

    private void Update()
    {
        if (!isShow)
            return;
        // CalculateCorners();
        DrawFrustum(m_nearCorners, m_farCorners, Color.red);
    }

    private void CalculateCorners()
    {
        cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono,
            m_nearCorners);
        cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.farClipPlane, Camera.MonoOrStereoscopicEye.Mono,
            m_farCorners);

        for (int i = 0; i < 4; i++)
        {
            m_nearCorners[i] = cam.transform.TransformVector(m_nearCorners[i]) + cam.transform.position;
            m_farCorners[i] = cam.transform.TransformVector(m_farCorners[i]) + cam.transform.position;
        }
    }

    private void DrawFrustum(Vector3[] nearCorners, Vector3[] farCorners, Color color)
    {
        for (int i = 0; i < 4; i++)
            Debug.DrawLine(nearCorners[i], farCorners[i], color);

        Debug.DrawLine(farCorners[0], farCorners[1], color);
        Debug.DrawLine(farCorners[0], farCorners[3], color);
        Debug.DrawLine(farCorners[2], farCorners[1], color);
        Debug.DrawLine(farCorners[2], farCorners[3], color);
        Debug.DrawLine(nearCorners[0], nearCorners[1], color);
        Debug.DrawLine(nearCorners[0], nearCorners[3], color);
        Debug.DrawLine(nearCorners[2], nearCorners[1], color);
        Debug.DrawLine(nearCorners[2], nearCorners[3], color);
    }
}