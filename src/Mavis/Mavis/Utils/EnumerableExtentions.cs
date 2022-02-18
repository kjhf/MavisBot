using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Mavis.Utils
{
  public static class EnumerableExtentions
  {
    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    [return: NotNullIfNotNull("defaultValue")]
    public static TValue? Get<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue? defaultValue = default)
      => key == null || !dictionary.TryGetValue(key, out var value) ? defaultValue : value;

    /// <summary>
    /// Gets the value associated with the specified key and box/un-box correctly.
    /// </summary>
    [return: NotNullIfNotNull("defaultValue")]
    public static TTarget? GetWithConversion<TTarget, TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TTarget? defaultValue = default) where TTarget : TValue
      => (TTarget?)Convert.ChangeType(Get(dictionary, key, defaultValue), typeof(TTarget?), CultureInfo.InvariantCulture);

    /// <summary>
    /// Add an entry to the dictionary key's collection or begin the key's collection with the value like a setdefault dict.
    /// </summary>
    public static void AddOrAppend<TKey, TValueCollection, TValue>(this IDictionary<TKey, TValueCollection> dictionary, TKey key, TValue value) where TValueCollection : ICollection<TValue>, new()
    {
      bool hasKey = dictionary.ContainsKey(key);
      if (hasKey)
      {
        if (dictionary[key] != null)
        {
          dictionary[key].Add(value);
        }
        else
        {
          dictionary[key] = new TValueCollection { value };
        }
      }
      else
      {
        dictionary[key] = new TValueCollection { value };
      }
    }

    /// <summary>
    /// Returns the list after sorting.
    /// Sorts the elements in the entire List using the default comparer.
    /// </summary>
    public static List<T> SortInline<T>(this List<T> list, bool reverse = false)
    {
      if (reverse)
      {
        list.Sort();
        list.Reverse();
      }
      else
      {
        list.Sort();
      }
      return list;
    }
  }
}