using Mavis.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.DrawingCore;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DiscordColor = Discord.Color;
using DrawingColor = System.DrawingCore.Color;

namespace Mavis.Imaging
{
  public static class ImageManipulator
  {
    private static readonly Random rand = new();

    //private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    public static void ChangeHue(Bitmap image, int hueChange)
    {
      for (int x = 0; x < image.Width; x++)
      {
        for (int y = 0; y < image.Height; y++)
        {
          DrawingColor px = image.GetPixel(x, y);
          byte alpha = px.A;
          float hue = (px.GetHue() + hueChange) % 360;
          float sat = px.GetSaturation();
          float bri = px.GetBrightness();
          image.SetPixel(x, y, FromAHSB(alpha, hue, sat, bri));
        }
      }
    }

    public static IDictionary<DrawingColor, uint> EvaluateColours(Bitmap image)
    {
      var retVal = new Dictionary<DrawingColor, uint>();
      for (int x = 0; x < image.Width; x++)
      {
        for (int y = 0; y < image.Height; y++)
        {
          DrawingColor px = image.GetPixel(x, y);
          if (retVal.ContainsKey(px))
          {
            ++retVal[px];
          }
          else
          {
            retVal.Add(px, 1);
          }
        }
      }
      return retVal;
    }

    /// <summary>
    /// Find the first hex colour in a string
    /// </summary>
    /// <param name="input"></param>
    public static bool FindHexColour(string input, [NotNullWhen(true)] out string? match)
    {
      const string HexCodeRegex = @"(\b#?(([\da-fA-F]{3})\b)|(([\da-fA-F]{6})\b)|(([\da-fA-F]{8})\b))";
      match = Regex.Match(input, HexCodeRegex, RegexOptions.Compiled)?.Value;
      return !string.IsNullOrEmpty(match);
    }

    /// <summary>
    /// Creates a Color from alpha, hue, saturation and brightness.
    /// </summary>
    /// <param name="alpha">The alpha channel value.</param>
    /// <param name="hue">The hue value. Note hue is from 0-360 degrees.</param>
    /// <param name="saturation">The saturation value from 0-1.</param>
    /// <param name="brightness">The brightness value from 0-1.</param>
    /// <returns>A Color with the given values.</returns>
    public static DrawingColor FromAHSB(byte alpha, float hue, float saturation, float brightness)
    {
      Contract.Requires(0f <= hue && hue <= 360f);
      Contract.Requires(0f <= saturation && saturation <= 1f);
      Contract.Requires(0f <= brightness && brightness <= 1f);
      Contract.EndContractBlock();

      if (saturation == 0)
      {
        return DrawingColor.FromArgb(
                            alpha,
                            Convert.ToInt32(brightness * 255),
                            Convert.ToInt32(brightness * 255),
                            Convert.ToInt32(brightness * 255));
      }

      float fMax, fMid, fMin;
      int iSextant, iMax, iMid, iMin;

      if (brightness > 0.5)
      {
        fMax = brightness - (brightness * saturation) + saturation;
        fMin = brightness + (brightness * saturation) - saturation;
      }
      else
      {
        fMax = brightness + (brightness * saturation);
        fMin = brightness - (brightness * saturation);
      }

      iSextant = (int)Math.Floor(hue / 60f);
      if (hue >= 300f)
      {
        hue -= 360f;
      }

      hue /= 60f;
      hue -= 2f * (float)Math.Floor((iSextant + 1f) % 6f / 2f);
      if (iSextant % 2 == 0)
      {
        fMid = (hue * (fMax - fMin)) + fMin;
      }
      else
      {
        fMid = fMin - (hue * (fMax - fMin));
      }

      iMax = Convert.ToInt32(fMax * 255);
      iMid = Convert.ToInt32(fMid * 255);
      iMin = Convert.ToInt32(fMin * 255);

      return iSextant switch
      {
        1 => DrawingColor.FromArgb(alpha, iMid, iMax, iMin),
        2 => DrawingColor.FromArgb(alpha, iMin, iMax, iMid),
        3 => DrawingColor.FromArgb(alpha, iMin, iMid, iMax),
        4 => DrawingColor.FromArgb(alpha, iMid, iMin, iMax),
        5 => DrawingColor.FromArgb(alpha, iMax, iMin, iMid),
        _ => DrawingColor.FromArgb(alpha, iMax, iMid, iMin),
      };
    }

    /// <summary>
    /// Get a drawing colour from a string code (rgb or rrggbb or aarrggbb). Optionally set to opaque.
    /// </summary>
    public static DrawingColor FromHexCode(string code, bool setOpaque = true)
    {
      code = code.Replace("#", "");
      if (code.Length == 3)
      {
        code = $"{code[0]}{code[0]}{code[1]}{code[1]}{code[2]}{code[2]}";
      }

      uint argb = uint.Parse(code, NumberStyles.HexNumber);
      if (setOpaque)
      {
        argb |= 0xFF000000;
      }

      return DrawingColor.FromArgb((int)argb);
    }

