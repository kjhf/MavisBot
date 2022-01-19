using System;

namespace Mavis.Utils
{
  public static class ConsoleHelper
  {
    public static void Pause(string? message = null)
    {
      Console.WriteLine();
      Console.WriteLine(message ?? "Press any key to continue ...");
      Console.ReadKey();
    }
  }
}