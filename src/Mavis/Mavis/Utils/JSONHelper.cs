using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace Mavis.Utils
{
  public static class JSONHelper
  {
    public static async Task<object?> GetJsonAsync(string website, string? cookies = null) => await WebHelper.GetJsonAsync(website, cookies).ConfigureAwait(false);

    public static async Task<object?> LoadJSONFromFile(string filePath) => JsonConvert.DeserializeObject(await File.ReadAllTextAsync(filePath).ConfigureAwait(false));
  }
}