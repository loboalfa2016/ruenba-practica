using Microsoft.AspNetCore.Identity;
using FrameRent.Application.DTOs.Auth;
using FrameRent.Application.Interfaces;

namespace FrameRent.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(UserManager<IdentityUser> userManager, IJwtTokenGenerator jwtTokenGenerator)
    {
        _userManager = userManager;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        var user = new IdentityUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new ArgumentException($"User registration failed: {errors}");
        }

        // Functional Requirement: "Quien se registra lo hace siempre como cliente."
        await _userManager.AddToRoleAsync(user, "Client");

        var roles = new List<string> { "Client" };
        var token = _jwtTokenGenerator.GenerateToken(user.Id, user.Email!, roles);

        return new AuthResponse(token, user.Email!, "Client", user.Id);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? "Client";

        var token = _jwtTokenGenerator.GenerateToken(user.Id, user.Email!, roles);

        return new AuthResponse(token, user.Email!, primaryRole, user.Id);
    }
}
