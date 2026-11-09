using System.Net.Http.Headers;
using FrontEnd.Authentication;
 
namespace Frontend.Authentication;

public class JwtAuthHandler : DelegatingHandler
{
    private readonly CustomAuthStateProvider _auth;

    public JwtAuthHandler(CustomAuthStateProvider auth)
    {
        _auth = auth;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = _auth.Token;

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}