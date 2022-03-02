using Mavis.Utils;
using System;
using System.Collections.Generic;

namespace Mavis.SlappSupport
{
  internal static class PleaseWaitMessages
  {
    private static readonly Random random = new();

    public static readonly IReadOnlyList<string> Messages = new[]
    {
      "<a:typing:897094396502224916> Just a sec!",
      "<:RunSpeedUp:841052610320400475> On it!",
      "<:SwimSpeedUp:841052610106097674> I'll go get that!",
      "<:InkResistanceUp:841052609627684875> Give me two secs!",
      "<:DropRoller:841052609859289118> Slidin' in shortly!",
      "<:Tenacity:841052610173206548> Back with you soon!",
      "<:Unknown:892498504822431754> What will it be?",
      "<a:eevee_slap:895391059985715241> Slap slap slap",
      $"<:InkSaverMain:841052609875148911> You should play the {Weapon.GetRandomWeapon()} next!",
    };

    public static string GetRandomMessage()
    {
      return random.Choice(Messages);
    }
  }
}