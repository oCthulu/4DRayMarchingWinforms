using System.Text.Json;
using System.Text.Json.Nodes;

public enum SkyboxType
{
    Transparent,
    Black,
    Gradient
}

public static class Options{
    public static SkyboxType skyboxType;
    public static SkyboxType screenshotSkyboxType;

    public static Point screenshotResolution;

    public static float mainLightWeight;
    public static float skyLightWeight;
    public static float ambientLightWeight;

    static Options(){
        JsonElement options = JsonDocument.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "/options.json")).RootElement;

        skyboxType = GetSkyboxType(options.GetProperty("skyboxType").GetString()) ?? SkyboxType.Transparent;
        screenshotSkyboxType = GetSkyboxType(options.GetProperty("screenshotSkyboxType").GetString()) ?? skyboxType;

        screenshotResolution = new Point(options.GetProperty("screenshotResolution").GetProperty("x").GetInt32(), options.GetProperty("screenshotResolution").GetProperty("y").GetInt32());

        mainLightWeight = options.GetProperty("mainLightWeight").GetSingle();
        skyLightWeight = options.GetProperty("skyLightWeight").GetSingle();
        ambientLightWeight = options.GetProperty("ambientLightWeight").GetSingle();
    }

    static SkyboxType? GetSkyboxType(string? skyboxType){
        return skyboxType == null? null : skyboxType switch
        {
            "transparent" => SkyboxType.Transparent,
            "black" => SkyboxType.Black,
            "gradient" => SkyboxType.Gradient,
            _ => throw new Exception("Invalid skybox type")
        };
    }

    public static RenderingOptions MainRenderingOptions => new RenderingOptions{
        skyboxType = skyboxType,
        mainLightWeight = mainLightWeight,
        skyLightWeight = skyLightWeight,
        ambientLightWeight = ambientLightWeight
    };

    public static RenderingOptions ScreenshotRenderingOptions => new RenderingOptions{
        skyboxType = screenshotSkyboxType,
        mainLightWeight = mainLightWeight,
        skyLightWeight = skyLightWeight,
        ambientLightWeight = ambientLightWeight
    };
}

public struct RenderingOptions{
    public SkyboxType skyboxType;
    public float mainLightWeight;
    public float skyLightWeight;
    public float ambientLightWeight;
}