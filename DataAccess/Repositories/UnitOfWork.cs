using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace TicketHub.DataAccess.Repositories;


public class UnitOfWork : IUnitOfWork
{

    private readonly DbContext _context;


    private readonly Dictionary<Type, object> _repositories = new();




    public UnitOfWork(
        DbContext context)
    {
        _context = context;
    }





    public IRepository<T> Repository<T>()
        where T : AuditableEntity
    {


        var type = typeof(T);



        if (!_repositories.ContainsKey(type))
        {

            var repository =
                new Repository<T>(_context);


            _repositories.Add(type, repository);

        }



        return (IRepository<T>)_repositories[type];

    }





    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {

        return await _context
            .SaveChangesAsync(cancellationToken);

    }


    public void Dispose()
    {

        _context.Dispose();

    }

}
