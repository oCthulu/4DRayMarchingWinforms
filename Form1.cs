using System.Drawing.Imaging;
using System.Numerics;
using ComputeSharp;

using static Util;

namespace _4DRayMarchingWinforms;

public partial class Form1 : Form
{
    public float AspectRatio => (float)ClientSize.Width / ClientSize.Height;
    public float fieldOfView = 90;
    ReadWriteBuffer<float4> posBuffer;
    ReadWriteBuffer<float4> normBuffer;

    ReadWriteTexture2D<Rgba32, float4> finalTextureBuffer;
    Bitmap finalTexture;

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
        Graphics g = e.Graphics;

        RerenderImage();

        g.DrawImage(finalTexture, 0, 0);

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
        Matrix4x4 camTransform = Matrix4x4.CreateTranslation(0, 0, -2) * Matrix4x4.CreateFromYawPitchRoll(DegToRad(-45), DegToRad(45), 0);
        
        //calculate camera slope
        float vertSlope = MathF.Tan(fieldOfView/2);
        float horzSlope = vertSlope * AspectRatio;

        //render the position and normal buffers
        GraphicsDevice.GetDefault().For(ClientSize.Width, ClientSize.Height, new RenderBase(
            posBuffer,
            normBuffer,
            new float2(horzSlope, vertSlope),
            Matrix4x4.Transpose(camTransform)
        ));

        //render the final image
        GraphicsDevice.GetDefault().For(ClientSize.Width, ClientSize.Height, new RenderFinal(
            posBuffer,
            normBuffer,
            finalTextureBuffer
        ));

        CopyToImage();
    }

    private unsafe void CopyToImage(){
        BitmapData data = finalTexture.LockBits(new Rectangle(0, 0, finalTexture.Width, finalTexture.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        void* scan0 = data.Scan0.ToPointer();

        finalTextureBuffer.CopyTo(new Span<Rgba32>(scan0, ClientSize.Width * ClientSize.Height * sizeof(Rgba32)));

        finalTexture.UnlockBits(data);
    }
}
