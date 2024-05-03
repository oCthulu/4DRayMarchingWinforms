using ComputeSharp;

public static class DistanceEstimators{
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
}