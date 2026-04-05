using CorpServe.Services.Abstraction;
using CorpServe.Shared.DTOs.AuthDTOs;
using CorpServe.Shared.CommonResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CorpServe.Presentation.Controllers
{
    public class AuthenticationController : ApiBaseController
    {
        private const string RefreshTokenCookieName = "corpserve_refresh_token";
        private readonly IAuthenticationService _authenticationService;

        public AuthenticationController(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDTO>> RegisterAsync(RegisterDTO registerDTO)
        {
            var result = await _authenticationService.RegisterAsync(registerDTO);
            if (result.IsSuccess)
            {
                SetRefreshTokenCookie(result.Value.RefreshToken, result.Value.RefreshTokenExpiresAtUtc);
                result.Value.RefreshToken = string.Empty;
            }

            return HandleResult(result);
        }
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDTO>> LoginAsync(LoginDTO loginDTO)
        {
            var result = await _authenticationService.LoginAsync(loginDTO);
            if (result.IsSuccess)
            {
                SetRefreshTokenCookie(result.Value.RefreshToken, result.Value.RefreshTokenExpiresAtUtc);
                result.Value.RefreshToken = string.Empty;
            }

            return HandleResult(result);
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<LoginResponseDTO>> RefreshTokenAsync()
        {
            if (!Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken) || string.IsNullOrWhiteSpace(refreshToken))
                return HandleResult(Result<LoginResponseDTO>.Fail(Error.Unauthorized("Auth.RefreshTokenRequired", "Refresh token is required.")));

            var result = await _authenticationService.RefreshTokenAsync(new RefreshTokenRequestDTO { RefreshToken = refreshToken });
            if (result.IsSuccess)
            {
                SetRefreshTokenCookie(result.Value.RefreshToken, result.Value.RefreshTokenExpiresAtUtc);
                result.Value.RefreshToken = string.Empty;
            }

            return HandleResult(result);
        }

        [HttpPost("revoke-refresh-token")]
        public async Task<ActionResult<bool>> RevokeRefreshTokenAsync()
        {
            if (!Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken) || string.IsNullOrWhiteSpace(refreshToken))
            {
                DeleteRefreshTokenCookie();
                return Ok(true);
            }

            var result = await _authenticationService.RevokeRefreshTokenAsync(new RevokeRefreshTokenRequestDTO { RefreshToken = refreshToken });
            DeleteRefreshTokenCookie();
            return result.IsSuccess ? Ok(true) : HandleResult(result);
        }
        

        [HttpPost("forgot-password")]
        public async Task<ActionResult<bool>> ForgotPasswordAsync(ForgetPasswordDTO forgetPasswordDTO)
        {
            var result = await _authenticationService.ForgotPasswordAsync(forgetPasswordDTO);
            return HandleResult(result);
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult<bool>> ResetPasswordAsync(ResetPasswordDTO resetPasswordDTO)
        {
            var result = await _authenticationService.ResetPasswordAsync(resetPasswordDTO);
            return HandleResult(result);
        }

        private void SetRefreshTokenCookie(string refreshToken, DateTime expiresAtUtc)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = expiresAtUtc,
                Path = "/"
            };

            Response.Cookies.Append(RefreshTokenCookieName, refreshToken, options);
        }

        private void DeleteRefreshTokenCookie()
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/"
            };

            Response.Cookies.Delete(RefreshTokenCookieName, options);
        }
    }
}
