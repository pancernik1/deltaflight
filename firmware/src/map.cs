using System.Collections.Generic;
using System.IO;
using SkiaSharp;
using MaxRev.Gdal.Core;
using OSGeo.GDAL;

public class map
{
    public static string checkForExceptions(List<int> arr, string _path, string ext)
    {
        if (arr.Count % 2 != 0)
            return "Quotes aren't closed";
        else if (arr.Count < 2)
            return "Add path to the image you want to convert";
        else if (arr.Count > 2)
            return "Multiple paths not supported";
        else if (arr[1] - arr[0] < 2)
            return "No path found";
        else if (!File.Exists(_path))
            return "File doesn't exist";
        else if (ext != "jp2")
            return "Wrong extension";
        else
            return null;
    }

    public static string path(string input)
    {
        List<int> quoteIndexes = new List<int>();
        string indexes = "";
        string path = "";
        string ext = "";
        int dotIndex = 0;

        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == '"') quoteIndexes.Add(i);
            if (input[i] == '.') dotIndex = i;
        }

        for (int i = 0; quoteIndexes.Count >= 2 && i < input.Length; i++)
        {
            if (quoteIndexes[0] < i && i < quoteIndexes[1])
            {
                path += input[i];
                if (i > dotIndex) ext += input[i];
            }
        }

        foreach (int n in quoteIndexes)
            indexes += n.ToString() + ',';

        if (checkForExceptions(quoteIndexes, path, ext) == null)
        {
            jp2ds(path);
            return "Quote indexes- " + indexes + " Path- " + path;
        }
        else
        {
            return "Path error- " + checkForExceptions(quoteIndexes, path, ext);
        }
    }

    static public void jp2ds(string _path)
    {
        GdalBase.ConfigureAll();
        Gdal.AllRegister();

        Dataset ds = Gdal.Open(_path, Access.GA_ReadOnly);

        int fullWidth  = ds.RasterXSize;
        int fullHeight = ds.RasterYSize;

        int outWidth  = 600;
        int outHeight = 400;
        int pixelCount = outWidth * outHeight;

        Band bandR = ds.GetRasterBand(1);
        Band bandG = ds.GetRasterBand(2);
        Band bandB = ds.GetRasterBand(3);

        byte[] bufR = new byte[pixelCount];
        byte[] bufG = new byte[pixelCount];
        byte[] bufB = new byte[pixelCount];

        bandR.ReadRaster(0, 0, fullWidth, fullHeight, bufR, outWidth, outHeight, 0, 0);
        bandG.ReadRaster(0, 0, fullWidth, fullHeight, bufG, outWidth, outHeight, 0, 0);
        bandB.ReadRaster(0, 0, fullWidth, fullHeight, bufB, outWidth, outHeight, 0, 0);

        var bmp = new SKBitmap(outWidth, outHeight, SKColorType.Rgb888x, SKAlphaType.Opaque);

        for (int y = 0; y < outHeight; y++)
        {
            for (int x = 0; x < outWidth; x++)
            {
                int i = y * outWidth + x;
                bmp.SetPixel(x, y, new SKColor(bufR[i], bufG[i], bufB[i]));
            }
        }

        using var image   = SKImage.FromBitmap(bmp);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream  = File.OpenWrite("conv.png");
        encoded.SaveTo(stream);

        ds.Dispose();
    }
}
