using Mavis.Controllers;
using Mavis.DAL;
using Mavis.Utils;
using NLog;
using NLog.Conditions;
using NLog.Targets;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

public static class MavisMain
{
  private static readonly Logger log = LogManager.GetCurrentClassLogger();
  private static readonly MavisBotController instance = new MavisBotController();

  public static async Task Main(string[] args)
  {
    ConfigureNLog();
    DotEnv.Load();
    Console.Title = Constants.ProgramName;

    try
    {
      log.Debug("RUNNING IN DEBUG.");
      if (Debugger.IsAttached)
      {
        log.Info("DEBUGGER IS ATTACHED.");
      }
      await instance.MainLoop(args);
    }
    catch (Exception ex)
    {
      log.Fatal(ex);
      log.Debug("Last exception left the main loop. The program will exit.");
    }

    Console.WriteLine();
    Console.WriteLine("Press any key to continue ...");
    Console.ReadKey();
  }

  private static void ConfigureNLog()
  {
    var config = new NLog.Config.LoggingConfiguration();
    var logconsole = new ColoredConsoleTarget("logconsole");

    // Rules for mapping loggers to targets
#if DEBUG
    var logfile = new FileTarget("logfile") { FileName = $"{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}-log.txt" };
    config.AddRule(LogLevel.Trace, LogLevel.Fatal, logconsole);
    config.AddRule(LogLevel.Warn, LogLevel.Fatal, logfile);
#else
    config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
#endif // DEBUG

    logconsole.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
    {
      Condition = ConditionParser.ParseExpression("level == LogLevel.Trace"),
      ForegroundColor = ConsoleOutputColor.DarkGray
    });
    logconsole.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
    {
      Condition = ConditionParser.ParseExpression("level == LogLevel.Debug"),
      ForegroundColor = ConsoleOutputColor.Gray
    });
    logconsole.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
    {
      Condition = ConditionParser.ParseExpression("level == LogLevel.Info"),
      ForegroundColor = ConsoleOutputColor.Green
    });
    logconsole.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
    {
      Condition = ConditionParser.ParseExpression("level == LogLevel.Warn"),
      ForegroundColor = ConsoleOutputColor.DarkYellow
    });
    logconsole.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
    {
      Condition = ConditionParser.ParseExpression("level == LogLevel.Error"),
      ForegroundColor = ConsoleOutputColor.DarkRed
    });
    logconsole.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
    {
      Condition = ConditionParser.ParseExpression("level == LogLevel.Fatal"),
      ForegroundColor = ConsoleOutputColor.Red
    });

    // Apply config
    LogManager.Configuration = config;
  }
}