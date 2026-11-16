using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace SportHubBase.Models
{
    public sealed class UpdateRelease
    {
        public Version Version { get; private set; }
        public string Notes { get; private set; }
        public Uri DownloadUrl { get; private set; }

        public bool IsNewerThan(Version installed) => Version > Normalize(installed);
        public static bool IsValidRepository(string repository) => !string.IsNullOrWhiteSpace(repository)
            && Regex.IsMatch(repository, @"\A[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+\z");

        public static UpdateRelease FromGitHubJson(string json, string repository)
        {
            if (!IsValidRepository(repository)) throw new ArgumentException("Неверно задан репозиторий GitHub.");
            var release = JObject.Parse(json);
            if ((bool?)release["draft"] == true || (bool?)release["prerelease"] == true) return null;
            string tag = ((string)release["tag_name"] ?? "").Trim();
            Version version;
            if (!System.Version.TryParse(tag.TrimStart('v', 'V'), out version))
                throw new InvalidOperationException("Тег релиза GitHub должен содержать номер версии, например v1.2.3.");
            var assets = (release["assets"] as JArray ?? new JArray()).OfType<JObject>().ToList();
            string expectedName = "SportHubBase-" + tag.TrimStart('v', 'V') + ".zip";
            var asset = assets.FirstOrDefault(a => string.Equals((string)a["name"], expectedName, StringComparison.OrdinalIgnoreCase)
                && (string)a["state"] == "uploaded");
            string url = (string)asset?["browser_download_url"];
            Uri download;
            if (!Uri.TryCreate(url, UriKind.Absolute, out download) || download.Scheme != Uri.UriSchemeHttps
                || !string.Equals(download.Host, "github.com", StringComparison.OrdinalIgnoreCase)
                || !download.IsDefaultPort || !string.IsNullOrEmpty(download.UserInfo)
                || !download.AbsolutePath.StartsWith("/" + repository + "/releases/download/", StringComparison.OrdinalIgnoreCase))
            {
                // Без подходящего архива открываем официальный релиз со списком файлов.
                download = new Uri("https://github.com/" + repository + "/releases/tag/" + Uri.EscapeDataString(tag));
            }
            return new UpdateRelease { Version = Normalize(version), Notes = (string)release["body"], DownloadUrl = download };
        }

        private static Version Normalize(Version version) => new Version(version.Major, version.Minor, Math.Max(0, version.Build), Math.Max(0, version.Revision));
    }
}
