using System.Collections.Generic;

namespace ClinicManager.DAL
{
    public interface IDal<T> where T : class
    {
        T? GetById(int id);
        IEnumerable<T> GetAll();
        void Add(T entity);
        void Update(T entity);
        void Delete(int id);
    }
}
