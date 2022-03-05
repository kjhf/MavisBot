using Mavis.SlappSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace MavisTests
{
  [TestClass]
  public class SlappCommandHandlerTests
  {
    [DataRow(1, "1999-01-01-my-tournament-is-the-best-jan-1999", "1999-02-01-my-tournament-is-the-best-feb-1999")]
    [DataRow(4,
      "2020-09-01-splatoon-2-north-america-open-september-2020",
      "2021-06-01-splatoon-2-north-american-online-open-june-2021",
      "2020-02-01-splatoon-2-the-return-of-2v2-tournaments",
      "2019-09-04-splatoon-2-2v2-tuesdays-38",
      "2019-08-28-splatoon-2-2v2-tuesdays-37-5d5eb9432419834065725fc1")]  // Only the last two should group (notice america(n) and "online" open)
    [DataRow(2,
      "2021-12-09-swim-or-sink-51-61a27ec037497001768b52d9",
      "2022-01-06-swim-or-sink-54-61d264630d5ab55c817b180b",
      "2021-12-23-swim-or-sink-53-61a27f77cec81d6a1b5aad10",
      "2020-06-28-dual-ink-19-5ef353ce0633ab5068e80615",
      "2020-07-05-dual-ink-20-5efd2cf3a2b8f022508f7ccd")]
    [DataRow(2,
      "2021-11-15-LUTI-S12",
      "2021-04-17-LUTI-S11")]  // LUTI sources are too short to combine
    [DataRow(1, "Test")]
    [DataRow(0)]
    [DataTestMethod]
    public void TestSourceGroups(int expected, params string[] sources)
    {
      var sourceable = new ReadonlySourceableContainer(sources);
      var lines = SlappCommandHandler.GetGroupedSourcesText(sourceable);
      Console.WriteLine(string.Join("\n", lines));
      Assert.AreEqual(expected, lines.Count, $"{expected} != {lines.Count}");
    }
  }
}