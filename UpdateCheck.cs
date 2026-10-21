using System;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

// Update check: reads Night-Night's page on teamwolfgamers.online - the same "Latest Version"
// and Download button the page shows. The website fills those in from the download's file name
// when it is published ("push to website.bat"), so publishing a new build is all it takes for
// every copy of the app to see it. The duckdns name is tried second, in case the domain lapses.
// Nothing is downloaded here: the app offers to open the download in the browser.
//
// Written for the C# 5 compiler that ships with Windows (build.bat), so no newer syntax.
static class UpdateCheck
{
    const string PageKey = "night-night";
    static readonly string[] Pages =
    {
        "https://teamwolfgamers.online/" + PageKey,
        "https://pattywatty.duckdns.org/" + PageKey,
    };

    public class Offer
    {
        public string Version;   // "1.0.1"
        public string Download;  // full URL of the file, or the page if it has none
    }

    /// <summary>This build's version, from version.txt (stamped in by build.bat), as "1.0.0".</summary>
    public static string CurrentVersion
    {
        get
        {
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            return string.Format("{0}.{1}.{2}", v.Major, v.Minor, Math.Max(v.Build, 0));
        }
    }

    /// <summary>What the website offers, or null if neither address answers. Blocks: run it off the UI thread.</summary>
    public static Offer Check()
    {
        // .NET Framework only turns TLS 1.2 on by itself on newer Windows builds.
        ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
        foreach (string page in Pages)
        {
            try
            {
                string html;
                using (WebClient web = new WebClient())
                {
                    web.Headers[HttpRequestHeader.UserAgent] = "Night-Night/" + CurrentVersion;
                    web.Encoding = System.Text.Encoding.UTF8;
                    html = web.DownloadString(page);
                }
                Match ver = Regex.Match(html, "data-version=\"" + PageKey + "\">\\s*V?([^<\\s]+)\\s*<");
                if (!ver.Success) continue;

                Offer offer = new Offer();
                offer.Version = ver.Groups[1].Value;
                offer.Download = page;
                Match link = Regex.Match(html, "data-download=\"" + PageKey + "\" href=\"([^\"]+)\"");
                if (link.Success && !link.Groups[1].Value.TrimEnd('/').EndsWith("downloads"))
                    offer.Download = new Uri(new Uri(page), link.Groups[1].Value).ToString();
                return offer;
            }
            catch (Exception)
            {
                // try the other address
            }
        }
        return null;
    }

    /// <summary>True if <paramref name="remote"/> is a higher version than this build.</summary>
    public static bool IsNewer(string remote)
    {
        int[] a = Parts(remote), b = Parts(CurrentVersion);
        for (int i = 0; i < 4; i++)
            if (a[i] != b[i]) return a[i] > b[i];
        return false;
    }

    static int[] Parts(string text)
    {
        int[] parts = new int[4];
        MatchCollection nums = Regex.Matches(text ?? "", "\\d+");
        for (int i = 0; i < nums.Count && i < 4; i++)
            int.TryParse(nums[i].Value, out parts[i]);
        return parts;
    }
}
