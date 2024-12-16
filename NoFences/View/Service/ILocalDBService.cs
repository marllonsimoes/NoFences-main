using NoFencesService.Repository;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoFences.View.Service
{
    public interface ILocalDBService<T> where T : class
    {
        T Get(long id);

        List<T> List();

        void Delete(long id);

        T Update(T path);

        T Add(T path);
    }
}
