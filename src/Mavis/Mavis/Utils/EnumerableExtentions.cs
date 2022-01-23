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
  }
}