using CorpServe.Presistence.Data.DbContext;
using CorpServe.Domain.Contracts;
using CorpServe.Domain.Entities;
using CorpServe.Domain.Entities.NotificationModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CorpServe.Presistence.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CorpServe.Presistence.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly CorpServeDbContext _dbContext;
        private readonly Dictionary<Type, object> _Repositories = [];
        private IDbContextTransaction? _currentTransaction;
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

        public Task<int> MarkAllNotificationsAsReadAsync(string recipientId, CancellationToken cancellationToken = default)
        {
            return _dbContext.Set<SystemNotification>()
                .Where(n => n.RecipientId == recipientId && !n.IsRead)
                .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true), cancellationToken);
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction is not null)
                return;

            _currentTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction is null)
                return;

            await _currentTransaction.CommitAsync(cancellationToken);
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction is null)
                return;

            await _currentTransaction.RollbackAsync(cancellationToken);
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }
}
