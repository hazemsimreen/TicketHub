using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
//using TicketHub.DataAccess.Entities;

namespace TicketHub.DataAccess.Repositories;

public interface IRepository<T>
    where T : AuditableEntity
{

    IQueryable<T> Query();


    Task<T?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);



    Task<List<T>> GetAllAsync(
        CancellationToken cancellationToken = default);



    Task<List<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);



    Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);



    Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default);



    Task AddAsync(
        T entity,
        CancellationToken cancellationToken = default);



    void Update(T entity);



    void Remove(T entity);

}