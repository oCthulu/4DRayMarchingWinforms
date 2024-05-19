using System.Runtime.CompilerServices;
using ComputeSharp;

public static class DistanceEstimators{
    static readonly float SQRT_1_2 = Hlsl.Sqrt(1/2f);
    static readonly float SQRT_1_3 = Hlsl.Sqrt(1/3f);
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

    public static float Cone2D(float2 sample)
    {
        float2 t = Hlsl.Abs(sample);
        return (t.X-t.Y)*SQRT_1_2;
    }

    //--------------------------------------------3D--------------------------------------------
    public static float Cube(float3 sample, float3 pos, float3 size)
    {
        float3 t = Hlsl.Max(float3.Zero, Hlsl.Abs(sample - pos) - size);
        return Hlsl.Length(t);
    }

    public static float Torus(float3 sample, float3 pos, float majorRadius, float minorRadius)
    {
        return Circle(Spin3dXZ(sample - pos), new float2(majorRadius, 0), minorRadius);
    }

    public static float Cone(float3 sample){
        return Cone2D(Spin3dXZ(sample));
    }

    public static float Octohedron(float3 sample, float3 pos, float size)
    {
        float3 t = Hlsl.Abs(sample - pos);
        return Hlsl.Dot(t, new float3(SQRT_1_3, SQRT_1_3, SQRT_1_3)) - size;
    }

    public static float Sphere(float3 sample, float3 pos, float radius)
    {
        return Hlsl.Length(sample - pos) - radius;
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

    public static float HyperCone(float4 sample){
        return Cone(Spin4dXW(sample));
    }
    public static float HyperCone(GpuCrossSectionPlane plane, float3 sample){
        return HyperCone(plane.GetPosition(sample));
    }

    public static float HyperOctohedron(float4 sample, float4 pos, float size)
    {
        float4 t = Hlsl.Abs(sample - pos);
        return Hlsl.Dot(t, new float4(0.5f, 0.5f, 0.5f, 0.5f)) - size;
    }
    public static float HyperOctohedron(GpuCrossSectionPlane plane, float3 sample, float4 pos, float size)
    {
        return HyperOctohedron(plane.GetPosition(sample), pos, size);
    }

    public static float HyperSphere(float4 sample, float4 pos, float radius)
    {
        return Hlsl.Length(sample - pos) - radius;
    }
    public static float HyperSphere(GpuCrossSectionPlane plane, float3 sample, float4 pos, float radius)
    {
        return HyperSphere(plane.GetPosition(sample), pos, radius);
    }
}