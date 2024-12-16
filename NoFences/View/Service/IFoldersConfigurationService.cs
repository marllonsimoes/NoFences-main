using NoFencesService.Repository;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoFences.View.Service
{
    public interface IFoldersConfigurationService : ILocalDBService<FolderConfiguration>
    {
    }

    public sealed class FolderConfigurationService : IFoldersConfigurationService
    {
        private LocalDBContext localDBContext = new LocalDBContext();

        public FolderConfiguration Add(FolderConfiguration path)
        {
            throw new NotImplementedException();
        }

        void ILocalDBService<FolderConfiguration>.Delete(long id)
        {
            localDBContext.FolderConfigurations.Remove(localDBContext.FolderConfigurations.Where(m => m.Id == id).First());
        }

        FolderConfiguration ILocalDBService<FolderConfiguration>.Get(long id)
        {
            return localDBContext.FolderConfigurations.Where(m => m.Id == id).FirstOrDefault();
        }

        List<FolderConfiguration> ILocalDBService<FolderConfiguration>.List()
        {
            return localDBContext.FolderConfigurations.ToList();
        }

        FolderConfiguration ILocalDBService<FolderConfiguration>.Update(FolderConfiguration path)
        {
            throw new NotImplementedException();
        }
    }
}
