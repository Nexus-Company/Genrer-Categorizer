using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Net;

namespace Nexus.Spotify.Client;

public static class Utils
{
    public static async Task<SpotifyClient> GetConsoleClientAsync(IConfiguration config)
    {
        var spotifyConfig = config.GetSection("Spotify");

        string[] scopes = spotifyConfig.GetSection("Scopes")
            .GetChildren()
            .Select(x => x.Value)
            .ToArray()!;

        string clientId = spotifyConfig["ClientId"]!,
           secret = spotifyConfig["Secret"]!,
           link = OAuthCredential.GenerateOAuthLink(clientId, null, scopes);

        Process.Start(new ProcessStartInfo(link)
        {
            UseShellExecute = true
        });

        var credential = await OAuthCredential
            .GetCredentialAsync(clientId, secret, GetCode(), scopes);

        return new(credential);
    }

    private static string GetCode()
    {
        var uri = new Uri(OAuthCredential.RedirectUri);

        if (!string.IsNullOrEmpty(uri.PathAndQuery))
            uri = new(uri.OriginalString.Replace(uri.PathAndQuery, "/"));

        using HttpListener listner = new();
        listner.Prefixes.Add(uri.AbsoluteUri);
        listner.Start();

        var ctx = listner.GetContext();

        var code = ctx.Request.QueryString["code"] ?? string.Empty;

        if (!string.IsNullOrEmpty(code))
        {
            var fs = File.OpenRead(Path.Combine(Environment.CurrentDirectory, "Resources/EndLogin.html"));
            ctx.Response.ContentType = "text/html; charset=\"utf-8\"";
            fs.CopyTo(ctx.Response.OutputStream);
            ctx.Response.Close();
        }

        listner.Close();

        return code;
    }
}

