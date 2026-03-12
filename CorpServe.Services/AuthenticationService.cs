using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Domain.Entities.SpecializedCategoryModule;
using CorpServe.Services.Abstraction;
using CorpServe.Shared.DTOs.AuthDTOs;
using E_Commerce.Shared.CommonResult;
using EventHub.Domain.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;

        public AuthenticationService(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IEmailService emailService,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _configuration = configuration;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> RegisterAsync(RegisterDTO registerDTO)
        {
            if (registerDTO.Role == "Admin")
                return Error.Validation("User.InvalidRole", "Admin role cannot be assigned during registration.");

            if (registerDTO.Role != "Client" && registerDTO.Role != "Vendor")
                return Error.Validation("User.InvalidRole", "Role must be Client Or Vendor");

            var selectedCategoryIds = new List<string>();
            if (registerDTO.Role == "Vendor")
            {
                selectedCategoryIds = registerDTO.CategoryIds?
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(id => id.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList() ?? [];

                if (selectedCategoryIds.Count == 0)
                    return Error.Validation("Vendor.CategoryRequired", "Please select at least one category for vendor registration.");

                var categoryRepo = _unitOfWork.GetRepository<Category, string>();
                var existingCategoryIds = (await categoryRepo.GetAllAsync())
                    .Select(c => c.Id)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var invalidCategoryIds = selectedCategoryIds
                    .Where(id => !existingCategoryIds.Contains(id))
                    .ToList();

                if (invalidCategoryIds.Count > 0)
                    return Error.Validation("Vendor.InvalidCategory", $"Invalid category ids: {string.Join(", ", invalidCategoryIds)}");
            }

            var User = new ApplicationUser
            {
                FullName = registerDTO.FullName,
                Email = registerDTO.Email,
                UserName = registerDTO.Email.Split('@')[0],
                PhoneNumber = registerDTO.Phone,
                Status = UserStatus.Active
            };

            var IdentityResult = await _userManager.CreateAsync(User, registerDTO.Password);

            if (!IdentityResult.Succeeded)
                return IdentityResult.Errors.Select(e => Error.Validation(e.Code, e.Description)).ToList();

            var roleResult = await _userManager.AddToRoleAsync(User, registerDTO.Role);
            if (!roleResult.Succeeded)
                return roleResult.Errors.Select(e => Error.Validation(e.Code, e.Description)).ToList();

            if (registerDTO.Role == "Vendor")
            {
                foreach (var categoryId in selectedCategoryIds)
                {
                    User.VendorCategories.Add(new VendorCategory
                    {
                        VendorId = User.Id,
                        CategoryId = categoryId
                    });
                }

                var updateResult = await _userManager.UpdateAsync(User);
                if (!updateResult.Succeeded)
                    return updateResult.Errors.Select(e => Error.Validation(e.Code, e.Description)).ToList();
            }

            return true;
        }

        public async Task<Result<AuthResponseDTO>> LoginAsync(LoginDTO loginDTO)
        {
            var User = await _userManager.FindByEmailAsync(loginDTO.Email);
            if(User is null)
                return Error.InvalidCrendentials("User.InvalidCredentials", "Email Not Valid");
            if(User.Status == UserStatus.Suspended)
                return Error.Unauthorized("User.Suspended", "Your Account Has Been Suspended, Please Contact Support");
            var PasswordValid = await _userManager.CheckPasswordAsync(User, loginDTO.Password);
            if (!PasswordValid)
                return Error.InvalidCrendentials("User.InvalidCredentials", "Password Not Valid");
            var Token  = await CreateTokenAsync(User);
            return new AuthResponseDTO
            {
                FullName = User.FullName,
                Email = User.Email!,
                Role = (await _userManager.GetRolesAsync(User)).FirstOrDefault()!,
                Token = Token
            };
        }

        public async Task<Result<bool>> UpdateUserAsync(string UserId, UpdateUserDTO updateUserDTO)
        {
            var User = await _userManager.FindByIdAsync(UserId);
            if (User is null)
                return Error.NotFound("User.NotFound", "User not found.");

            if (!string.IsNullOrEmpty(updateUserDTO.Email))
            {
                var ExistingUser = await _userManager.FindByEmailAsync(updateUserDTO.Email);
                if (ExistingUser is not null && ExistingUser.Id != UserId)
                    return Error.Failure("User.EmailTaken", "Email Is Already Taken");
                User.Email = updateUserDTO.Email;
                User.UserName = updateUserDTO.Email.Split('@')[0];
            }
            if (!string.IsNullOrEmpty(updateUserDTO.FullName))
                User.FullName = updateUserDTO.FullName;

            var UpdateResult = await _userManager.UpdateAsync(User);
            if (!UpdateResult.Succeeded)
                return Error.Failure("User.UpdateFailed", string.Join(", ", UpdateResult.Errors.Select(e => e.Description)));

            return UpdateResult.Succeeded;
        }
        public async Task<Result<bool>> ChangePasswordAsync(string UserId, ChangePasswordDTO changePasswordDTO)
        {
            var User = await _userManager.FindByIdAsync(UserId);
            if (User is null)
                return Error.NotFound("User.NotFound", "User not found.");

            if (!string.IsNullOrEmpty(changePasswordDTO.CurrentPassword) && !string.IsNullOrEmpty(changePasswordDTO.NewPassword))
            {
                if (changePasswordDTO.CurrentPassword == changePasswordDTO.NewPassword)
                    return Error.Validation("User.Password", "Current Password same New Password");
                if (changePasswordDTO.NewPassword != changePasswordDTO.ConfirmNewPassword)
                    return Error.Validation("User.Password", "New Password and Confirm New Password do not match.");

                var ChangePasswordResult = await _userManager.ChangePasswordAsync(User, changePasswordDTO.CurrentPassword, changePasswordDTO.NewPassword);
                if (!ChangePasswordResult.Succeeded)
                    return Error.Failure("User.PasswordUpdateFailed", string.Join(", ", ChangePasswordResult.Errors.Select(e => e.Description)));
            }
            return true;
        }

        public async Task<Result<bool>> ForgotPasswordAsync(ForgetPasswordDTO forgetPasswordDTO)
        {
            var User = await _userManager.FindByEmailAsync(forgetPasswordDTO.Email);
            if (User is null)
                return Error.NotFound("User.NotFound" , "Email Not Found");

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(User);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(resetToken));
            var resetPasswordUrlBase = _configuration["Frontend:ResetPasswordUrl"];

            if (string.IsNullOrWhiteSpace(resetPasswordUrlBase))
                return Error.Failure("ResetPassword.UrlNotConfigured", "Reset password URL is not configured.");

            var resetLink =
                $"{resetPasswordUrlBase}?email={Uri.EscapeDataString(User.Email!)}&token={Uri.EscapeDataString(encodedToken)}";

            var emailBody = $@"
                <p>Hi {User.FullName},</p>
                <p>You requested to reset your password.</p>
                <p>
                    <a href=""{resetLink}"">Click here to reset your password</a>
                </p>
                <p>If you did not request this, you can safely ignore this email.</p>";

            await _emailService.SendEmailAsync(User.Email!, "Reset your password", emailBody);
            return true;
        }

        public async Task<Result<bool>> ResetPasswordAsync(ResetPasswordDTO resetPasswordDTO)
        {
            if (resetPasswordDTO.NewPassword != resetPasswordDTO.ConfirmNewPassword)
                return Error.Validation("User.Password", "New password and confirmation password do not match.");

            var user = await _userManager.FindByEmailAsync(resetPasswordDTO.Email);
            if (user is null)
                return Error.NotFound("User.NotFound", "User not found.");

            string decodedToken;
            try
            {
                decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(resetPasswordDTO.Token));
            }
            catch
            {
                return Error.Validation("ResetPassword.InvalidToken", "Reset password token is invalid.");
            }

            var resetResult = await _userManager.ResetPasswordAsync(user, decodedToken, resetPasswordDTO.NewPassword);
            if (!resetResult.Succeeded)
                return resetResult.Errors.Select(e => Error.Validation(e.Code, e.Description)).ToList();

            return true;
        }

        private async Task<string> CreateTokenAsync(ApplicationUser user)
        {
            var Claims = new List<Claim>()
            {
                new(JwtRegisteredClaimNames.Email , user.Email!),
                new(JwtRegisteredClaimNames.NameId , user.Id!),
                new(JwtRegisteredClaimNames.Name , user.FullName!)
            };

            var Roles = await _userManager.GetRolesAsync(user);
            foreach (var Role in Roles)
            {
                Claims.Add(new Claim(ClaimTypes.Role, Role));
            }
            var SecretKey = _configuration["JWTOptions:SecretKey"]!;
            var Key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
            var Cred = new SigningCredentials(Key, SecurityAlgorithms.HmacSha256);

            var Token = new JwtSecurityToken(
                issuer: _configuration["JWTOptions:Issuer"],
                audience: _configuration["JWTOptions:Audience"],
                expires: DateTime.UtcNow.AddHours(1),
                claims: Claims,
                signingCredentials: Cred);

            return new JwtSecurityTokenHandler().WriteToken(Token);
        }
    }
}
