using System.Globalization;
using TheOtherRoles.Modules.Languages;

namespace TheOtherRoles.Helper;

public static class DownloadHelper
{
    public static bool IsCN()
    {
        return RegionInfo.CurrentRegion.ThreeLetterISORegionName == "CHN" || LanguageManager.Instance.CurrentLang == SupportedLangs.SChinese;
    }

    public static string GithubUrl(this string url)
    {
        if (IsCN() && !url.Contains("github.moeyy.xyz"))
        {
            if (url.Contains("github.com"))
            {
                return url.Replace("https://github.com", "https://github.moeyy.xyz/https://github.com");
            }

            if (url.Contains("raw.githubusercontent.com"))
            {
                return url.Replace("https://raw.githubusercontent.com", "https://github.moeyy.xyz/https://raw.githubusercontent.com");
            }
        }

        return url;
    }
}