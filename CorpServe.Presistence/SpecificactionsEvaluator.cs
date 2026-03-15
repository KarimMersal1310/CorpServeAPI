using EventHub.Domain.Contracts;
using EventHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Presistence
{
    internal static class SpecificactionsEvaluator
    {
        public static IQueryable<TEntity> CreateQuery<TEntity, TKey>(IQueryable<TEntity> EntryPoint
    , ISpecificactions<TEntity, TKey> specificactions) where TEntity : BaseEntity<TKey>
        {
            var Query = EntryPoint;
            if (specificactions is not null)
            {
                if (specificactions.Criteria is not null)
                {
                    Query = Query.Where(specificactions.Criteria);
                }
                if (specificactions.IncludeExpressions is not null && specificactions.IncludeExpressions.Any())
                {
                    Query = specificactions.IncludeExpressions.Aggregate(Query
                        , (CurrentQuery, IncludeExp) => CurrentQuery.Include(IncludeExp));

                }
                if (specificactions.OrderBy is not null)
                {
                    Query = Query.OrderBy(specificactions.OrderBy);
                }
                if (specificactions.OrderByDescending is not null)
                {
                    Query = Query.OrderByDescending(specificactions.OrderByDescending);
                }
                if (specificactions.IsPaginated)
                {
                    Query = Query.Skip(specificactions.Skip).Take(specificactions.Take);
                }
            }

            return Query;
        }
    }
}
