using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Domain.Entities.SpecializedCategoryModule;
using CorpServe.Services.Abstraction;
using CorpServe.Services.EmailTemplates;
using CorpServe.Shared.DTOs.AuthDTOs;
using CorpServe.Shared.CommonResult;
using CorpServe.Domain.Contracts;
using CorpServe.Shared.Notifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private const string RefreshTokenProvider = "CorpServe";
        private const string RefreshTokenName = "RefreshToken";
        private const string RefreshTokenExpiryName = "RefreshTokenExpiresAtUtc";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AuthenticationService> _logger;
        private readonly IOptions<DataProtectionTokenProviderOptions> _options;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICategoryDataQueries _categoryDataQueries;

        public AuthenticationService(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IEmailService emailService,
            INotificationService notificationService,
            ILogger<AuthenticationService> logger,
            IUnitOfWork unitOfWork,
            IOptions<DataProtectionTokenProviderOptions> options,
            ICategoryDataQueries categoryDataQueries)
        {
            _userManager = userManager;
            _configuration = configuration;
            _emailService = emailService;
            _notificationService = notificationService;
            _logger = logger;
            _options = options;
            _unitOfWork = unitOfWork;
            _categoryDataQueries = categoryDataQueries;
        }

        public async Task<Result<AuthResponseDTO>> RegisterAsync(RegisterDTO registerDTO)
        {
            var phoneNumber = registerDTO.Phone?.Trim();
            var phoneValidationResult = ValidatePhoneNumber(phoneNumber);
            if (phoneValidationResult.IsFailure)
                return phoneValidationResult.Errors.ToList();

            var phoneAlreadyExists = await _userManager.Users.AnyAsync(u => u.PhoneNumber == phoneNumber);
            if (phoneAlreadyExists)
                return Error.Conflict("User.PhoneNumberTaken", "Phone number is already registered.");

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

                var invalidCategoryIds = (await _categoryDataQueries.GetInvalidCategoryIdsAsync(selectedCategoryIds)).ToList();

                if (invalidCategoryIds.Count > 0)
                    return Error.Validation("Vendor.InvalidCategory", $"Invalid category ids: {string.Join(", ", invalidCategoryIds)}");
            }

            var User = new ApplicationUser
            {
                FullName = registerDTO.FullName,
                Email = registerDTO.Email,
                UserName = registerDTO.Email.Split('@')[0],
                PhoneNumber = phoneNumber,
                Status = UserStatus.Active
            };

            IdentityResult IdentityResult;
            try
            {
                IdentityResult = await _userManager.CreateAsync(User, registerDTO.Password);
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                var hasPhoneToken = errorMessage.Contains("PhoneNumber", StringComparison.OrdinalIgnoreCase);
                var hasDuplicateToken =
                    errorMessage.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                    errorMessage.Contains("unique", StringComparison.OrdinalIgnoreCase) ||
                    errorMessage.Contains("IX_AspNetUsers_PhoneNumber", StringComparison.OrdinalIgnoreCase);

                if (hasPhoneToken && hasDuplicateToken)
                    return Error.Conflict("User.PhoneNumberTaken", "Phone number is already registered.");

                if (errorMessage.Contains("UserValidPhoneCheck", StringComparison.OrdinalIgnoreCase)
                    || errorMessage.Contains("PhoneNumber", StringComparison.OrdinalIgnoreCase))
                {
                    return Error.Validation("User.InvalidPhone", "Invalid phone number format. Please enter a valid phone number.");
                }

                return Error.Validation("User.RegisterFailed", "Registration failed due to invalid data.");
            }

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

            var accessTokenExpiresAtUtc = GetAccessTokenExpiryUtc();
            var token = await CreateTokenAsync(User, accessTokenExpiresAtUtc);
            var refreshToken = CreateRefreshToken(User.Id);
            var refreshTokenExpiresAtUtc = GetRefreshTokenExpiryUtc();
            await SetRefreshTokenAsync(User, refreshToken, refreshTokenExpiresAtUtc);

            var welcomeNotification = await _notificationService.SendNotificationAsync(
                User.Id,
                NotificationTitles.WelcomeToCorpServe,
                "Your account was created successfully.",
                NotificationTypes.Success,
                User.Id,
                "User");

            if (welcomeNotification.IsFailure)
                LogNotificationFailure("Register", welcomeNotification.Errors);

            return new AuthResponseDTO
            {
                FullName = User.FullName,
                Email = User.Email!,
                Role = registerDTO.Role,
                Token = token,
                AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc,
                RefreshToken = refreshToken,
                RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc
            };
        }

        public async Task<Result<LoginResponseDTO>> LoginAsync(LoginDTO loginDTO)
        {
            var User = await _userManager.FindByEmailAsync(loginDTO.Email);
            if(User is null)
                return Error.InvalidCrendentials("User.InvalidCredentials", "Email Not Valid");
            if(User.Status == UserStatus.Suspended)
                return Error.Unauthorized("User.Suspended", "Your Account Has Been Suspended, Please Contact Support");
            var PasswordValid = await _userManager.CheckPasswordAsync(User, loginDTO.Password);
            if (!PasswordValid)
                return Error.InvalidCrendentials("User.InvalidCredentials", "Password Not Valid");
            var accessTokenExpiresAtUtc = GetAccessTokenExpiryUtc();
            var Token  = await CreateTokenAsync(User, accessTokenExpiresAtUtc);
            var refreshToken = CreateRefreshToken(User.Id);
            var refreshTokenExpiresAtUtc = GetRefreshTokenExpiryUtc();
            await SetRefreshTokenAsync(User, refreshToken, refreshTokenExpiresAtUtc);

            return new LoginResponseDTO
            {
                FullName = User.FullName,
                Email = User.Email ?? string.Empty,
                Role = (await _userManager.GetRolesAsync(User)).FirstOrDefault()!,
                Token = Token,
                AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc,
                RefreshToken = refreshToken,
                RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc
            };
        }

        public async Task<Result<LoginResponseDTO>> RefreshTokenAsync(RefreshTokenRequestDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return Error.Validation("Auth.RefreshTokenRequired", "Refresh token is required.");

            var userId = GetUserIdFromRefreshToken(request.RefreshToken);
            if (string.IsNullOrWhiteSpace(userId))
                return Error.Unauthorized("Auth.InvalidRefreshToken", "Refresh token is invalid.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return Error.Unauthorized("Auth.InvalidRefreshToken", "Refresh token is invalid.");

            var storedRefreshToken = await _userManager.GetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
            if (string.IsNullOrWhiteSpace(storedRefreshToken) || !string.Equals(storedRefreshToken, request.RefreshToken, StringComparison.Ordinal))
                return Error.Unauthorized("Auth.InvalidRefreshToken", "Refresh token is invalid or revoked.");

            var expiryValue = await _userManager.GetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenExpiryName);
            if (!DateTime.TryParse(expiryValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var refreshTokenExpiresAtUtc))
                return Error.Failure("Auth.RefreshTokenInvalidState", "Refresh token state is invalid.");

            if (refreshTokenExpiresAtUtc <= DateTime.UtcNow)
                return Error.Unauthorized("Auth.RefreshTokenExpired", "Refresh token has expired.");

            if (user.Status == UserStatus.Suspended)
                return Error.Unauthorized("User.Suspended", "Your Account Has Been Suspended, Please Contact Support");

            var newRefreshToken = CreateRefreshToken(user.Id);
            var newRefreshTokenExpiresAtUtc = GetRefreshTokenExpiryUtc();
            await SetRefreshTokenAsync(user, newRefreshToken, newRefreshTokenExpiresAtUtc);

            var accessTokenExpiresAtUtc = GetAccessTokenExpiryUtc();
            var accessToken = await CreateTokenAsync(user, accessTokenExpiresAtUtc);

            return new LoginResponseDTO
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? string.Empty,
                Token = accessToken,
                AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc,
                RefreshToken = newRefreshToken,
                RefreshTokenExpiresAtUtc = newRefreshTokenExpiresAtUtc
            };
        }

        public async Task<Result<bool>> RevokeRefreshTokenAsync(RevokeRefreshTokenRequestDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return Error.Validation("Auth.RefreshTokenRequired", "Refresh token is required.");

            var userId = GetUserIdFromRefreshToken(request.RefreshToken);
            if (string.IsNullOrWhiteSpace(userId))
                return Error.Unauthorized("Auth.InvalidRefreshToken", "Refresh token is invalid.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return Error.Unauthorized("Auth.InvalidRefreshToken", "Refresh token is invalid.");

            var storedRefreshToken = await _userManager.GetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
            if (string.IsNullOrWhiteSpace(storedRefreshToken) || !string.Equals(storedRefreshToken, request.RefreshToken, StringComparison.Ordinal))
                return Error.Unauthorized("Auth.InvalidRefreshToken", "Refresh token is invalid or already revoked.");

            await RevokeStoredRefreshTokenAsync(user);
            return true;
        }

        public async Task<Result<UserProfileDTO>> GetUserProfileAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Error.Unauthorized("User.Unauthorized", "User identity is required.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return Error.NotFound("User.NotFound", "User not found.");

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? string.Empty;

            return new UserProfileDTO
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Role = role
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
                    return Error.Conflict("User.EmailTaken", "Email Is Already Taken");
                User.Email = updateUserDTO.Email;
                User.UserName = updateUserDTO.Email.Split('@')[0];
            }
            if (!string.IsNullOrEmpty(updateUserDTO.FullName))
                User.FullName = updateUserDTO.FullName;

            if(!string.IsNullOrEmpty(updateUserDTO.PhoneNumber))
            {
                var phoneValidationResult = ValidatePhoneNumber(updateUserDTO.PhoneNumber);
                if (phoneValidationResult.IsFailure)
                    return phoneValidationResult.Errors.ToList();
                var ExistingUser = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == updateUserDTO.PhoneNumber);
                if (ExistingUser is not null && ExistingUser.Id != UserId)
                    return Error.Conflict("User.PhoneNumberTaken", "Phone number is already registered.");
                User.PhoneNumber = updateUserDTO.PhoneNumber;
            }

            var UpdateResult = await _userManager.UpdateAsync(User);
            if (!UpdateResult.Succeeded)
                return Error.Failure("User.UpdateFailed", string.Join(", ", UpdateResult.Errors.Select(e => e.Description)));

            var updateNotification = await _notificationService.SendNotificationAsync(
                User.Id,
                NotificationTitles.ProfileUpdated,
                "Your profile details were updated successfully.",
                NotificationTypes.Info,
                User.Id,
                "User");

            if (updateNotification.IsFailure)
                LogNotificationFailure("UpdateUser", updateNotification.Errors);

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

                await RevokeStoredRefreshTokenAsync(User);

                var passwordChangedNotification = await _notificationService.SendNotificationAsync(
                    User.Id,
                    NotificationTitles.PasswordChanged,
                    "Your password was changed successfully. If this was not you, contact support immediately.",
                    NotificationTypes.Warning,
                    User.Id,
                    "User");

                if (passwordChangedNotification.IsFailure)
                    LogNotificationFailure("ChangePassword", passwordChangedNotification.Errors);
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

            var lifespanMinutes = _options.Value.TokenLifespan.TotalMinutes.ToString("0");
            var resetPasswordEmail = CorpServeEmailTemplateFactory.BuildResetPassword(User.FullName ?? "User", resetLink, lifespanMinutes);
            await _emailService.SendEmailAsync(User.Email!, resetPasswordEmail.Subject, resetPasswordEmail.Body);
            return true;
        }

        public async Task<Result<bool>> ResetPasswordAsync(ResetPasswordDTO resetPasswordDTO)
        {
            if (resetPasswordDTO.NewPassword != resetPasswordDTO.ConfirmNewPassword)
                return Error.Validation("User.Password", "New password and confirmation password do not match.");

            var user = await _userManager.FindByEmailAsync(resetPasswordDTO.Email);
            if (user is null)
                return Error.NotFound("User.NotFound", "User not found.");

            // Prevent setting the same password currently stored in DB.
            var isSameAsCurrentPassword = await _userManager.CheckPasswordAsync(user, resetPasswordDTO.NewPassword);
            if (isSameAsCurrentPassword)
                return Error.Validation("User.Password", "New password must be different from your current password.");

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

            // Explicitly rotate security stamp so this reset token cannot be reused.
            var stampResult = await _userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
                return Error.Failure("ResetPassword.SecurityStampUpdateFailed", "Password was changed but reset link could not be invalidated.");

            await RevokeStoredRefreshTokenAsync(user);

            var passwordResetNotification = await _notificationService.SendNotificationAsync(
                user.Id,
                NotificationTitles.PasswordReset,
                "Your password was reset successfully. If this was not you, contact support immediately.",
                NotificationTypes.Warning,
                user.Id,
                "User");

            if (passwordResetNotification.IsFailure)
                LogNotificationFailure("ResetPassword", passwordResetNotification.Errors);

            return true;
        }

        private async Task<string> CreateTokenAsync(ApplicationUser user, DateTime expiresAtUtc)
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
                expires: expiresAtUtc,
                claims: Claims,
                signingCredentials: Cred);

            return new JwtSecurityTokenHandler().WriteToken(Token);
        }

        private static Result ValidatePhoneNumber(string? phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return Result.Fail(Error.Validation("User.PhoneRequired", "Phone number is required."));

            if (phoneNumber.Length < 11)
                return Result.Fail(Error.Validation("User.InvalidPhone", "Phone number must be at least 11 digits."));

            return Result.Ok();
        }

        private DateTime GetAccessTokenExpiryUtc()
        {
            var accessTokenHours = _configuration.GetValue<int?>("JWTOptions:AccessTokenHours") ?? 30;
            if (accessTokenHours <= 0)
                accessTokenHours = 30;

            return DateTime.UtcNow.AddHours(accessTokenHours);
        }

        private DateTime GetRefreshTokenExpiryUtc()
        {
            var refreshTokenDays = _configuration.GetValue<int?>("JWTOptions:RefreshTokenDays") ?? 7;
            if (refreshTokenDays <= 0)
                refreshTokenDays = 7;

            return DateTime.UtcNow.AddDays(refreshTokenDays);
        }

        private static string CreateRefreshToken(string userId)
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            var tokenPart = WebEncoders.Base64UrlEncode(bytes);
            return $"{userId}.{tokenPart}";
        }

        private static string? GetUserIdFromRefreshToken(string refreshToken)
        {
            var separatorIndex = refreshToken.IndexOf('.', StringComparison.Ordinal);
            if (separatorIndex <= 0)
                return null;

            return refreshToken[..separatorIndex];
        }

        private async Task SetRefreshTokenAsync(ApplicationUser user, string refreshToken, DateTime refreshTokenExpiresAtUtc)
        {
            await _userManager.SetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName, refreshToken);
            await _userManager.SetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenExpiryName, refreshTokenExpiresAtUtc.ToString("O", CultureInfo.InvariantCulture));
        }

        private async Task RevokeStoredRefreshTokenAsync(ApplicationUser user)
        {
            await _userManager.RemoveAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
            await _userManager.RemoveAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenExpiryName);
        }

        private void LogNotificationFailure(string flow, IReadOnlyList<Error> errors)
        {
            _logger.LogWarning(
                "Notification failed in auth flow {Flow}. Errors: {Errors}",
                flow,
                string.Join(" | ", errors.Select(e => $"{e.Code}:{e.Description}")));
        }

    }
}
