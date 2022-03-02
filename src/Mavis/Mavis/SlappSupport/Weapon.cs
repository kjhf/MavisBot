using Mavis.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Mavis.SlappSupport
{
  public static class Weapon
  {
    private static readonly Random random = new();

    /// <summary>
    /// weapons are keyed by the actual name, values are "additional" names after spaces and punctuation ('-.) have been removed.
    /// e.g. ".52 Gal" already matches ".52gal" and "52gal".
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> weapons = new()
    {
      { ".52 Gal", new HashSet<string> { "gal", "52", "v52", "52g" } },  // default gal
      { ".52 Gal Deco", new HashSet<string> { "galdeco", "52deco", "52galdeco", "52gd" } },
      { ".96 Gal", new HashSet<string> { "96", "v96", "96g" } },
      { ".96 Gal Deco", new HashSet<string> { "96deco", "96galdeco", "96gd" } },
      { "Aerospray MG", new HashSet<string> { "mg", "aeromg", "silveraero", "silveraerospray", "aero", "aerospray" } },  // default aero
      { "Aerospray PG", new HashSet<string> { "pg", "aeropg", "bronzeaero", "bronzeaerospray" } },
      { "Aerospray RG", new HashSet<string> { "rg", "aerorg", "goldaero", "goldaerospray" } },
      { "Ballpoint Splatling", new HashSet<string> { "ballpoint", "bp", "pen" } },  // default ballpoint
      { "Ballpoint Splatling Nouveau", new HashSet<string> { "ballpointnouveau", "bpn", "bpsn", "bsn" } },
      { "Bamboozler 14 Mk I", new HashSet<string> { "bambooi", "bamboo1", "bamboo14mki", "bamboomki", "bamboomk1" } },
      { "Bamboozler 14 Mk II", new HashSet<string> { "bambooii", "bamboo2", "bamboo14mkii", "bamboomkii", "bamboomk2" } },
      { "Bamboozler 14 Mk III", new HashSet<string> { "bambooiii", "bamboo3", "bamboo14mkiii", "bamboomkiii", "bamboomk3" } },
      { "Blaster", new HashSet<string> { "vblaster" } },
      { "Bloblobber", new HashSet<string> { "blob", "vblob" } },
      { "Bloblobber Deco", new HashSet<string> { "blobdeco" } },
      { "Carbon Roller", new HashSet<string> { "carbon", "vcarbon" } },
      { "Carbon Roller Deco", new HashSet<string> { "carbondeco", "crd" } },
      { "Cherry H-3 Nozzlenose", new HashSet<string> { "cherry", "ch3", "ch3n", "cherrynozzle" } },
      { "Clash Blaster", new HashSet<string> { "clash", "vclash", "clashter" } },
      { "Clash Blaster Neo", new HashSet<string> { "clashneo", "clashterneo", "cbn" } },
      { "Classic Squiffer", new HashSet<string> { "csquif", "csquiff", "bluesquif", "bluesquiff", "squif", "squiff", "squiffer" } },  // default squiffer
      { "Clear Dapple Dualies", new HashSet<string> { "cdapple", "cdapples", "cleardualies", "clapples", "clappies", "cdd" } },
      { "Custom Blaster", new HashSet<string> { "cblaster" } },
      { "Custom Dualie Squelchers", new HashSet<string> { "cds", "customdualies", "cdualies" } },
      { "Custom E-Liter 4K", new HashSet<string> { "c4k", "ce4k", "celiter", "celitre", "celiter4k", "celitre4k", "custom4k" } },
      { "Custom E-Liter 4K Scope", new HashSet<string> { "c4ks", "ce4ks", "celiterscope", "celitrescope", "celiter4kscope", "celitre4kscope", "custom4kscope" } },
      { "Custom Explosher", new HashSet<string> { "cex", "cexplo", "cexplosher" } },
      { "Custom Goo Tuber", new HashSet<string> { "customgoo", "cgoo", "cgootube", "cgootuber", "cgt" } },
      { "Custom Hydra Splatling", new HashSet<string> { "customhyra", "chydra", "chydrasplatling", "chs" } },
      { "Custom Jet Squelcher", new HashSet<string> { "customjet", "cjet", "cjets", "cjs", "cjsquelcher", "cjetsquelcher" } },
      { "Custom Range Blaster", new HashSet<string> { "customrange", "crange", "crblaster", "crb" } },
      { "Custom Splattershot Jr.", new HashSet<string> { "customjunior", "cjr", "cjnr", "cjunior", "csj" } },
      { "Dapple Dualies", new HashSet<string> { "dapples", "vdapples", "vdd", "dd", "ddualies" } },
      { "Dapple Dualies Nouveau", new HashSet<string> { "dapplesnouveau", "ddn", "ddualiesn" } },
      { "Dark Tetra Dualies", new HashSet<string> { "tetra", "tetras", "tetradualies", "dark", "darks", "darktetra", "darktetras", "darkdualies", "dtd" } },  // default tetras
      { "Dualie Squelchers", new HashSet<string> { "ds", "vds" } },
      { "Dynamo Roller", new HashSet<string> { "dyna", "dynamo", "vdynamo", "silverdynamo" } },
      { "E-liter 4K", new HashSet<string> { "4k", "e4k", "eliter", "elitre", "eliter4k", "elitre4k" } },
      { "E-liter 4K Scope", new HashSet<string> { "4ks", "e4ks", "eliterscope", "elitrescope", "eliter4kscope", "elitre4kscope" } },
      { "Enperry Splat Dualies", new HashSet<string> { "edualies", "enperries", "enperrydualies", "esd" } },
      { "Explosher", new HashSet<string> { "vex", "explo", "vexplo" } },
      { "Firefin Splat Charger", new HashSet<string> { "firefin", "firefincharger", "fsc", "ffin" } },
      { "Firefin Splatterscope", new HashSet<string> { "firefinscope", "ffinscope" } },
      { "Flingza Roller", new HashSet<string> { "fling", "flingza", "vfling", "vflingza" } },
      { "Foil Flingza Roller", new HashSet<string> { "foilfling", "foilflingza", "ffling", "fflingza", "ffr" } },
      { "Foil Squeezer", new HashSet<string> { "fsqueezer" } },
      { "Forge Splattershot Pro", new HashSet<string> { "forge", "forgepro", "fpro", "fsp" } },
      { "Fresh Squiffer", new HashSet<string> { "fsquif", "fsquiff", "redsquif", "redsquiff" } },
      { "Glooga Dualies", new HashSet<string> { "glooga", "gloogas", "glues", "vglues", "vgloogas", "gd", "vgd" } },
      { "Glooga Dualies Deco", new HashSet<string> { "gloogadeco", "gloogasdeco", "gluesdeco", "dglues", "dgloogas", "gdd", "dgd" } },
      { "Gold Dynamo Roller", new HashSet<string> { "golddyna", "golddynamo", "gdr" } },
      { "Goo Tuber", new HashSet<string> { "goo", "vgoo", "gootube", "vgootube", "vgootuber" } },
      { "Grim Range Blaster", new HashSet<string> { "grim", "grange", "grblaster", "grb" } },
      { "H-3 Nozzlenose", new HashSet<string> { "h3", "vh3", "h3nozzle", "h3n" } },
      { "H-3 Nozzlenose D", new HashSet<string> { "h3d", "h3dnozzle", "h3nd", "h3dn" } },
      { "Heavy Splatling", new HashSet<string> { "heavy", "vheavy" } },
      { "Heavy Splatling Deco", new HashSet<string> { "heavyd", "heavydeco", "hsd" } },
      { "Heavy Splatling Remix", new HashSet<string> { "remix", "heavyremix", "hsr" } },
      { "Hero Blaster Replica", new HashSet<string> { "heroblaster" } },
      { "Hero Brella Replica", new HashSet<string> { "herobrella" } },
      { "Hero Charger Replica", new HashSet<string> { "herocharger" } },
      { "Hero Dualie Replicas", new HashSet<string> { "herodualie", "herodualies", "hdualie", "hdualies" } },
      { "Hero Roller Replica", new HashSet<string> { "heroroller" } },
      { "Hero Shot Replica", new HashSet<string> { "heroshot" } },
      { "Hero Slosher Replica", new HashSet<string> { "heroslosh", "heroslosher" } },
      { "Hero Splatling Replica", new HashSet<string> { "herosplatling", "heroheavy" } },
      { "Herobrush Replica", new HashSet<string> { "herobrush" } },
      { "Hydra Splatling", new HashSet<string> { "hydra", "vhydra", "vhydrasplatling" } },
      { "Inkbrush", new HashSet<string> { "brush", "vbrush", "vinkbrush" } },  // default brush
      { "Inkbrush Nouveau", new HashSet<string> { "brushn", "brushnouveau", "nbrush", "inkbrushn" } },
      { "Jet Squelcher", new HashSet<string> { "jet", "vjet", "jets", "vjets", "js", "vjs", "jsquelcher", "vjsquelcher", "vjetsquelcher" } },
      { "Kensa .52 Gal", new HashSet<string> { "kgal", "k52", "k52gal" } },  // default kgal
      { "Kensa Charger", new HashSet<string> { "kcharger" } },
      { "Kensa Dynamo Roller", new HashSet<string> { "kdyna", "kdynamo", "kensadynamo", "kdr" } },
      { "Kensa Glooga Dualies", new HashSet<string> { "kensaglooga", "kensagloogas", "kensaglues", "klues", "kglues", "klooga", "kloogas", "kgloogas", "kgd" } },
      { "Kensa L-3 Nozzlenose", new HashSet<string> { "knozzle", "kl3", "kl3n", "kl3nozzle" } },
      { "Kensa Luna Blaster", new HashSet<string> { "kensaluna", "kluna", "kuna", "kunablaster", "klb" } },
      { "Kensa Mini Splatling", new HashSet<string> { "kensamini", "kmini", "kimi", "kimisplatling", "kminisplatling", "kms" } },
      { "Kensa Octobrush", new HashSet<string> { "kensabrush", "kbrush", "krush", "kocto", "koctobrush", "kob" } },
      { "Kensa Rapid Blaster", new HashSet<string> { "kensarapid", "krapid", "krapidblaster", "kraster", "krb" } },
      { "Kensa Sloshing Machine", new HashSet<string> { "kensasloshmachine", "ksloshmachine", "kensamachine", "kmachine", "kachine", "kachin", "ksm" } },
      { "Kensa Splat Dualies", new HashSet<string> { "kensadualie", "kensadualies", "kdaulies", "kdaulie", "kdualie", "kdualies", "kaulies", "kualies", "kaulie", "kualie", "ksd" } },
      { "Kensa Splat Roller", new HashSet<string> { "kensaroller", "kroller", "kroll", "ksr" } },
      { "Kensa Splatterscope", new HashSet<string> { "kensascope", "ksscope", "kscope", "kss" } },
      { "Kensa Splattershot", new HashSet<string> { "kensashot", "ksshot", "kshot" } },
      { "Kensa Splattershot Jr.", new HashSet<string> { "kensajunior", "kjr", "kjnr", "kjunior", "ksj" } },
      { "Kensa Splattershot Pro", new HashSet<string> { "kensapro", "kpro", "ksp" } },
      { "Kensa Undercover Brella", new HashSet<string> { "kensaundercover", "kunder", "kensabrella", "kub" } },
      { "Krak-On Splat Roller", new HashSet<string> { "krakon", "krakonroller", "krack", "krackonroller", "krak", "krakenroller", "koroller", "koro", "kosr" } },
      { "L-3 Nozzlenose", new HashSet<string> { "l3", "vl3", "l3nozzle", "l3n" } },
      { "L-3 Nozzlenose D", new HashSet<string> { "l3d", "l3dnozzle", "l3nd", "l3dn" } },
      { "Light Tetra Dualies", new HashSet<string> { "light", "lights", "lightdualies", "lighttetra", "lighttetras" } },
      { "Luna Blaster", new HashSet<string> { "luna", "vluna", "vuna", "vlunablaster" } },
      { "Luna Blaster Neo", new HashSet<string> { "lunaneo", "lbn" } },
      { "Mini Splatling", new HashSet<string> { "mini", "vmini", "vimi", "vimisplatling", "vminisplatling", "vms" } },
      { "N-ZAP '83", new HashSet<string> { "zap83", "83", "bronzenzap", "bronzezap", "brownnzap", "brownzap", "rednzap", "redzap" } },  // By Twitter poll, this zap is the red one.
      { "N-ZAP '85", new HashSet<string> { "zap85", "85", "greynzap", "greyzap", "graynzap", "grayzap", "zap", "nzap" } },  // default zap
      { "N-ZAP '89", new HashSet<string> { "zap89", "89", "orangenzap", "orangezap" } },
      { "Nautilus 47", new HashSet<string> { "naut47", "47", "naut" } },  // default nautilus
      { "Nautilus 79", new HashSet<string> { "naut79", "79" } },
      { "Neo Splash-o-matic", new HashSet<string> { "neosplash", "nsplash", "nsplashomatic" } },
      { "Neo Sploosh-o-matic", new HashSet<string> { "neosploosh", "nsploosh", "nsplooshomatic" } },
      { "New Squiffer", new HashSet<string> { "nsquif", "nsquiff", "newsquif", "newsquiff" } },
      { "Octobrush", new HashSet<string> { "octo", "obrush", "vocto", "voctobrush", "vobrush" } },
      { "Octobrush Nouveau", new HashSet<string> { "octon", "obrushn", "octobrushn" } },
      { "Octo Shot Replica", new HashSet<string> { "oshot", "osr" } },
      { "Permanent Inkbrush", new HashSet<string> { "pbrush", "permabrush", "permanentbrush", "pinkbrush", "permainkbrush" } },
      { "Range Blaster", new HashSet<string> { "range", "vrange", "vrangeblaster" } },
      { "Rapid Blaster", new HashSet<string> { "rapid", "vrapid", "vrapidblaster" } },
      { "Rapid Blaster Deco", new HashSet<string> { "rapiddeco", "rapidd", "rapidblasterd", "rbd" } },
      { "Rapid Blaster Pro", new HashSet<string> { "rapidpro", "prorapid", "rbp" } },
      { "Rapid Blaster Pro Deco", new HashSet<string> { "rapidprodeco", "prodecorapid", "rbpd" } },
      { "Slosher", new HashSet<string> { "slosh", "vslosh" } },
      { "Slosher Deco", new HashSet<string> { "sloshd", "sloshdeco" } },
      { "Sloshing Machine", new HashSet<string> { "sloshmachine", "vsloshmachine", "vmachine", "machine", "vachine", "vsm" } },
      { "Sloshing Machine Neo", new HashSet<string> { "sloshmachineneo", "neosloshmachine", "neomachine", "machineneo", "smn" } },
      { "Soda Slosher", new HashSet<string> { "soda", "sodaslosh" } },
      { "Sorella Brella", new HashSet<string> { "sorella", "sbrella", "srella" } },
      { "Splash-o-matic", new HashSet<string> { "splash", "vsplash", "vsplashomatic" } },
      { "Splat Brella", new HashSet<string> { "brella", "vbrella", "vsplatbrella" } },
      { "Splat Charger", new HashSet<string> { "charger", "vcharger", "vsplatcharger" } },
      { "Splat Dualies", new HashSet<string> { "dualies", "vdualies", "vsplatdualies" } },
      { "Splat Roller", new HashSet<string> { "roller", "vroller", "vsplatroller" } },
      { "Splatterscope", new HashSet<string> { "scope", "vscope", "vsplatscope", "vsplatterscope" } },
      { "Splattershot", new HashSet<string> { "shot", "vshot", "vsplatshot", "vsplattershot" } },
      { "Splattershot Jr.", new HashSet<string> { "junior", "jr", "vjr", "jnr", "vjnr", "vjunior", "vsj" } },
      { "Splattershot Pro", new HashSet<string> { "pro", "vpro", "vsplatshotpro", "vsplatterpro" } },
      { "Sploosh-o-matic", new HashSet<string> { "sploosh", "vsploosh", "vsplooshomatic" } },
      { "Sploosh-o-matic 7", new HashSet<string> { "7", "sploosh7", "7sploosh", "7splooshomatic" } },
      { "Squeezer", new HashSet<string> { "vsqueezer" } },
      { "Tenta Brella", new HashSet<string> { "tent", "vent", "vtent", "tentbrella", "vtentbrella" } },
      { "Tenta Camo Brella", new HashSet<string> { "tentcamo", "camo", "camotent", "camobrella", "tentcamobrella", "tcb" } },
      { "Tenta Sorella Brella", new HashSet<string> { "tentsorella", "tsorella", "sorellatent", "tsorellabrella", "tentsorellabrella", "tsb" } },
      { "Tentatek Splattershot", new HashSet<string> { "ttek", "ttekshot", "tshot", "ttshot", "ttsplatshot", "ttsplattershot", "ttss", "ttk" } },
      { "Tri-Slosher", new HashSet<string> { "tri", "trislosh", "vtri", "vtrislosh", "vtrislosher" } },
      { "Tri-Slosher Nouveau", new HashSet<string> { "trin", "trisloshn", "trinouveau", "trisloshnouveau", "tsn" } },
      { "Undercover Brella", new HashSet<string> { "undercover", "ubrella", "vundercover", "vundercoverbrella" } },
      { "Undercover Sorella Brella", new HashSet<string> { "sunder", "sundercover", "undercoversorella", "sundercoverbrella", "usb" } },
      { "Zink Mini Splatling", new HashSet<string> { "zinkmini", "zmini", "zimi", "zimisplatling", "zminisplatling", "zms" } },
    };

    public static readonly IReadOnlyList<string> Weapons = weapons.Keys.ToImmutableArray();

    /// <summary>
    /// Weapons but have undergone <see cref="TransformWeapon"/>.
    /// </summary>
    private static readonly string[] weaponsTransformed = weapons.Keys.Select(TransformWeapon).ToArray();

    /// <summary>
    /// Try and get a weapon from this query.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="exact"></param>
    public static string? TryFindWeapon(string query, bool exact = false)
    {
      // First, match exact against the known weapons and then their transformed variants.
      var result = weapons.FirstOrDefault(entry => entry.Key.Equals(query)).Key;
      if (result == default)
      {
        result = weaponsTransformed.FirstOrDefault(wep => wep.Equals(query));
      }

      if (result != null || exact)
      {
        return result;
      }

      // Search inexact
      query = TransformWeapon(query);
      return Array.Find(weaponsTransformed, w => w.Equals(query));
    }

    public static string GetRandomWeapon()
    {
      return random.Choice(Weapons);
    }

    /// <summary>
    /// Transform the weapon into a searchable format.
    /// </summary>
    /// <param name="wep"></param>
    private static string TransformWeapon(string wep)
    {
      // Lower-case and remove spaces and punctuation
      return wep
        .ToLower()
        .Replace("[", "")
        .Replace(" ", "")
        .Replace(".", "")
        .Replace("\\", "")
        .Replace("-", "")
        .Replace("'", "")
        .Replace("]", "")

        // Typo corrections
        .Replace("duel", "dual")
        ;
    }
  }
}