using System.Numerics;

public static class Util{
    public enum Axis{
        X,
        Y,
        Z,
        W
    }

    public static float DegToRad(float deg) => deg * (MathF.PI / 180);

    public static Matrix4x4 CreateRotationMatrix(Axis a, Axis b, float angle){
        Matrix4x4 m = Matrix4x4.Identity;
        m[(int)a, (int)a] = MathF.Cos(angle);
        m[(int)a, (int)b] = MathF.Sin(angle);
        m[(int)b, (int)a] = -MathF.Sin(angle);
        m[(int)b, (int)b] = MathF.Cos(angle);
        return m;
    }
}