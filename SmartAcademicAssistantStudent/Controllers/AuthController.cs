using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartAcademicAssistantStudent.Data;
using SmartAcademicAssistantStudent.Entities;
using SmartAcademicAssistantStudent.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SmartAcademicAssistantStudent.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(AppDbContext context, IConfiguration configuration) : ControllerBase
    {
        // ─── Register ───────────────────────────────────────────
        [HttpPost("register")]
        public async Task<IActionResult> Register(UserDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Missing data");

            if (await context.Users.AnyAsync(x => x.Name == request.Name || x.Email == request.Email))
                return BadRequest("Username or Email already exists");

            var user = new User();
            user.Name = request.Name;
            user.Email = request.Email;
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, request.Password);
            user.Role = "Student"; // افتراضي

            context.Users.Add(user);
            await context.SaveChangesAsync();

            return Ok(new { user.Id, user.Name, user.Email, user.Role });
        }

        // ─── Login ──────────────────────────────────────────────
        [HttpPost("login")]
        public async Task<ActionResult<TokenResponseDto>> Login(UserDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Missing data");

            var user = await context.Users.FirstOrDefaultAsync(x => x.Name == request.Name);
            if (user is null) return BadRequest("Username not found");

            if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password)
                == PasswordVerificationResult.Failed)
                return BadRequest("Wrong password");

            return Ok(new TokenResponseDto
            {
                AccessToken = CreateToken(user),
                RefreshToken = await GenerateAndSaveRefreshToken(user)
            });
        }

        // ─── Refresh Token ──────────────────────────────────────
        [HttpPost("refresh-token")]
        public async Task<ActionResult<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto request)
        {
            var resault = await ValidateRefreshToken(request.UserId, request.RefreshToken);
            if (resault == null) return Unauthorized();
            var response = new TokenResponseDto
            {
                AccessToken = CreateToken(resault),
                RefreshToken = await GenerateAndSaveRefreshToken(resault)
            };
            return Ok(response);

        }
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // إذا الـ claim مش موجود أو مش رقم — أحسن تتعامل معه
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            var user = await context.Users.FindAsync(userId);
            if (user is null) return Unauthorized();

            user.RefreshToken = null;
            user.RefreshTokenExpireTime = null;
            await context.SaveChangesAsync();

            return Ok("Logged out successfully");
        }


        // ─── Private Helpers ────────────────────────────────────
        private async Task<User?> ValidateRefreshToken(int userId, string refreshToken)
        {
            var user = await context.Users.FindAsync(userId);
            if (user is null ||
                user.RefreshToken != refreshToken ||
                user.RefreshTokenExpireTime < DateTime.UtcNow)
                return null;
            return user;
        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private async Task<string> GenerateAndSaveRefreshToken(User user)
        {
            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpireTime = DateTime.UtcNow.AddDays(7);
            await context.SaveChangesAsync();
            return refreshToken;
        }

        private string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration.GetValue<string>("AppSettings:Token")!));

            var token = new JwtSecurityToken(
                issuer: configuration.GetValue<string>("AppSettings:Issuer"),
                audience: configuration.GetValue<string>("AppSettings:Audience"),
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha512)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}