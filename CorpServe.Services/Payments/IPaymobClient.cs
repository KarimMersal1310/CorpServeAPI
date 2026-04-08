namespace CorpServe.Services.Payments
{
    public interface IPaymobClient
    {
        Task<PaymobIntentionCreateResult> CreateIntentionAsync(PaymobIntentionRequest request, CancellationToken cancellationToken = default);
    }
}
