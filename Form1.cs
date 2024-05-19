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
    private Vector3 cameraRotation = new Vector3(45, 45, 0);

    private Vector4 planeOffset = new Vector4(0, 0, 0, 0);
    private Vector3 planeRotation = new Vector3(0, 0, 0);

    private Point mousePosPrev;

    int sceneType = 0;

    ReadWriteBuffer<float4> posBuffer;
    ReadWriteBuffer<float4> normBuffer;

    ReadWriteTexture2D<Bgra32, float4> finalTextureBuffer;
    Bitmap finalTexture;

    public Form1()
    {
        posBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<float4>(ClientSize.Width * ClientSize.Height);
        normBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<float4>(ClientSize.Width * ClientSize.Height);

        finalTextureBuffer = GraphicsDevice.GetDefault().AllocateReadWriteTexture2D<Bgra32, float4>(ClientSize.Width, ClientSize.Height);
        finalTexture = new Bitmap(ClientSize.Width, ClientSize.Height);

        InitializeComponent();
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        //update camera rotation and plane
        if (MouseButtons.HasFlag(MouseButtons.Right) || (MouseButtons.HasFlag(MouseButtons.Left) && ModifierKeys.HasFlag(Keys.Control)))
        {
            planeRotation.X += (MousePosition.Y - mousePosPrev.Y) * 0.25f;
            planeRotation.Y += (MousePosition.X - mousePosPrev.X) * 0.25f;
        }
        else if (MouseButtons.HasFlag(MouseButtons.Middle) || (MouseButtons.HasFlag(MouseButtons.Left) && ModifierKeys.HasFlag(Keys.Shift)))
        {
            planeRotation.Z += (MousePosition.Y - mousePosPrev.Y) * 0.25f;
        }
        else if (MouseButtons.HasFlag(MouseButtons.Left) && ModifierKeys.HasFlag(Keys.Alt))
        {
            planeOffset.W += (MousePosition.Y - mousePosPrev.Y) * 0.002f;
        }
        else if (MouseButtons.HasFlag(MouseButtons.Left))
        {
            cameraRotation.X += (MousePosition.Y - mousePosPrev.Y) * 0.5f;
            cameraRotation.Y += (MousePosition.X - mousePosPrev.X) * 0.5f;
        }

        mousePosPrev = MousePosition;

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
        finalTextureBuffer = GraphicsDevice.GetDefault().AllocateReadWriteTexture2D<Bgra32, float4>(ClientSize.Width, ClientSize.Height);
        finalTexture = new Bitmap(ClientSize.Width, ClientSize.Height);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch(e.KeyCode){
            case Keys.F5: SaveScreenshot(); break;
            case Keys.Right: sceneType = (sceneType + 1) % RenderBase.SCENE_COUNT; break;
            case Keys.Left: sceneType = (sceneType + (RenderBase.SCENE_COUNT - 1)) % RenderBase.SCENE_COUNT; break;
            case Keys.R: planeOffset = new Vector4(0, 0, 0, 0); planeRotation = new Vector3(0, 0, 0); break;
        }
    }

    private void RerenderImage(){
        RenderImage(posBuffer, normBuffer, finalTextureBuffer, Options.MainRenderingOptions);
        CopyToImage(finalTextureBuffer, finalTexture);
    }

    private void RenderImage(ReadWriteBuffer<float4> posBuffer, ReadWriteBuffer<float4> normBuffer, IReadWriteNormalizedTexture2D<float4> finalTexture, RenderingOptions options)
    {
        int width = finalTexture.Width;
        int height = finalTexture.Height;

        Matrix4x4 camTransform = Matrix4x4.CreateTranslation(cameraOffset) * Matrix4x4.CreateFromYawPitchRoll(DegToRad(cameraRotation.Y), DegToRad(cameraRotation.X), 0);

        CpuCrossSectionPlane plane = new CpuCrossSectionPlane(Matrix4x4.Identity, planeOffset);
        plane *= CreateRotationMatrix(Axis.Z, Axis.W, DegToRad(planeRotation.Z));
        plane *= CreateRotationMatrix(Axis.X, Axis.W, DegToRad(planeRotation.X));
        plane *= CreateRotationMatrix(Axis.Y, Axis.W, DegToRad(planeRotation.Y));
        
        //calculate camera slope
        float vertSlope = MathF.Tan(fieldOfView/2);
        float horzSlope = vertSlope * (width / (float)height);

        //render the position and normal buffers
        GraphicsDevice.GetDefault().For(width, height, new RenderBase(
            posBuffer,
            normBuffer,
            new float2(horzSlope, vertSlope),
            Matrix4x4.Transpose(camTransform),
            plane,
            sceneType
        ));

        //render the final image
        GraphicsDevice.GetDefault().For(width, height, new RenderFinal(
            posBuffer,
            normBuffer,
            finalTexture,
            (int) options.skyboxType,
            new float4(1, 1, 1, options.mainLightWeight),
            new float4(1, 1, 1, options.skyLightWeight),
            new float4(1, 1, 1, options.ambientLightWeight)
        ));
    }

    private void RenderImage(IReadWriteNormalizedTexture2D<float4> finalTexture, RenderingOptions options){
        ReadWriteBuffer<float4> posBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<float4>(finalTexture.Width * finalTexture.Height);
        ReadWriteBuffer<float4> normBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<float4>(finalTexture.Width * finalTexture.Height);

        RenderImage(posBuffer, normBuffer, finalTexture, options);
    }

    private void SaveScreenshot(){
        ReadWriteTexture2D<Rgba32, float4> screenshot = GraphicsDevice.GetDefault().AllocateReadWriteTexture2D<Rgba32, float4>(Options.screenshotResolution.X, Options.screenshotResolution.Y);
        //Bitmap finalScreenshot = new Bitmap(screenshot.Width, screenshot.Height);

        RenderImage(screenshot, Options.ScreenshotRenderingOptions);
        //CopyToImage(screenshot, finalScreenshot);

        string screenshotsDir = AppDomain.CurrentDomain.BaseDirectory + "screenshots";
        //create the screenshots directory if it doesn't exist
        if(!Directory.Exists(screenshotsDir)) Directory.CreateDirectory(screenshotsDir);

        string basePath = screenshotsDir + "/screenshot";

        //find the next available screenshot number
        int i = 0;
        while(File.Exists(basePath + i + ".png")) i++;
        
        screenshot.Save(basePath + i + ".png");
    }

    private unsafe void CopyToImage(ReadWriteTexture2D<Bgra32, float4> finalTextureBuffer, Bitmap finalTexture){
        BitmapData data = finalTexture.LockBits(new Rectangle(0, 0, finalTexture.Width, finalTexture.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        void* scan0 = data.Scan0.ToPointer();

        finalTextureBuffer.CopyTo(new Span<Bgra32>(scan0, ClientSize.Width * ClientSize.Height * sizeof(Rgba32)));

        finalTexture.UnlockBits(data);
    }
}
