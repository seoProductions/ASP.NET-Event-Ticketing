using BackEnd.DatabaseModels;
using BackEnd.DTOs;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace BackEnd;

public static class AuthEndpoints
{
    public static void RegisterAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/login", Login);
    }

    static async Task<IResult> Login(
        LoginRequest request,
        AppDbContext db,
        IConfiguration config)
    {
        // Find user
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        // Verify password
        if (user == null ||
            !BCrypt.Net.BCrypt.Verify(
                request.Password,
                user.PasswordHash))
        {
            return Results.Unauthorized();
        }

        // Validate role
        var role = user.Role.Trim().ToLowerInvariant();

        if (role != "admin" &&
            role != "organizer" &&
            role != "customer")
        {
            return Results.Forbid();
        }

        // Create JWT claims
        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new Claim(
                ClaimTypes.Name,
                user.Username),

            new Claim(
                ClaimTypes.Role,
                role)
        };

        // Signing key
        var key = new SymmetricSecurityKey(
            Convert.FromBase64String(config["Jwt:Key"]!));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        // Generate JWT
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler()
            .WriteToken(token);

        return Results.Ok(new LoginResponse(
            tokenString,
            user.Username,
            role
        ));
    }
}