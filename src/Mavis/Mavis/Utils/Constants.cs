using System;

namespace Mavis.Utils
{
  public static class Constants
  {
    public static string ClientId => Environment.GetEnvironmentVariable("CLIENT_ID") ?? "931310753233895514";
    public static string BotInviteLink => $"https://discord.com/api/oauth2/authorize?client_id={ClientId}&permissions=431983414336&scope=bot%20applications.commands";
    public const string DevelopmentServerLink = "https://discord.gg/wZZv2Cr";
    public const string ProgramName = nameof(Mavis);
    public const string DebugProgramName = "Peep";
  }
}