    /// <summary>
    /// Get a drawing colour from a string, which can be a hex code or name. Optionally set to opaque.
    /// Returns null if the name is not recognised.
    /// </summary>
    public static DrawingColor? FromString(string input, bool setOpaque = true)
    {
      if (FindHexColour(input, out string? foundColour))
      {
        return FromHexCode(foundColour, setOpaque);
      }

      if (Enum.TryParse(input, true, out KnownColor knownColor))
      {
        DrawingColor c = DrawingColor.FromKnownColor(knownColor);
        if (setOpaque)
        {
          c = c.ToOpaqueDrawingColor();
        }
        return c;
      }

      // Custom additions e.g. from Inkipedia and colours missed by Discord and .NET
      return input switch
      {
        "generic" => (DrawingColor?)DrawingColor.FromArgb(255, 0, 153, 255),
        "grello" => (DrawingColor?)DrawingColor.FromArgb(255, 170, 220, 0),
        "niwa" => (DrawingColor?)DrawingColor.FromArgb(255, 255, 128, 0),
        "random" => (DrawingColor?)GetRandomColor(),
        "octo" => (DrawingColor?)DrawingColor.FromArgb(255, 174, 21, 102),
        "splatoon" or "splatoon_1" => (DrawingColor?)DrawingColor.FromArgb(255, 170, 220, 0),
        "splatoon_2" => (DrawingColor?)DrawingColor.FromArgb(255, 240, 60, 120),
        "splatoon_3" => (DrawingColor?)DrawingColor.FromArgb(255, 235, 238, 61),
        _ => null,
      };
    }

    public static int FunctionARGB(byte a, byte r, byte g, byte b)
    {
      return a << 24 | 255 - Math.Max(Math.Max(r, g), b) << 16 | 255 - Math.Max(Math.Max(r, g), b) << 8 | 255 - Math.Max(Math.Max(r, g), b);
    }

    /// <summary>
    /// Generates a square Bitmap of the specified colour and size (pixels across).
    /// </summary>
    /// <param name="c"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    public static Bitmap GenerateBitmapFromColour(DrawingColor c, int size = 100)
    {
      var b = new Bitmap(size, size);
      using (Graphics g = Graphics.FromImage(b))
      {
        g.Clear(c);
      }

      return b;
    }

    public static string GreyscaleImageToASCII(Image img)
    {
      var output = new StringBuilder();

      // Create a bitmap from the image
      using (var bmp = new Bitmap(img))
      {
        try
        {
          // Loop through each pixel in the bitmap
          for (int y = 0; y < bmp.Height; y++)
          {
            for (int x = 0; x < bmp.Width; x++)
            {
              // Get the colour of the current pixel
              DrawingColor col = bmp.GetPixel(x, y);

              // To convert to greyscale, the easiest method is to add
              // the R+G+B colours and divide by three to get the greyscale colour.
              col = DrawingColor.FromArgb(
                (col.R + col.G + col.B) / 3,
                (col.R + col.G + col.B) / 3,
                (col.R + col.G + col.B) / 3);

              // Get the Red value from the greyscale colour,
              // parse to an int [0-255]
              int rValue = int.Parse(col.R.ToString());

              // Append the "colour" using various darknesses of ASCII character.
              output.Append(GetAsciiChar(rValue));

              // If we're at the width, insert a line break
              if (x == bmp.Width - 1 && y != bmp.Height - 1)
              {
                output.Append('\n');
              }
            }
          }
        }
        catch (Exception exc)
        {
          output.AppendLine(exc.ToString());
        }
      }
      return output.ToString();
    }

    /// <summary>
    /// Performs a meme and returns the file path.
    /// </summary>
    public static string PerformMeme(Bitmap overlay, string templatePath, Point[] destinationPoints)
    {
      string filePath;
      using (Image i = Image.FromFile(templatePath))
      {
        using var template = new Bitmap(i);
        using var result = new Bitmap(template.Width, template.Height);
        result.MakeTransparent(DrawingColor.White);

        using (var g = Graphics.FromImage(result))
        {
          g.DrawImage(overlay, destinationPoints);
          g.DrawImage(template, 0, 0);

          for (int x = 0; x < overlay.Width; x++)
          {
            for (int y = 0; y < overlay.Height; y++)
            {
              if (overlay.GetPixel(x, y).A == 0)
              {
                overlay.SetPixel(x, y, DrawingColor.White);
              }
            }
          }

          g.Save();
        }

        filePath = Path.GetTempFileName() + ".png";
        result.Save(filePath);
      }
      return filePath;
    }

