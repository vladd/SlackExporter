using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SlackExporter
{
    class RemoteCache : IDisposable
    {
        Dictionary<string, string> UriToFile;
        string cacheRegistryPath, cacheDir;

        public RemoteCache(string cacheRegistryPath, string cacheDir)
        {
            this.cacheRegistryPath = cacheRegistryPath;
            this.cacheDir = cacheDir;
            Directory.CreateDirectory(cacheDir);
            try
            {
                using (var reader = File.OpenText(cacheRegistryPath))
                    UriToFile = new JsonSerializer().Deserialize<Dictionary<string, string>>(new JsonTextReader(reader));
            }
            catch (FileNotFoundException)
            {
                UriToFile = new Dictionary<string, string>();
            }
        }

        HttpClient httpClient = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = true });

        public string Cache(string remoteUri, string suggestedName)
        {
            if (UriToFile.TryGetValue(remoteUri, out var path))
            {
                //Console.WriteLine($"Uri <{remoteUri}> found as {path}");
                return Path.Combine(cacheDir, path);
            }

            var candidateName = suggestedName;
            var baseName = Path.GetFileNameWithoutExtension(suggestedName); // with or without dir?
            var ext = Path.GetExtension(suggestedName);
            for (int i = 1; File.Exists(Path.Combine(cacheDir, candidateName)); i++)
                candidateName = $"{baseName}.{i}{ext}";

            var fullTargetName = Path.Combine(cacheDir, candidateName);

            //TODO: need to sanitize file name (security!)
            Console.WriteLine($"Downloading <{remoteUri}> to {candidateName}");
            var response = httpClient.GetAsync(remoteUri).Result;
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Download failed because: {response.ReasonPhrase}");
                throw new Exception();
            }

            var content = response.Content;
            using (var outf = File.Create(fullTargetName))
                content.CopyToAsync(outf).Wait();

            Console.WriteLine("Download finished successfully");
            UriToFile[remoteUri] = candidateName;
            WriteBack();
            return fullTargetName;
        }

        void WriteBack()
        {
            using (var writer = File.CreateText(cacheRegistryPath))
                new JsonSerializer().Serialize(writer, UriToFile);
        }

        public void Dispose()
        {
            WriteBack();
        }
    }
}
