using CorpServe.Domain.Entities.RatingModule;

namespace CorpServe.Services.Specifications
{
    public sealed class RatingByRequestIdSpecification : BaseSpecificactions<Rating, string>
    {
        public RatingByRequestIdSpecification(string requestId)
            : base(r => r.RequestId == requestId)
        {
            AddInclude(r => r.Request);
            AddInclude(r => r.Vendor);
        }
    }
}
