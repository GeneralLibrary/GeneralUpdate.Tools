using Newtonsoft.Json;
using System.IO;
using System.Net.Http;

namespace GeneralUpdate.Packet.Utils
{
    internal class HttpUtil
    {
        public static async Task PostTaskAsync<T>(string httpUrl, Dictionary<string, string> parameters, string filePath,Action<T> callbackAction)
        {
            var uri = new Uri(httpUrl);
            using (var client = new HttpClient())
            using (var content = new MultipartFormDataContent())
            {
                foreach (var parameter in parameters)
                {
                    var stringContent = new StringContent(parameter.Value);
                    content.Add(stringContent, parameter.Key);
                }

                if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
                {
                    var fileStream = File.OpenRead(filePath);
                    var fileInfo = new FileInfo(filePath);
                    var fileContent = new StreamContent(fileStream);
                    content.Add(fileContent, "file", Path.GetFileName(filePath));
                }

                var result = await client.PostAsync(uri, content);
                var reseponseJson = await result.Content.ReadAsStringAsync();
                callbackAction.Invoke(JsonConvert.DeserializeObject<T>(reseponseJson));
            }
        }
    }
}
