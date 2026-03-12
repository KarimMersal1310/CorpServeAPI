using CorpServe.Shared.DTOs.VendorVerify;
using E_Commerce.Shared.CommonResult;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CorpServe.Services.Abstraction
{
    public interface IVendorVerifyService
    {
    Task<Result<VendorVerifyDTO>> SubmitVerificationAsync(string vendorId, VendorVerifyRequestDTO request);
    Task<Result<VendorVerifyDTO>> GetVendorVerificationStatusAsync(string vendorId);
    }
}
