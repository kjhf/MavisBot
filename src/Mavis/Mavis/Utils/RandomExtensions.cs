using System;
using System.Collections.Generic;
using System.Linq;

namespace Mavis.Utils
{
  public static class RandomExtensions
  {
    /// <summary>
    /// Returns a random long from min (inclusive) to max (exclusive)
    /// </summary>
    /// <param name="random">The given random instance</param>
    /// <param name="min">The inclusive minimum bound</param>
    /// <param name="max">The exclusive maximum bound.  Must be greater than min</param>
    public static long NextLong(this Random random, long min, long max)
    {
      if (max <= min)
        throw new ArgumentOutOfRangeException("max", "max must be > min!");

      // Defer to the int implementation if we don't need to handle longs.
      if (min > int.MinValue && max < int.MaxValue)
      {
        return random.Next((int)min, (int)max);
      }

      //Working with ulong so that modulo works correctly with values > long.MaxValue
      ulong uRange = (ulong)(max - min);

      //Prevent a modolo bias; see https://stackoverflow.com/a/10984975/238419
      //for more information.
      //In the worst case, the expected number of calls is 2 (though usually it's
      //much closer to 1) so this loop doesn't really hurt performance at all.
      ulong ulongRand;
      do
      {
        byte[] buf = new byte[8];
        random.NextBytes(buf);
        ulongRand = (ulong)BitConverter.ToInt64(buf, 0);
      } while (ulongRand > ulong.MaxValue - ((ulong.MaxValue % uRange) + 1) % uRange);

      return (long)(ulongRand % uRange) + min;
    }

    /// <summary>
    /// Returns a random long from 0 (inclusive) to max (exclusive)
    /// </summary>
    /// <param name="random">The given random instance</param>
    /// <param name="max">The exclusive maximum bound.  Must be greater than 0</param>
    public static long NextLong(this Random random, long max)
    {
      return random.NextLong(0, max);
    }

    /// <summary>
    /// Returns a random long over all possible values of long (except long.MaxValue, similar to
    /// random.Next())
    /// </summary>
    /// <param name="random">The given random instance</param>
    public static long NextLong(this Random random)
    {
      return random.NextLong(long.MinValue, long.MaxValue);
    }

    public static T Choice<T>(this Random random, IReadOnlyList<T> choices)
    {
      return choices == null || choices.Count == 0
          ? throw new ArgumentNullException(nameof(choices), "Choices cannot be null or empty.")
          : choices[random.Next(choices.Count)];
    }

    public static T? Choice<T>(this Random random, IEnumerable<T> choices)
      => random.Choice(choices, Enumerable.Repeat(1, choices.Count()));

    public static T? Choice<T>(this Random random, IEnumerable<T> choices, IEnumerable<int> weights)
    {
      var cumulativeWeight = new List<int>();
      int last = 0;
      foreach (var cur in weights)
      {
        last += cur;
        cumulativeWeight.Add(last);
      }
      int choice = random.Next(last);
      int i = 0;
      foreach (var cur in choices)
      {
        if (choice < cumulativeWeight[i])
        {
          return cur;
        }
        i++;
      }
      return default;
    }
  }
}