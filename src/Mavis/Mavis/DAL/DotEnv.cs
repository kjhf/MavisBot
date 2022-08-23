using NLog;
using System;
using System.IO;

namespace Mavis.DAL
{
  public static class DotEnv
  {
    public static string DotEnvFileName { get; set; } = ".env";
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    public static void Load()
    {
      var dir = Directory.GetCurrentDirectory();
      string? dotenv = Path.Combine(dir, DotEnvFileName);
      if (File.Exists(dotenv))
      {
        Load(dotenv);
      }
      else
      {
        do
        {
          dir = Directory.GetParent(dir)?.FullName;
          dotenv = Path.Combine(dir ?? "", DotEnvFileName);
        }
        while (!File.Exists(dotenv) && dir != null);
        Load(dotenv);
      }
    }

    public static void Load(string? filePath)
    {
      if (filePath == null || !File.Exists(filePath))
      {
        log.Warn(DotEnvFileName + " file not found.");
      }
      else
      {
        log.Debug(DotEnvFileName + " file: " + filePath);
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