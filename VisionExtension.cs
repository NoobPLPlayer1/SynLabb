using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

static class VisionExtension
{
    public static async Task<IList<DetectedObject>> DetectObjects(this ComputerVisionClient client, string path)
    {
        List<VisualFeatureTypes?> features = new List<VisualFeatureTypes?>()
            {
                VisualFeatureTypes.Objects
            };

        using FileStream fs = new(path, FileMode.Open);
        ImageAnalysis results = await client.AnalyzeImageInStreamAsync(fs, visualFeatures: features);

        return results.Objects;
    }
    public static async Task<IList<DetectedObject>> DetectObjectsOnline(this ComputerVisionClient client, string url)
    {
        List<VisualFeatureTypes?> features = new List<VisualFeatureTypes?>()
            {
                VisualFeatureTypes.Objects
            };

        ImageAnalysis results = await client.AnalyzeImageAsync(url, visualFeatures: features);

        return results.Objects;
    }
}
