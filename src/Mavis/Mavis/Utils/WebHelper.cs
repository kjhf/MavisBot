using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mavis.Utils
{
  public static partial class WebHelper
  {
    private static HttpClient HttpClient { get; } = new HttpClient();

    /// <summary>
    /// Simple URL string match
    /// </summary>
    public static readonly Regex URL_REGEX = UrlRegex();

    /// <summary>
    /// Get JSON from a website and optionally specify the cookie headers.
    /// </summary>
    public static async Task<object?> GetJsonAsync(string website, string? cookies = null)
    {
      if (cookies != null)
      {
        HttpClient.DefaultRequestHeaders.Add("Cookie", cookies);
      }

      HttpResponseMessage response = await HttpClient.GetAsync(website).ConfigureAwait(false);
      response.EnsureSuccessStatusCode();

      string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
      return JsonSerializer.Deserialize<JsonElement>(json);
    }

    /// <summary>
    /// Download text from a website synchronously.
    /// Call GetTextAsync where possible to avoid deadlocks.
    /// </summary>
    public static string GetText(string website)
    {
      HttpResponseMessage response = HttpClient.GetAsync(website).Result;
      response.EnsureSuccessStatusCode();

      return response.Content.ReadAsStringAsync().Result;
    }

    /// <summary>
    /// Download text from a website asynchronously.
    /// </summary>
    public static async Task<string> GetTextAsync(string website)
    {
      HttpResponseMessage response = await HttpClient.GetAsync(website).ConfigureAwait(false);
      response.EnsureSuccessStatusCode();

      return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Download a file from the given URL. Optionally specify the maximum size to download (default is 4MB).
    /// Returns a tuple of the Http Response, and the image bytes or null.
    /// </summary>
    /// <param name="url"></param>
    /// <param name="maximumSize"></param>
    public static async Task<Tuple<HttpResponseMessage, byte[]?>> DownloadFile(string url, int maximumSize = 0x400000)
    {
      using var client = new HttpClient();
      client.MaxResponseContentBufferSize = maximumSize;
      client.Timeout = TimeSpan.FromSeconds(3);
      using HttpResponseMessage result = await client.GetAsync(url, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
      if (result.IsSuccessStatusCode)
      {
        return new Tuple<HttpResponseMessage, byte[]?>(result, await result.Content.ReadAsByteArrayAsync());
      }
      return new(result, null);
    }

    /// <summary>
    /// Download a file from the given URL and write to the specified file path.
    /// Optionally specify the maximum size to download (default is 4MB).
    /// Returns the Http Response.
    /// </summary>
    public static async Task<HttpResponseMessage> DownloadAndWriteFile(string url, string filePath, int maximumSize = 0x400000)
    {
      using var client = new HttpClient();
      client.MaxResponseContentBufferSize = maximumSize;
      client.Timeout = TimeSpan.FromSeconds(3);
      using HttpResponseMessage result = await client.GetAsync(url, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
      if (result.IsSuccessStatusCode)
      {
        await File.WriteAllBytesAsync(filePath, await result.Content.ReadAsByteArrayAsync()).ConfigureAwait(false);
      }
      return result;
    }

    /// <summary>
    /// Return if the url is an image (content type's media is an image/)
    /// </summary>
    public static async Task<bool> IsImageUrlAsync(string URL)
    {
      HttpRequestMessage request = new(HttpMethod.Head, URL);
      HttpResponseMessage response = await HttpClient.SendAsync(request).ConfigureAwait(false);
      response.EnsureSuccessStatusCode();

      string? contentType = response.Content.Headers.ContentType?.MediaType;
      return contentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true;
    }

    [GeneratedRegex("(www|http:|https:)+[^\\s]+[\\w]", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled, "en-GB")]
    private static partial Regex UrlRegex();
  }
}