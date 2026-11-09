namespace FrontEnd.DTOs;

public record LoginResponse(
    string Token,
    string Username,
    string Role
);