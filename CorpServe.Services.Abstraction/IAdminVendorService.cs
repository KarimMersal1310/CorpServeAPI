using CorpServe.Shared.DTOs.VendorVerify;
using CorpServe.Shared.CommonResult;

public interface IAdminVendorService
{
    Task<Result<IEnumerable<VendorVerifyDTO>>> GetPendingVerificationsAsync();
    Task<Result<bool>> ApproveVerificationAsync(string vendorVerifyId, string adminId);
    Task<Result<bool>> RejectVerificationAsync(string vendorVerifyId, string adminId, string rejectReason);
}
