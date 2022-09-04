using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Rest;
using System.Diagnostics;
using System.Drawing;
using Microsoft.Extensions.Configuration;

IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath("appsettings.json"));
IConfigurationRoot configuration = builder.Build();
string subscriptionKey = configuration["VisionServiceKey"];
string endpoint = configuration["VisionServicesEndpoint"];

bool mark = true;
int markIndex = 0;
MarkType[] types = Enum.GetValues<MarkType>();
int markThickness = 4;

ComputerVisionClient client = Authenticate(endpoint, subscriptionKey);

ComputerVisionClient Authenticate(string endpoint, string key)
{
    ComputerVisionClient client =
      new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
      { 
          Endpoint = endpoint 
      };
    return client;
}


Menu menu = new();
foreach (var path in Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.jpg"))
{
    menu.Options.Add((
        () => $"{path}", 
        (key) => {
            if(key.Key == ConsoleKey.Enter) SelectObjectInImage(path).Wait(); 
        }
    ));
}
menu.Show();

async Task SelectObjectInImage(string path)
{
    var items = await client.DetectObjects(path);
    Menu menu = new()
    {
        Options = new()
        {
            (() => "Options".PadRight(32, '-'), null),
            (() => $"Action: {(mark ? "Mark" : "Cut")}", (key) =>
            {
                if (key.Key == ConsoleKey.LeftArrow)
                    mark = !mark;
                if (key.Key == ConsoleKey.RightArrow)
                    mark = !mark;
                if (key.Key == ConsoleKey.Enter)
                    mark = !mark;
            }
            ),
            (() => $"Mark Type: {types[markIndex]}", (key) =>
            {
                if (key.Key == ConsoleKey.LeftArrow)
                    markIndex = (markIndex - 1 + types.Length) % types.Length;
                if (key.Key == ConsoleKey.RightArrow)
                    markIndex = (markIndex + 1 + types.Length) % types.Length;
            }
            ),
            (() => $"Mark Thickness: {markThickness}", (key) =>
            {
                if (key.Key == ConsoleKey.LeftArrow)
                    markThickness -= 1;
                if (key.Key == ConsoleKey.RightArrow)
                    markThickness += 1;
                Math.Max(markThickness, 1);
            }
            ),
            (() => "Output".PadRight(32, '-'), null),
            (() => "All", (key) =>
            {
                if (key.Key == ConsoleKey.Enter)
                {
                    string newVariantPath = Path.GetFileNameWithoutExtension(path) + "_with_detected_objects" + Path.GetExtension(path);
                    if (mark)
                        MarkSelectedObjects(path, newVariantPath, items.ToArray());
                    else
                    {
                        int maxX = int.MinValue, maxY = int.MinValue, minX = int.MaxValue, minY = int.MaxValue;
                        foreach (var item in items)
                        {
                            maxX = Math.Max(item.Rectangle.X + item.Rectangle.W, maxX);
                            maxY = Math.Max(item.Rectangle.Y + item.Rectangle.H, maxY);
                            minX = Math.Min(item.Rectangle.X, minX);
                            minY = Math.Min(item.Rectangle.Y, minY);
                        }

                        CutOutSelectedObject(path, newVariantPath, new(
                            new(minX, minY, maxX - minX, maxY - minY)
                        ));
                    }
                    Process.Start("mspaint", $"\"{newVariantPath}\"");
                }
            }
            )

        }
    };
    foreach (var obj in items)
    {
        menu.Options.Add(
            (() => $"{obj.ObjectProperty}: {obj.Confidence}", 
            (key) => { 
                if (key.Key == ConsoleKey.Enter)
                {
                    string newVariantPath = Path.GetFileNameWithoutExtension(path) + "_with_detected_objects" + Path.GetExtension(path); 
                    if (mark)
                        MarkSelectedObjects(path, newVariantPath, obj);
                    else
                        CutOutSelectedObject(path, newVariantPath, obj);
                    Process.Start("mspaint", $"\"{newVariantPath}\"");
                }
            })
            );
    }
    menu.Show();
}

void MarkSelectedObjects(string srcPath, string dstPath, params DetectedObject[] objs)
{
    using var i = Image.FromFile(srcPath);
    using var g = Graphics.FromImage(i);
    using var p = new Pen(Brushes.Red, markThickness);

    foreach (var obj in objs)
        switch (types[markIndex])
        {
            case MarkType.Rectangle:
                g.DrawRectangle(p, new(new(obj.Rectangle.X, obj.Rectangle.Y), new(obj.Rectangle.W, obj.Rectangle.H)));
                break;
            case MarkType.Ellipse:
                g.DrawEllipse(p, new(new(obj.Rectangle.X, obj.Rectangle.Y), new(obj.Rectangle.W, obj.Rectangle.H)));
                break;
        }

    i.Save(dstPath);
}

void CutOutSelectedObject(string srcPath, string dstPath, DetectedObject obj)
{
    using var i = Image.FromFile(srcPath);
    using Bitmap b = new(obj.Rectangle.W, obj.Rectangle.H);
    using var g = Graphics.FromImage(b);

    g.DrawImage(i, new RectangleF(new(0, 0), new(obj.Rectangle.W, obj.Rectangle.H)), new RectangleF(new(obj.Rectangle.X, obj.Rectangle.Y), new(obj.Rectangle.W, obj.Rectangle.H)), GraphicsUnit.Pixel);

    b.Save(dstPath);
}

enum MarkType
{
    Rectangle,
    Ellipse
}
