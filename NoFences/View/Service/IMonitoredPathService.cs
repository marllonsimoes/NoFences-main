using NoFencesService.Repository;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace NoFences.View.Service
{
    public interface IMonitoredPathService : ILocalDBService<NoFencesService.Repository.MonitoredPath>
    {
    }

    public sealed class MonitoredPathService : IMonitoredPathService
    {

        private LocalDBContext localDBContext = new LocalDBContext();

        void ILocalDBService<NoFencesService.Repository.MonitoredPath>.Delete(long id)
        {
            localDBContext.MonitoredPaths.Remove(localDBContext.MonitoredPaths.Where(m => m.Id == id).First());
        }

        NoFencesService.Repository.MonitoredPath ILocalDBService<NoFencesService.Repository.MonitoredPath>.Get(long id)
        {
            return localDBContext.MonitoredPaths.Where(m => m.Id == id).FirstOrDefault();
        }

        List<NoFencesService.Repository.MonitoredPath> ILocalDBService<NoFencesService.Repository.MonitoredPath>.List()
        {
            return localDBContext.MonitoredPaths.ToList();
        }

        NoFencesService.Repository.MonitoredPath ILocalDBService<NoFencesService.Repository.MonitoredPath>.Add(NoFencesService.Repository.MonitoredPath monitoredPath)
        {
            if (monitoredPath.Device != null)
            {
                var device = localDBContext.DevicesInfo.Attach(monitoredPath.Device);
            }
            var response = localDBContext.MonitoredPaths.Add(monitoredPath);
            localDBContext.SaveChanges();
            return response;
        }

        NoFencesService.Repository.MonitoredPath ILocalDBService<NoFencesService.Repository.MonitoredPath>.Update(NoFencesService.Repository.MonitoredPath monitoredPath)
        {
            localDBContext.Entry(monitoredPath).State = EntityState.Modified;
            if (monitoredPath.Device != null)
            {
                var device = localDBContext.DevicesInfo.Attach(monitoredPath.Device);
            }
            localDBContext.SaveChanges();
            return monitoredPath;
        }
    }
}
