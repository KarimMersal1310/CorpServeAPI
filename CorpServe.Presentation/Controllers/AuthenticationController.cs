using CorpServe.Services.Abstraction;
using CorpServe.Shared.DTOs.AuthDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Presentation.Controllers
{
    public class AuthenticationController : ApiBaseController
    {
        private readonly IAuthenticationService _authenticationService;

        public AuthenticationController(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDTO>> RegisterAsync(RegisterDTO registerDTO)
        {
            var result = await _authenticationService.RegisterAsync(registerDTO);
            return HandleResult(result);
        }
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDTO>> LoginAsync(LoginDTO loginDTO)
        {
            var result = await _authenticationService.LoginAsync(loginDTO);
            return HandleResult(result);
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
    }
}
