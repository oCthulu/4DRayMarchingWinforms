using ComputeSharp;
using static DistanceEstimators;

[ThreadGroupSize(DefaultThreadGroupSizes.XY)]
[GeneratedComputeShaderDescriptor]
public readonly partial struct RenderBase(
    ReadWriteBuffer<float4> posBuffer,
    ReadWriteBuffer<float4> normBuffer,
    float2 camSlope,
    float4x4 camTransform,
    GpuCrossSectionPlane plane,
    int sceneType
) : IComputeShader
{
    public const int SCENE_COUNT = 5;
    public const float DISTANCE_THRESHOLD = 0.001f;
    public const float GRADIENT_DELTA = 0.001f;
    public const float MAX_DISTANCE = 1000;
    public const int MAX_STEPS = 1000;

    public float DistanceEstimator(float3 pos)
    {
        switch(sceneType){
            default:
            case 0: return HyperCube(plane, pos, new float4(0, 0, 0, 0), new float4(0.5f, 0.5f, 0.5f, 0.5f));
            case 1: return HyperTorus(plane, pos, new float4(0, 0, 0, 0), 0.5f, 0.25f, 0.1f);
            case 2: return HyperCone(plane, pos);
            case 3: return HyperOctohedron(plane, pos, new float4(0,0,0,0), 0.5f);
            case 4: return HyperSphere(plane, pos, new float4(0, 0, 0, 0), 0.5f);
        }
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

        for(int i = 0; i < MAX_STEPS; i++)
        {
            float dist = DistanceEstimator(pos);

            if(dist < DISTANCE_THRESHOLD)
            {
                posBuffer[index] = new float4(pos, 1);
                normBuffer[index] = new float4(Hlsl.Normalize(new float3(
                    DistanceEstimator(pos + new float3(GRADIENT_DELTA, 0, 0)) - dist,
                    DistanceEstimator(pos + new float3(0, GRADIENT_DELTA, 0)) - dist,
                    DistanceEstimator(pos + new float3(0, 0, GRADIENT_DELTA)) - dist
                )), 1);
                return;
            }

            if(Hlsl.Dot(pos, pos) > MAX_DISTANCE * MAX_DISTANCE)
            {
                break;
            }

            pos += dir * dist;
        }

        posBuffer[index] = new float4(0, 0, 0, 0);
        normBuffer[index] = new float4(dir, 0);
    }
}

[ThreadGroupSize(DefaultThreadGroupSizes.XY)]
[GeneratedComputeShaderDescriptor]
public readonly partial struct RenderFinal(
    ReadWriteBuffer<float4> posBuffer,
    ReadWriteBuffer<float4> normBuffer,
    IReadWriteNormalizedTexture2D<float4> finalTexture,
    int skyboxType,
    float4 mainLight,
    float4 skyLight,
    float4 ambientLight
) : IComputeShader
{
    public static readonly float3 lightDir = Hlsl.Normalize(new float3(0.5f, 1.5f, -1));

    public void Execute()
    {
        int index = ThreadIds.Y * DispatchSize.X + ThreadIds.X;

        finalTexture[ThreadIds.XY] = new float4(0, 0, 0, 1);

        if(posBuffer[index].W == 0) {
            //sky
            switch(skyboxType){
                case 0: //transparent
                    finalTexture[ThreadIds.XY] = new float4(0, 0, 0, 0);
                    break;
                case 1: //black
                    finalTexture[ThreadIds.XY] = new float4(0, 0, 0, 1);
                    break;
                case 2: //gradient
                    float dot = Hlsl.Dot(normBuffer[index].XYZ, float3.UnitY);
                    float3 skyColor = Hlsl.Lerp(new float3(0.75f, 0.85f, 0.9f), new float3(0.2f, 0.4f, 0.8f), Hlsl.Max(dot, 0));
                    float3 groundColor = Hlsl.Lerp(new float3(0.6f, 0.6f, 0.6f), new float3(0.3f, 0.3f, 0.3f), Hlsl.Max(-dot, 0));
                    //for some reason, its only set correctly with BGR insetad of RGB
                    finalTexture[ThreadIds.XY].RGB = Hlsl.Lerp(skyColor, groundColor, Hlsl.Saturate((dot-0.05f)/(-0.1f)));
                    break;
            }

            return;
        }

        finalTexture[ThreadIds.XY].BGR = (
            Hlsl.Saturate(mainLight.W * Hlsl.Dot(normBuffer[index].XYZ, lightDir) * mainLight.RGB) + 
            Hlsl.Saturate(skyLight.W * (Hlsl.Dot(normBuffer[index].XYZ, float3.UnitY) * 0.5f + 0.5f) * mainLight.RGB) + 
            Hlsl.Saturate(ambientLight.W * mainLight.RGB)
        ) / (mainLight.W + skyLight.W + ambientLight.W);
        
        finalTexture[ThreadIds.XY].A = 1;
    }
}

[ThreadGroupSize(DefaultThreadGroupSizes.XY)]
[GeneratedComputeShaderDescriptor]
public readonly partial struct ConvertToBgra32 (
    IReadWriteNormalizedTexture2D<float4> texture
): IComputeShader{
    public void Execute()
    {
        texture[ThreadIds.XY] = texture[ThreadIds.XY].BGRA;
    }
}