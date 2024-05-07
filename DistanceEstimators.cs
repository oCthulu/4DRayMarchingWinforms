using ComputeSharp;

public static class DistanceEstimators{
    //-------------------------------------Transformations-------------------------------------
    public static float2 Spin3dXZ(float3 sample){
        return new float2(Hlsl.Length(sample.XZ), sample.Y);
    }

    public static float3 Spin4dXW(float4 sample){
        return new float3(Hlsl.Length(sample.XW), sample.YZ);
    }

    //--------------------------------------------2D--------------------------------------------
    public static float Circle(float2 sample, float2 pos, float radius)
    {
        return Hlsl.Length(sample - pos) - radius;
    }
    //--------------------------------------------3D--------------------------------------------
    public static float Cube(float3 sample, float3 pos, float3 size)
    {
        // float3 t = new float3(
        //     Hlsl.Max(0, Hlsl.Abs(sample.X) - 0.5f),
        //     Hlsl.Max(0, Hlsl.Abs(sample.Y) - 0.5f),
        //     Hlsl.Max(0, Hlsl.Abs(sample.Z) - 0.5f)
        // );
        // return Hlsl.Sqrt(
        //     t.X * t.X +
        //     t.Y * t.Y +
        //     t.Z * t.Z
        // );
        float3 t = Hlsl.Max(float3.Zero, Hlsl.Abs(sample - pos) - size);
        return Hlsl.Length(t);
    }

    public static float Torus(float3 sample, float3 pos, float majorRadius, float minorRadius)
    {
        return Circle(Spin3dXZ(sample - pos), new float2(majorRadius, 0), minorRadius);
    }

    //--------------------------------------------4D--------------------------------------------
    public static float HyperCube(float4 sample, float4 pos, float4 size)
    {
        float4 t = Hlsl.Max(float4.Zero, Hlsl.Abs(sample - pos) - size);
        return Hlsl.Length(t);
    }
    public static float HyperCube(GpuCrossSectionPlane plane, float3 sample, float4 pos, float4 size)
    {
        return HyperCube(plane.GetPosition(sample), pos, size);
    }

    public static float HyperTorus(float4 sample, float4 pos, float hyperRadius, float majorRadius, float minorRadius)
    {
        return Torus(Spin4dXW(sample - pos), new float3(hyperRadius, 0, 0), majorRadius, minorRadius);
    }
    public static float HyperTorus(GpuCrossSectionPlane plane, float3 sample, float4 pos, float hyperRadius, float majorRadius, float minorRadius)
    {
        return HyperTorus(plane.GetPosition(sample), pos, hyperRadius, majorRadius, minorRadius);
    }
}