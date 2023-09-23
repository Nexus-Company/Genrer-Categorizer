using System.Net.Http.Headers;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Nexus.Spotify.Client;
public class OAuthCredential
{
    public const string TokenEndpoint = "https://accounts.spotify.com/api/token";
    public const string AccountsEndpoint = "https://accounts.spotify.com/pt-BR/authorize?";
    public const string RedirectUri = "https://localhost:44383/spotify/callback";

    private string
        _clientId = null!,
        _secret = null!;
    [JsonProperty("access_token")] public string Token { get; set; } = null!;
    [JsonProperty("token_type")] public string Type { get; set; } = null!;
    [JsonProperty("refresh_token")] private string Refresh { get; set; } = null!;
    [JsonProperty("expires_in")] public double ExpiresIn { get; set; }


    private void ConfigureRefresh(string clientId, string secret)
    {
        _clientId = clientId;
        _secret = secret;

        var timer = new Timer((ExpiresIn - 10) * 1000);
        timer.Elapsed += RefreshToken;
        timer.Start();
    }

    private async void RefreshToken(object? sender, ElapsedEventArgs elapsedEventArgs)
    {
        using HttpClient client = new();

        var request = new HttpRequestMessage()
        {
            RequestUri = new Uri(TokenEndpoint),
            Method = HttpMethod.Post,
            Content = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "grant_type", "refresh_token" },
                { "client_id", _clientId },
                { "client_secret", _secret },
                { "refresh_token", Refresh }
            })
        };

        var response = await client.SendAsync(request);

        var obj = JsonConvert.DeserializeObject<OAuthCredential>(await response.Content.ReadAsStringAsync());

        Token = obj!.Token;
        Refresh = obj!.Refresh;
        ExpiresIn = obj!.ExpiresIn;
    }

    internal AuthenticationHeaderValue GetHeader()
        => new(Type, Token);


    public static async Task<OAuthCredential> GetCredentialAsync(string clientId, string secret, string code, string[] scopes)
    {
        Exception excp = new ArgumentException("Authentication OAuth 2.0 failed.");
        using HttpClient client = new();

        var request = new HttpRequestMessage()
        {
            RequestUri = new Uri(TokenEndpoint),
            Method = HttpMethod.Post,
            Content = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "grant_type", "authorization_code" },
                { "client_id", clientId },
                { "client_secret", secret },
                { "code", code },
                { "scope", string.Join(" ", scopes) },
                { "redirect_uri", RedirectUri }
            })
        };

        var response = await client.SendAsync(request);

        var text = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw excp;

        var credential = JsonConvert.DeserializeObject<OAuthCredential>(text) ??
                                   throw excp;

        credential.ConfigureRefresh(clientId, secret);

        return credential;
    }

    public static string GenerateOAuthLink(string clientId, string? state, string[] scopes)
        => AccountsEndpoint +
                    $"client_id={clientId}&" +
                    $"redirect_uri={RedirectUri}&" +
                    $"response_type=code&" +
                    $"state={state ?? string.Empty}&" +
                    $"scope={string.Join(" ", scopes)}";
    public override string ToString()
        => $"{Type}  {Token}";
}