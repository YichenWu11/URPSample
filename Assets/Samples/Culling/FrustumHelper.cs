using UnityEngine;
using UnityEngine.Rendering;

/*
 *  Plane : Ax + By + Cz + D = 0
 *  Vector4(A, B, C, D)
 */

public static class FrustumHelper
{
    //获取视锥体的六个平面
    public static Vector4[] GetFrustumPlane(Camera camera)
    {
        Vector4[] planes = new Vector4[6];
        Transform transform = camera.transform;
        Vector3 cameraPosition = transform.position;
        Vector3[] points = new Vector3[4];

        camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1),
            camera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono,
            points);

        /*
         *  CalculateFrustumCorners 得到的顶点绕序如下
         *   1    2
         *
         *   0    3 
         */

        // 转到世界空间下
        for (int i = 0; i < 4; i++)
            points[i] = camera.transform.TransformVector(points[i]) + camera.transform.position;

        // 顺时针绕序
        planes[0] = GetPlane(cameraPosition, points[0], points[1]); // left
        planes[1] = GetPlane(cameraPosition, points[2], points[3]); // right
        planes[2] = GetPlane(cameraPosition, points[3], points[0]); // bottom
        planes[3] = GetPlane(cameraPosition, points[1], points[2]); // up
        planes[4] = GetPlane(-transform.forward, transform.position + transform.forward * camera.nearClipPlane); // near
        planes[5] = GetPlane(transform.forward, transform.position + transform.forward * camera.farClipPlane); // far
        return planes;
    }

    public static Vector4 GetPlane(Vector3 normal, Vector3 point)
    {
        return new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(normal, point));
    }

    public static Vector4 GetPlane(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
        return GetPlane(normal, a);
    }
}