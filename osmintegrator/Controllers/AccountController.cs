using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using osmintegrator.Interfaces;
using osmintegrator.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TS.Mobile.WebApp.Models;

namespace osmintegrator.Controllers
{
    [EnableCors("AllowOrigin")]
    [Authorize]
    [Route("api/[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;

        public AccountController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IConfiguration configuration
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<string> Protected()
        {
            return await System.Threading.Tasks.Task.Run(() => { return "Protected area"; });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<string> NoProtected()
        {
            return await System.Threading.Tasks.Task.Run(() => { return "No protected area"; });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IAuthenticationResponse> Login([FromBody] LoginData model)
        {
            AuthenticationResponse authResponse = new AuthenticationResponse();
            try
            {
                var result = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, false, false);

                if (result.Succeeded)
                {
                    var appUser = _userManager.Users.SingleOrDefault(r => r.UserName == model.UserName);
                    authResponse.IsSuccess = true;
                    authResponse.TokenData = GenerateJwtToken(model.UserName, appUser);
                }
                else
                {
                    authResponse.IsSuccess = false;
                    authResponse.ErrorMsg = "The username or password were not correct. Try again.";
                }
            }
            catch (Exception ex)
            {
                authResponse.IsSuccess = false;
                authResponse.ErrorMsg = ex.Message;
            }

            return authResponse;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IAuthenticationResponse> Refresh([FromBody] TokenData refreshTokenData)
        {
            AuthenticationResponse authResponse = new AuthenticationResponse();
            try
            {
                var principal = await System.Threading.Tasks.Task.Run(() => { return GetPrincipalFromExpiredToken(refreshTokenData.Token); });

                var username = ((ClaimsIdentity)(principal.Identity)).Claims.First(n => n.Type == "sub").Value;
                var savedRefreshToken = ((ClaimsIdentity)(principal.Identity)).Claims.First(n => n.Type == "refresh_token").Value;

                if (savedRefreshToken != refreshTokenData.RefreshToken)
                {
                    authResponse.IsSuccess = false;
                    authResponse.ErrorMsg = "Invalid refresh token";
                }
                else
                {
                    var appUser = _userManager.Users.SingleOrDefault(r => r.UserName == username);
                    authResponse.IsSuccess = true;
                    authResponse.TokenData = GenerateJwtToken(username, appUser);
                }

            }
            catch (Exception ex)
            {
                authResponse.IsSuccess = false;
                authResponse.ErrorMsg = ex.Message;
            }

            return authResponse;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IAuthenticationResponse> Register([FromBody] RegisterData model)
        {
            AuthenticationResponse authResponse = new AuthenticationResponse();
            try
            {
                var user = new IdentityUser
                {
                    UserName = model.UserName
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, false);
                    authResponse.IsSuccess = true;
                    authResponse.TokenData = GenerateJwtToken(model.UserName, user);
                }
                else
                {
                    authResponse.IsSuccess = false;
                    authResponse.ErrorMsg = result.Errors.FirstOrDefault().Code + " " + result.Errors.FirstOrDefault().Description;
                }

            }
            catch (Exception ex)
            {
                authResponse.IsSuccess = false;
                authResponse.ErrorMsg = ex.Message;
            }


            return authResponse;
        }


        private TokenData GenerateJwtToken(string userName, IdentityUser user)
        {
            string newRefreshToken = GenerateRefreshToken();

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim("refresh_token", newRefreshToken)
            };

            ApplyClaimsForContextUser(claims);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JwtExpireMinutes"]));

            var token = new JwtSecurityToken(
                _configuration["JwtIssuer"],
                _configuration["JwtIssuer"],
                claims,
                expires: expires,
                signingCredentials: creds
            );

            string result = (new JwtSecurityTokenHandler()).WriteToken(token);

            return new TokenData()
            {
                Token = result,
                RefreshToken = newRefreshToken
            };
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = _configuration["JwtIssuer"],
                ValidAudience = _configuration["JwtIssuer"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtKey"])),
                ClockSkew = TimeSpan.Zero,
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }

        private void ApplyClaimsForContextUser(List<Claim> claims)
        {
            var identity = new ClaimsIdentity(claims);
            _signInManager.Context.User = new ClaimsPrincipal(identity);
        }
    }
}
