using System.Numerics;

public struct CpuCrossSectionPlane{
    public static CpuCrossSectionPlane identity = new CpuCrossSectionPlane(
        new Matrix4x4(
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1
        ),
        new Vector4(0, 0, 0, 0)
    );

    public Matrix4x4 plane;
    public Vector4 offset;

    public CpuCrossSectionPlane(Matrix4x4 plane, Vector4 offset)
    {
        this.plane = plane;
        this.offset = offset;
    }

    public static CpuCrossSectionPlane operator *(CpuCrossSectionPlane a, Matrix4x4 b)
    {
        return new CpuCrossSectionPlane(
            a.plane * b,
            Vector4.Transform(a.offset, b)
        );
    }

    public static CpuCrossSectionPlane operator +(CpuCrossSectionPlane a, Vector4 b)
    {
        return new CpuCrossSectionPlane(
            a.plane,
            a.offset + b
        );
    }
}

public struct GpuCrossSectionPlane{
    public float4x4 plane;
    public float4 offset;

    public GpuCrossSectionPlane(float4x4 plane, float4 offset)
    {
        this.plane = plane;
        this.offset = offset;
    }

    public float4 GetPosition(float3 sample)
    {
        return plane * new float4(sample, 0) + offset;
    }

    public static implicit operator GpuCrossSectionPlane(CpuCrossSectionPlane plane)
    {
        return new GpuCrossSectionPlane(
            Matrix4x4.Transpose(plane.plane),
            plane.offset
        );
    }
}