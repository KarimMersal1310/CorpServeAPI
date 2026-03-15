using CorpServe.Presistence.Data.DbContext;
using EventHub.Domain.Contracts;
using EventHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Presistence.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly CorpServeDbContext _dbContext;
        private readonly Dictionary<Type, object> _Repositories = [];
        public UnitOfWork(CorpServeDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>() where TEntity : BaseEntity<TKey>
        {
            var EntityType = typeof(TEntity);
            if (_Repositories.TryGetValue(EntityType, out var Repo))
                return (IGenericRepository<TEntity ,TKey>)Repo;
            var NewRepo = new GenericRepository<TEntity , TKey>(_dbContext);
            _Repositories[EntityType] = NewRepo;
            return NewRepo;
        }

        public Task<int> SaveChangesAsync() => _dbContext.SaveChangesAsync(); 
    }
}
