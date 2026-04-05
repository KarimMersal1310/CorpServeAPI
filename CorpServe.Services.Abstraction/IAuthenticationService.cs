using CorpServe.Shared.DTOs.AuthDTOs;
using CorpServe.Shared.CommonResult;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Services.Abstraction
{
    public interface IAuthenticationService
    {
        // Register
        // => FullName , Email, Phone, Password, ConfirmPassword, Role (Client or Vendor) Return FullName , Token , Role
        Task<Result<AuthResponseDTO>> RegisterAsync(RegisterDTO registerDTO);
        // Login 
        // => Email , Password Return FullName , Token , Role
        Task<Result<LoginResponseDTO>> LoginAsync(LoginDTO loginDTO);
        // RefreshToken
        // => RefreshToken => return new access token + new refresh token
        Task<Result<LoginResponseDTO>> RefreshTokenAsync(RefreshTokenRequestDTO request);
        // RevokeRefreshToken
        // => RefreshToken => return True or False
        Task<Result<bool>> RevokeRefreshTokenAsync(RevokeRefreshTokenRequestDTO request);
        // GetCurrentUserProfile
        // => Return FullName, Email, PhoneNumber, Role
        Task<Result<UserProfileDTO>> GetUserProfileAsync(string userId);
        // UpdateUser
        // => FullName , Email , Phone => return True or False
        Task<Result<bool>> UpdateUserAsync(string UserId , UpdateUserDTO updateUserDTO);
        // ChangePassword
        // => OldPassword , NewPassword , ConfirmNewPassword => return True or False
        Task<Result<bool>> ChangePasswordAsync(string UserId , ChangePasswordDTO changePasswordDTO);
        // ForgotPassword
        // => Email => return True or False sent email with reset password link
        Task<Result<bool>> ForgotPasswordAsync(ForgetPasswordDTO forgetPasswordDTO);
        // ResetPassword
        // => Email, Token, NewPassword, ConfirmNewPassword => return True or False
        Task<Result<bool>> ResetPasswordAsync(ResetPasswordDTO resetPasswordDTO);

    }
}
