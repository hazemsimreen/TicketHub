using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace TicketHub.DataAccess.Repositories;


public interface IUnitOfWork : IDisposable
{


    IRepository<T> Repository<T>()
        where T : AuditableEntity;



    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);

}
