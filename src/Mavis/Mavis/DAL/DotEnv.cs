using NLog;
using System;
using System.IO;

namespace Mavis.DAL
{
  public static class DotEnv
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    public static void Load()
    {
      var dir = Directory.GetCurrentDirectory();
      string? dotenv = Path.Combine(dir, ".env");
      if (File.Exists(dotenv))
      {
        Load(dotenv);
      }
      else
      {
        do
        {
          dir = Directory.GetParent(dir)?.FullName;
          dotenv = Path.Combine(dir ?? "", ".env");
        }
        while (!File.Exists(dotenv) && dir != null);
        Load(dotenv);
      }
    }

    public static void Load(string? filePath)
    {
      if (filePath == null || !File.Exists(filePath))
      {
        log.Warn(".env file not found.");
      }
      else
      {
        log.Debug(".env file: " + filePath);
        foreach (var line in File.ReadAllLines(filePath))
        {
          var parts = line.Split(
              '=',
              StringSplitOptions.RemoveEmptyEntries);

          if (parts.Length != 2)
            continue;

          log.Trace("Setting env key " + parts[0]);
          Environment.SetEnvironmentVariable(parts[0], parts[1]);
        }
      }
    }
  }
}