using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace FrontEnd.Authentication;


public class CustomAuthStateProvider
    : AuthenticationStateProvider
{
    private string? _token;
    public string? Token => _token;
    
    public void SetToken(string token)
    {
        _token = token;

        NotifyAuthenticationStateChanged(
            GetAuthenticationStateAsync());
    }

    public void Logout()
    {
        _token = null;

        NotifyAuthenticationStateChanged(
            GetAuthenticationStateAsync());
    }

    public override Task<AuthenticationState>
        GetAuthenticationStateAsync()
    {
        if (string.IsNullOrEmpty(_token))
        {
            return Task.FromResult(
                new AuthenticationState(
                    new ClaimsPrincipal(
                        new ClaimsIdentity())));
        }

        try
        {
            var parts = _token.Split('.');

            if (parts.Length != 3)
                throw new Exception("Invalid token");

            var payload = parts[1]
                .Replace('-', '+')
                .Replace('_', '/');

            payload = payload.PadRight(
                payload.Length + (4 - payload.Length % 4) % 4,
                '=');

            using var document = JsonDocument.Parse(
                Convert.FromBase64String(payload));

            var claims = document.RootElement
                .EnumerateObject()
                .Select(property => new Claim(
                    property.Name,
                    property.Value.ToString()))
                .ToList();

            // Map JWT role claim to Blazor role claim
            var roleClaim = claims.FirstOrDefault(
                c => c.Type == "role" ||
                     c.Type == ClaimTypes.Role);

            if (roleClaim != null &&
                roleClaim.Type != ClaimTypes.Role)
            {
                claims.Remove(roleClaim);
                claims.Add(new Claim(
                    ClaimTypes.Role,
                    roleClaim.Value));
            }

            var identity = new ClaimsIdentity(
                claims,
                "jwt",
                ClaimTypes.Name,
                ClaimTypes.Role);

            return Task.FromResult(
                new AuthenticationState(
                    new ClaimsPrincipal(identity)));
        }
        catch
        {
            _token = null;

            return Task.FromResult(
                new AuthenticationState(
                    new ClaimsPrincipal(
                        new ClaimsIdentity())));
        }
    }
}