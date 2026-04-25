using CorpServe.Presistence.Data.DbContext;
using CorpServe.Domain.Contracts;
using CorpServe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using CorpServe.Presistence;

namespace CorpServe.Presistence.Repository
{
    public class GenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey> where TEntity : BaseEntity<TKey>
    {
        private readonly CorpServeDbContext _dbContext;
        public GenericRepository(CorpServeDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task AddAsync(TEntity entity) => await _dbContext.Set<TEntity>().AddAsync(entity);
        public async Task<IReadOnlyList<TEntity>> GetAllAsync() =>
            await _dbContext.Set<TEntity>().AsNoTracking().ToListAsync();

        public async Task<IReadOnlyList<TEntity>> GetAllAsync(ISpecificactions<TEntity, TKey> specificactions)
        {
            return await SpecificactionsEvaluator.CreateQuery(_dbContext.Set<TEntity>().AsNoTracking(), specificactions).ToListAsync();
        }
        public async Task<TEntity?> GetByIdAsync(TKey id) => await _dbContext.Set<TEntity>().FindAsync(id);
        public async Task<TEntity?> GetByIdAsync(ISpecificactions<TEntity, TKey> specificactions)
        {
            return await SpecificactionsEvaluator.CreateQuery(_dbContext.Set<TEntity>().AsNoTracking(), specificactions).FirstOrDefaultAsync();
        }
        public IQueryable<TEntity> Query(ISpecificactions<TEntity, TKey>? specificactions = null) =>
            SpecificactionsEvaluator.CreateQuery(_dbContext.Set<TEntity>().AsNoTracking(), specificactions);
        public void Remove(TEntity entity) => _dbContext.Set<TEntity>().Remove(entity);
        public void Update(TEntity entity) => _dbContext.Set<TEntity>().Update(entity);
        public async Task<int> CountAsync(ISpecificactions<TEntity, TKey> specificactions)
        {
            return await SpecificactionsEvaluator.CreateQuery(_dbContext.Set<TEntity>().AsNoTracking(), specificactions).CountAsync();
        }
        public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate) => _dbContext.Set<TEntity>().AnyAsync(predicate);
    }
}
