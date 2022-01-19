using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mavis.Utils
{
  public static class WebHelper
  {
    private static WebClient? _client;
    private static WebClient WebClient => _client ??= new WebClient { Encoding = Encoding.UTF8 };

    /// <summary>
    /// Simple URL string match
    /// </summary>
    public static readonly Regex URL_REGEX = new Regex(@"(www|http:|https:)+[^\s]+[\w]", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Get JSON from a website and optionally specify the cookie headers.
    /// </summary>
    /// <param name="website"></param>
    /// <param name="cookies"></param>
    /// <returns></returns>
    public static async Task<object?> GetJsonAsync(string website, string? cookies = null)
    {
      if (cookies != null)
      {
        WebClient.Headers.Add(HttpRequestHeader.Cookie, cookies);
      }

      Uri uri = new Uri(website);
      string value = await WebClient.DownloadStringTaskAsync(uri).ConfigureAwait(false);
      return JsonConvert.DeserializeObject(value);
    }

    /// <summary>
    /// Download text from a website synchronously.
    /// </summary>
    /// <param name="website"></param>
    /// <returns></returns>
    public static string GetText(string website) => WebClient.DownloadString(new Uri(website));

    /// <summary>
    /// Download text from a website asynchronously.
    /// </summary>
    /// <param name="website"></param>
    /// <returns></returns>
    public static async Task<string> GetTextAsync(string website) => await WebClient.DownloadStringTaskAsync(new Uri(website)).ConfigureAwait(false);

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
    /// Return if the url is an image (jpeg, jpg, png, tff, gif) or the content type starts with image/
    /// </summary>
    /// <param name="URL"></param>
    /// <returns></returns>
    public static bool IsImageUrl(string URL)
    {
      // First check the URL extension ending
      if (URL.Length > 4)
      {
        string urlExtension = URL.Substring(URL.Length - 5).ToLowerInvariant();
        if (urlExtension.EndsWith(".jpeg") || URL.EndsWith(".jpg") || URL.EndsWith(".png") || URL.EndsWith(".tff") || URL.EndsWith(".gif"))
        {
          return true;
        }
      }

      // If ambiguous, make a request to ask the type of the page.
      var req = (HttpWebRequest)WebRequest.Create(URL);
      req.Method = "HEAD";
      req.AllowAutoRedirect = true;
      req.Timeout = 3000;
      using (var resp = req.GetResponse())
      {
        string contentType = resp.ContentType;
        return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) || contentType.Equals("jpeg") || contentType.Equals("png");
      }
    }
  }
}