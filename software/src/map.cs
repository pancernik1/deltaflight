using System;
using System.Collections.Generic;
using System.IO;
using SkiaSharp;
using MaxRev.Gdal.Core;
using OSGeo.GDAL;

public class map
{
    public int offx = 0;
    public int offy = 0;

    static public (string? argName, int? argVal) parseArg(string arg)
    {
        string? argName = null;
        int? argVal = null;
        string[] argList = new string[3];
        argList[0] = "offX";
        argList[1] = "offY";
        argList[2] = "scale";
        string extrName = "";
        string extrVal = "";
        int? eqI = null;

        for (int i = 0; i < arg.Length; i++)
        {
            if (arg[i] == '=') eqI = i;
        }

        if (eqI != null)
        {
            for (int i = 0; i < arg.Length; i++)
            {
                if (i < eqI)      extrName += arg[i];
                else if (i > eqI) extrVal  += arg[i];
            }

            for (int i = 0; i < argList.Length; i++)
            {
                if (extrName == argList[i]) { argName = argList[i]; break; }
                else argName = null;
            }

            try   { argVal = int.Parse(extrVal); }
            catch { argVal = null; }

            return (argName, argVal);
        }
        else
        {
            return (null, null);
        }
    }

    static public List<string> getArgs(string cmd, int qtEnd)
    {
        List<int> dIndexes = new List<int>();
        List<string> args  = new List<string>();

        for (int i = 0; i < cmd.Length; i++)
        {
            if (cmd[i] == '-') dIndexes.Add(i);
        }

        for (int i = 0; i < dIndexes.Count; i++)
        {
            args.Add("");
            for (int ch = 1; dIndexes[i] + ch < cmd.Length && cmd[dIndexes[i] + ch] != ' '; ch++)
                args[i] += cmd[dIndexes[i] + ch];
        }

        return args;
    }

    public static string checkForExceptions(List<int> arr, string _path, string ext, string cmd)
    {
        if (arr.Count % 2 != 0)      return "Quotes aren't closed";
        if (arr.Count < 2)           return "Add path to the image you want to convert";
        if (arr.Count > 2)           return "Multiple paths not supported";
        if (arr[1] - arr[0] < 2)     return "No path found";
        if (!File.Exists(_path))     return "File doesn't exist";
        if (ext != "jp2")            return "Wrong extension";

        List<string>  args     = getArgs(cmd, arr[1]);
        List<string?> argNames = new List<string?>();
        List<int?>    argVals  = new List<int?>();

        if (args.Count > 0)
        {
            for (int i = 0; i < args.Count; i++)
            {
                argNames.Add(parseArg(args[i]).argName);
                argVals.Add(parseArg(args[i]).argVal);
            }
            if (argNames.Contains(null)) return "unknown argument found";
            if (argVals.Contains(null))  return "Couldn't parse argument value (it has to be int)";
        }

        return null;
    }

    public static string path(string input)
    {
        List<int> quoteIndexes = new List<int>();
        string path  = "";
        string ext   = "";
        int dotIndex = 0;
        int scale    = 1;

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

        try
        {
            if (checkForExceptions(quoteIndexes, path, ext, input) == null)
            {
                int offX = 0;
                int offY = 0;

                List<string> args = getArgs(input, quoteIndexes[1]);

                if (args.Count > 0)
                {
                    for (int i = 0; i < args.Count; i++)
                    {
                        if (parseArg(args[i]).argName == "offX")
                            offX = parseArg(args[i]).argVal ?? 0;
                        if (parseArg(args[i]).argName == "offY")
                            offY = parseArg(args[i]).argVal ?? 0;
                        if (parseArg(args[i]).argName == "scale" && parseArg(args[i]).argVal != 0)
                            scale = parseArg(args[i]).argVal ?? 1;
                    }
                }

                jp2ds(path, offX, offY, scale);
            }
            else
            {
                return "Path error- " + checkForExceptions(quoteIndexes, path, ext, input);
            }
        }
        catch (System.Exception err)
        {
            return err.ToString();
        }

        return "conversion successful, scale=" + scale;
    }

    static public void jp2ds(string _path, int offX, int offY, int scale)
{
    GdalBase.ConfigureAll();
    Gdal.AllRegister();

    Dataset ds = Gdal.Open(_path, Access.GA_ReadOnly);

    int fullWidth  = ds.RasterXSize;
    int fullHeight = ds.RasterYSize;

    int outWidth  = 600;
    int outHeight = 400;
    int pixelCount = outWidth * outHeight;

    int srcOffX = Math.Clamp(offX, 0, fullWidth  - 1);
    int srcOffY = Math.Clamp(offY, 0, fullHeight - 1);

    // scale=1 reads full image, scale=2 reads half, etc.
    int srcRegionW = Math.Clamp(outWidth  * scale, 1, fullWidth  - srcOffX);
    int srcRegionH = Math.Clamp(outHeight * scale, 1, fullHeight - srcOffY);

    Band bandR = ds.GetRasterBand(1);
    Band bandG = ds.GetRasterBand(2);
    Band bandB = ds.GetRasterBand(3);

    byte[] bufR = new byte[pixelCount];
    byte[] bufG = new byte[pixelCount];
    byte[] bufB = new byte[pixelCount];

    bandR.ReadRaster(srcOffX, srcOffY, srcRegionW, srcRegionH, bufR, outWidth, outHeight, 0, 0);
    bandG.ReadRaster(srcOffX, srcOffY, srcRegionW, srcRegionH, bufG, outWidth, outHeight, 0, 0);
    bandB.ReadRaster(srcOffX, srcOffY, srcRegionW, srcRegionH, bufB, outWidth, outHeight, 0, 0);

    var bmp = new SKBitmap(outWidth, outHeight, SKColorType.Rgb888x, SKAlphaType.Opaque);
    for (int y = 0; y < outHeight; y++)
        for (int x = 0; x < outWidth; x++)
        {
            int i = y * outWidth + x;
            
            bmp.SetPixel(x, y, new SKColor(bufR[i], bufG[i], bufB[i]));
        }

    using var image   = SKImage.FromBitmap(bmp);
    using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
    using var stream  = File.OpenWrite("conv.png");
    encoded.SaveTo(stream);

    ds.Dispose();
    //Debug
    Console.WriteLine("Image created");
    Console.WriteLine("srcOffX - " + srcOffX.ToString());
    Console.WriteLine("srcOffY - " + srcOffY.ToString());
    Console.WriteLine("outWidth - " + outWidth.ToString());
    Console.WriteLine("outHeight - " + outHeight.ToString());
    Console.WriteLine("srcRegionW - " + srcRegionW.ToString());
    Console.WriteLine("srcRegionH - " + srcRegionH.ToString());
    Console.WriteLine("fullWidth - " + fullWidth.ToString());
    Console.WriteLine("fullHeight - " + fullHeight.ToString());
}
}