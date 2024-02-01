using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class DrawObjects : MonoBehaviour
{
    public enum CullMode
    {
        None = 0, // 不剔除
        Conservative, // 保守剔除
        Radical // 激进剔除
    }

    public Mesh mesh;
    public int subMeshIndex = 0;
    public Material instanceMaterial;
    public Camera mainCamera;

    public CullMode cullMode = CullMode.None;
    public ComputeShader cullShader;

    private int m_InstanceCount = 5 * 5;
    private ComputeBuffer m_PositionBuffer; // float4(x, y, z, scale)
    private ComputeBuffer m_ArgsBuffer;
    private uint[] m_Args = new uint[5] { 0, 0, 0, 0, 0 };

    private ComputeBuffer m_CullResult;
    private int m_Kernel;

    public void GenerateBuffers()
    {
        if (m_ArgsBuffer != null && m_PositionBuffer != null)
            return;

        m_ArgsBuffer =
            new ComputeBuffer(m_Args.Length, sizeof(uint), ComputeBufferType.IndirectArguments);

        m_PositionBuffer = new ComputeBuffer(m_InstanceCount, sizeof(float) * 4);
        m_CullResult = new ComputeBuffer(m_InstanceCount, sizeof(float) * 4, ComputeBufferType.Append);

        Vector4[] positions = new Vector4[m_InstanceCount];
        const int row = 5;
        const int col = 5;
        const int offset = -32;
        const int stride = 16;
        for (int i = 0; i < row; ++i)
        {
            for (int j = 0; j < col; ++j)
            {
                positions[i * row + j] = new Vector4(i * stride + offset, 5, j * stride + offset, 5);
            }
        }

        m_PositionBuffer.SetData(positions);
        instanceMaterial.SetBuffer("_PositionBuffer", m_CullResult);

        // Indirect args
        if (mesh != null)
        {
            m_Args[0] = (uint)mesh.GetIndexCount(subMeshIndex);
            m_Args[1] = (uint)m_InstanceCount;
            m_Args[2] = (uint)mesh.GetIndexStart(subMeshIndex);
            m_Args[3] = (uint)mesh.GetBaseVertex(subMeshIndex);
        }
        else
        {
            m_Args[0] = m_Args[1] = m_Args[2] = m_Args[3] = 0;
        }

        m_ArgsBuffer.SetData(m_Args);
    }

    private void OnEnable()
    {
        GenerateBuffers();
    }

    private void OnDisable()
    {
        m_PositionBuffer?.Release();
        m_PositionBuffer = null;
        m_ArgsBuffer?.Release();
        m_ArgsBuffer = null;

        m_CullResult?.Release();
        m_CullResult = null;
    }

    private void Update()
    {
        FrustumCulling();

        Graphics.DrawMeshInstancedIndirect(
            mesh,
            subMeshIndex,
            instanceMaterial,
            new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)),
            m_ArgsBuffer);
    }

    private void FrustumCulling()
    {
        if (cullShader == null)
            return;

        switch (cullMode)
        {
            case CullMode.None:
            {
                cullShader.EnableKeyword("None_Cull");
                cullShader.DisableKeyword("Conservative_Cull");
                cullShader.DisableKeyword("Racial_Cull");
            }
                break;
            case CullMode.Conservative:
            {
                cullShader.EnableKeyword("Conservative_Cull");
                cullShader.DisableKeyword("None_Cull");
                cullShader.DisableKeyword("Racial_Cull");
            }
                break;
            case CullMode.Radical:
            {
                cullShader.EnableKeyword("Racial_Cull");
                cullShader.DisableKeyword("None_Cull");
                cullShader.DisableKeyword("Conservative_Cull");
            }
                break;
        }

        m_Kernel = cullShader.FindKernel("ViewFrustumCulling");

        Vector4[] planes = FrustumHelper.GetFrustumPlane(mainCamera);
        Vector4[] bounds = new Vector4[2];
        bounds[0] = mesh.bounds.min;
        bounds[1] = mesh.bounds.max;

        cullShader.SetBuffer(m_Kernel, "_Input", m_PositionBuffer);
        m_CullResult.SetCounterValue(0);
        cullShader.SetBuffer(m_Kernel, "_CullResult", m_CullResult);
        cullShader.SetInt("_InstanceCount", m_InstanceCount);
        cullShader.SetVectorArray("_Planes", planes);
        cullShader.SetVectorArray("_Bounds", bounds);

        cullShader.Dispatch(m_Kernel, 1 + (m_InstanceCount / 64), 1, 1);

        // 拷贝剔除后的 InstanceCount 到 ArgsBuffer 中
        ComputeBuffer.CopyCount(m_CullResult, m_ArgsBuffer, sizeof(uint));
    }
}