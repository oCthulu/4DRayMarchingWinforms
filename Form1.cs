using System.Diagnostics;
using System.Drawing.Imaging;
using System.Numerics;
using ComputeSharp;

using static Util;

namespace _4DRayMarchingWinforms;

public partial class Form1 : Form
{
    public float AspectRatio => (float)ClientSize.Width / ClientSize.Height;

    public float fieldOfView = 90;

    private Vector3 cameraOffset = new Vector3(0, 0, -2);
    private Vector3 cameraRotation = new Vector3(45, -45, 0);

    private Vector3 planeRotation = new Vector3(0, 0, 0);

    private Point mousePosPrev;

    ReadWriteBuffer<float4> posBuffer;
    ReadWriteBuffer<float4> normBuffer;

    ReadWriteTexture2D<Rgba32, float4> finalTextureBuffer;
    Bitmap finalTexture;

    Stopwatch stopwatch = new Stopwatch();

    public Form1()
    {
        posBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<float4>(ClientSize.Width * ClientSize.Height);
        normBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<float4>(ClientSize.Width * ClientSize.Height);

        finalTextureBuffer = GraphicsDevice.GetDefault().AllocateReadWriteTexture2D<Rgba32, float4>(ClientSize.Width, ClientSize.Height);
        finalTexture = new Bitmap(ClientSize.Width, ClientSize.Height);

        InitializeComponent();
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Console.WriteLine($"Other: {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.Restart();

        base.OnPaint(e);
        Console.WriteLine($"base.OnPaint(): {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.Restart();

        //update camera rotation
        if (MouseButtons.HasFlag(MouseButtons.Left))
        {
            cameraRotation.X += (MousePosition.Y - mousePosPrev.Y) * 0.5f;
            cameraRotation.Y += (MousePosition.X - mousePosPrev.X) * 0.5f;
        }

        if (MouseButtons.HasFlag(MouseButtons.Right))
        {
            planeRotation.X += (MousePosition.Y - mousePosPrev.Y) * 0.25f;
            planeRotation.Y += (MousePosition.X - mousePosPrev.X) * 0.25f;
        }

        if (MouseButtons.HasFlag(MouseButtons.Middle))
        {
            planeRotation.Z += (MousePosition.Y - mousePosPrev.Y) * 0.25f;
        }

        mousePosPrev = MousePosition;

        Console.WriteLine($"Updating: {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.Restart();

        Graphics g = e.Graphics;

        RerenderImage();

        g.DrawImage(finalTexture, 0, 0);
        Console.WriteLine($"Drawing image: {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.Restart();

        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        posBuffer.Dispose();
        normBuffer.Dispose();
        finalTextureBuffer.Dispose();

        posBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<float4>(ClientSize.Width * ClientSize.Height);
        normBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<float4>(ClientSize.Width * ClientSize.Height);
        finalTextureBuffer = GraphicsDevice.GetDefault().AllocateReadWriteTexture2D<Rgba32, float4>(ClientSize.Width, ClientSize.Height);
        finalTexture = new Bitmap(ClientSize.Width, ClientSize.Height);
    }

    private void RerenderImage(){
        Matrix4x4 camTransform = Matrix4x4.CreateTranslation(cameraOffset) * Matrix4x4.CreateFromYawPitchRoll(DegToRad(cameraRotation.Y), DegToRad(cameraRotation.X), 0);

        CpuCrossSectionPlane plane = CpuCrossSectionPlane.identity;
        plane *= CreateRotationMatrix(Axis.Z, Axis.W, DegToRad(planeRotation.Z));
        plane *= CreateRotationMatrix(Axis.X, Axis.W, DegToRad(planeRotation.X));
        plane *= CreateRotationMatrix(Axis.Y, Axis.W, DegToRad(planeRotation.Y));
        
        //calculate camera slope
        float vertSlope = MathF.Tan(fieldOfView/2);
        float horzSlope = vertSlope * AspectRatio;

        //render the position and normal buffers
        GraphicsDevice.GetDefault().For(ClientSize.Width, ClientSize.Height, new RenderBase(
            posBuffer,
            normBuffer,
            new float2(horzSlope, vertSlope),
            Matrix4x4.Transpose(camTransform),
            plane
        ));

        //render the final image
        GraphicsDevice.GetDefault().For(ClientSize.Width, ClientSize.Height, new RenderFinal(
            posBuffer,
            normBuffer,
            finalTextureBuffer
        ));

        Console.WriteLine($"Rendering: {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.Restart();

        CopyToImage();
        Console.WriteLine($"Copying: {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.Restart();
    }

    private unsafe void CopyToImage(){
        BitmapData data = finalTexture.LockBits(new Rectangle(0, 0, finalTexture.Width, finalTexture.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        void* scan0 = data.Scan0.ToPointer();

        finalTextureBuffer.CopyTo(new Span<Rgba32>(scan0, ClientSize.Width * ClientSize.Height * sizeof(Rgba32)));

        finalTexture.UnlockBits(data);
    }
}