    public static void PerformObabo(Bitmap image, MirrorType mirrorType)
    {
      int width = image.Width;
      int height = image.Height;

      switch (mirrorType)
      {
        case MirrorType.LeftOntoRight:
        {
          for (int x = 0; x < width / 2; x++)
          {
            for (int y = 0; y < image.Height; y++)
            {
              DrawingColor c = image.GetPixel(x, y);
              image.SetPixel(width - 1 - x, y, c);
            }
          }
          break;
        }

        case MirrorType.RightOntoLeft:
        {
          for (int x = 0; x < width / 2; x++)
          {
            for (int y = 0; y < image.Height; y++)
            {
              DrawingColor c = image.GetPixel(width - 1 - x, y);
              image.SetPixel(x, y, c);
            }
          }
          break;
        }

        case MirrorType.TopOntoBottom:
        {
          for (int y = 0; y < height / 2; y++)
          {
            for (int x = 0; x < width; x++)
            {
              DrawingColor c = image.GetPixel(x, y);
              image.SetPixel(x, height - 1 - y, c);
            }
          }
          break;
        }

        case MirrorType.BottomOntoTop:
        {
          for (int y = 0; y < height / 2; y++)
          {
            for (int x = 0; x < width; x++)
            {
              DrawingColor c = image.GetPixel(x, height - 1 - y);
              image.SetPixel(x, y, c);
            }
          }
          break;
        }
      }
    }

    /// <summary>
    /// Generate an image based off of <paramref name="image"/> with
    /// wavy rows.
    /// Neither Bitmaps are disposed.
    /// </summary>
    /// <param name="image"></param>
    /// <returns></returns>
    public static Bitmap PerformWave(Bitmap image)
    {
      int maxChange = Math.Max(10, (int)(image.Width * 0.2) + 1);
      bool leanRight = rand.Next(0, 2) != 0;

      var wave = new Bitmap(image.Width + (maxChange * 2), image.Height);
      wave.MakeTransparent(DrawingColor.Black);

      int offset = maxChange;
      for (int y = 0; y < image.Height; y++)
      {
        for (int x = 0; x < image.Width; x++)
        {
          DrawingColor c = image.GetPixel(x, y);
          // Make sure transparent does not collide.
          if (c == DrawingColor.Black)
          {
            c = DrawingColor.FromArgb(255, 0, 0, 1);
          }
          wave.SetPixel(x + offset, y, c);
        }

        int result = rand.Next(0, 3);
        switch (result)
        {
          case 0: // Going left!
            offset = Math.Max(0, offset - 1);
            break;

          case 1: // Going right!
            offset = Math.Min(maxChange * 2, offset + 1);
            break;

          case 2: // Lean!
            offset = leanRight ? Math.Min(maxChange * 2, offset + 1) : Math.Max(0, offset - 1);
            break;
        }
      }

      return wave;
    }

    public static ConsoleColor ToConsoleColor(this System.Drawing.Color c)
    {
      return ToConsoleColor(c.R, c.G, c.B);
    }

    public static ConsoleColor ToConsoleColor(this DrawingColor c)
    {
      return ToConsoleColor(c.R, c.G, c.B);
    }

    //https://stackoverflow.com/questions/1988833/converting-color-to-consolecolor
    public static ConsoleColor ToConsoleColor(byte r, byte g, byte b)
    {
      int index = (r > 128 || g > 128 || b > 128) ? 8 : 0; // Bright bit
      index |= (r > 64) ? 4 : 0; // Red bit
      index |= (g > 64) ? 2 : 0; // Green bit
      index |= (b > 64) ? 1 : 0; // Blue bit
      return (ConsoleColor)index;
    }

    public static DiscordColor ToDiscordColor(this DrawingColor c)
      => new(c.R, c.G, c.B);

    public static KnownColor ToNearestKnownColor(this DrawingColor c)
    {
      return Enum.GetValues<KnownColor>()
        .Skip((int)KnownColor.AliceBlue)  // Skip to the good part
        .Take((int)KnownColor.ButtonFace - (int)KnownColor.AliceBlue)
        .Select(known => DrawingColor.FromKnownColor(known))
        .OrderBy(test => Math.Abs(c.R - test.R) + Math.Abs(c.G - test.G) + Math.Abs(c.B - test.B))
        .First()
        .ToKnownColor();
    }

    public static DrawingColor ToOpaqueDrawingColor(this DrawingColor c)
      => DrawingColor.FromArgb(alpha: 0xFF, c.R, c.G, c.B);

    private static string GetAsciiChar(int redValue)
    {
      return redValue switch
      {
        >= 250 => " ",
        >= 240 => ".",
        >= 230 => "'",
        >= 220 => "^",
        >= 200 => "\"",
        >= 180 => ":",
        >= 170 => ";",
        >= 160 => "!",
        >= 150 => "i",
        >= 140 => "~",
        >= 120 => "+",
        >= 100 => "o",
        >= 80 => "X",
        >= 60 => "#",
        >= 40 => "%",
        >= 20 => "@",
        _ => "$",
      };
    }

    public static DrawingColor GetRandomColor()
      => rand.GetRandomColor();

    public static DrawingColor GetRandomColor(this Random rand)
      => DrawingColor.FromArgb(
        rand.Next(0, 256),
        rand.Next(0, 256),
        rand.Next(0, 256));
  }
}