using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class GraphicsTest : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public Material instanceMaterial;
    public int subMeshIndex = 0;
    public int instanceCount = 5;

    private Matrix4x4[] m_Object2WorldMats = new Matrix4x4[5];

    private ComputeBuffer m_PositionBuffer;
    private ComputeBuffer m_ArgsBuffer;
    private uint[] m_Args = new uint[5] { 0, 0, 0, 0, 0 };

    void Start()
    {
        // for (int i = 0; i < m_Object2WorldMats.Length; ++i)
        // {
        //     m_Object2WorldMats[i] =
        //         Matrix4x4.Translate(new Vector3(i * 1.0f, i * 1.0f, i * 1.0f));
        // }
        //
        // m_ArgsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
        // UpdateBuffer();
    }

    void Update()
    {
        // TestDrawMesh();
        // TestDrawMeshInstanced();
        // TestDrawMeshInstancedIndirect();
    }

    void TestDrawMesh()
    {
        Graphics.DrawMesh(mesh, Vector3.zero, Quaternion.identity, material, 0);
    }

    void TestDrawMeshInstanced()
    {
        Graphics.DrawMeshInstanced(mesh, subMeshIndex, material, m_Object2WorldMats);
    }

    void UpdateBuffer()
    {
        // Positions
        if (m_PositionBuffer != null)
            m_PositionBuffer.Release();
        m_PositionBuffer = new ComputeBuffer(instanceCount, 16);
        Vector4[] positions = new Vector4[instanceCount];
        for (int i = 0; i < instanceCount; i++)
        {
            float angle = Random.Range(0.0f, Mathf.PI * 2.0f);
            float distance = Random.Range(20.0f, 100.0f);
            float height = Random.Range(-2.0f, 2.0f);
            float size = Random.Range(0.05f, 0.25f);
            positions[i] = new Vector4(Mathf.Sin(angle) * distance, height, Mathf.Cos(angle) * distance, size);
        }

        m_PositionBuffer.SetData(positions);
        instanceMaterial.SetBuffer("_PositionBuffer", m_PositionBuffer);

        // Indirect args
        if (mesh != null)
        {
            m_Args[0] = (uint)mesh.GetIndexCount(subMeshIndex);
            m_Args[1] = (uint)instanceCount;
            m_Args[2] = (uint)mesh.GetIndexStart(subMeshIndex);
            m_Args[3] = (uint)mesh.GetBaseVertex(subMeshIndex);
        }
        else
        {
            m_Args[0] = m_Args[1] = m_Args[2] = m_Args[3] = 0;
        }

        m_ArgsBuffer.SetData(m_Args);
    }

    void TestDrawMeshInstancedIndirect()
    {
        Graphics.DrawMeshInstancedIndirect(mesh, subMeshIndex, instanceMaterial,
            new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), m_ArgsBuffer);
    }

    void OnDisable()
    {
        m_PositionBuffer?.Release();
        m_PositionBuffer = null;

        m_ArgsBuffer?.Release();
        m_ArgsBuffer = null;
    }
}