using ComputeSharp;
using static DistanceEstimators;

[ThreadGroupSize(DefaultThreadGroupSizes.XY)]
[GeneratedComputeShaderDescriptor]
public readonly partial struct RenderBase(
    ReadWriteBuffer<float4> posBuffer,
    ReadWriteBuffer<float4> normBuffer,
    float2 camSlope,
    float4x4 camTransform,
    GpuCrossSectionPlane plane
) : IComputeShader
{
    public const float DISTANCE_THRESHOLD = 0.001f;

    public float DistanceEstimator(float3 pos)
    {
        //return HyperCube(plane, pos, new float4(0, 0, 0, 0), new float4(0.5f, 0.5f, 0.5f, 0.5f));
        return HyperTorus(plane, pos, new float4(0, 0, 0, 0), 0.5f, 0.25f, 0.1f);
    }
    
    public void Execute()
    {
        // Get the index of the current thread
        int index = ThreadIds.Y * DispatchSize.X + ThreadIds.X;
        
        //ray marching
        float3 pos = new float3(0, 0, 0);
        float3 dir = Hlsl.Normalize(new float3(((float2)ThreadIds.XY / DispatchSize.XY - new float2(0.5f, 0.5f)) * new Float2(2, -2) * camSlope, 1));

        pos = Hlsl.Mul(camTransform, new float4(pos, 1)).XYZ;
        dir = Hlsl.Mul(camTransform, new float4(dir, 0)).XYZ;

        for(int i = 0; i < 100; i++)
        {
            float dist = DistanceEstimator(pos);

            if(dist < DISTANCE_THRESHOLD)
            {
                posBuffer[index] = new float4(pos, 1);
                normBuffer[index] = new float4(Hlsl.Normalize(new float3(
                    DistanceEstimator(pos + new float3(DISTANCE_THRESHOLD, 0, 0)) - dist,
                    DistanceEstimator(pos + new float3(0, DISTANCE_THRESHOLD, 0)) - dist,
                    DistanceEstimator(pos + new float3(0, 0, DISTANCE_THRESHOLD)) - dist
                )), 1);
                return;
            }

            pos += dir * dist;
        }

        posBuffer[index] = new float4(0, 0, 0, 0);
    }
}

[ThreadGroupSize(DefaultThreadGroupSizes.XY)]
[GeneratedComputeShaderDescriptor]
public readonly partial struct RenderFinal(
    ReadWriteBuffer<float4> posBuffer,
    ReadWriteBuffer<float4> normBuffer,
    IReadWriteNormalizedTexture2D<float4> finalTexture
) : IComputeShader
{
    public static readonly float3 lightDir = Hlsl.Normalize(new float3(0.5f, 1.5f, -1));

    public void Execute()
    {
        int index = ThreadIds.Y * DispatchSize.X + ThreadIds.X;

        finalTexture[ThreadIds.XY] = new float4(0, 0, 0, 1);
        if(posBuffer[index].W == 0) return;

        finalTexture[ThreadIds.XY].RGB = Hlsl.Dot(normBuffer[index].XYZ, lightDir);
        finalTexture[ThreadIds.XY].A = 1;
    }
}