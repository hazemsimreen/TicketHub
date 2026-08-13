using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace TicketHub.DataAccess.Repositories;


public interface IUnitOfWork : IDisposable
{


    IRepository<T> Repository<T>()
        where T : AuditableEntity;

    // جديد — لازم للـ Optimistic Concurrency (RowVersion)
    void SetOriginalValue<TEntity, TProperty>(
        TEntity entity,
        Expression<Func<TEntity, TProperty>> propertyExpression,
        TProperty value)
        where TEntity : class;

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);

}