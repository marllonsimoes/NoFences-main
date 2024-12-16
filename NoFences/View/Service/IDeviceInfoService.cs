using NoFencesService.Repository;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoFences.View.Service
{
    public interface IDeviceInfoService : ILocalDBService<NoFencesService.Repository.DeviceInfo>
    {
        DeviceInfo GetByMountPoint(string mountPoint);
    }

    public sealed class DeviceInfoService : IDeviceInfoService
    {

        private LocalDBContext localDBContext = new LocalDBContext();

        void ILocalDBService<NoFencesService.Repository.DeviceInfo>.Delete(long id)
        {
            localDBContext.DevicesInfo.Remove(localDBContext.DevicesInfo.Where(m => m.Id == id).First());
        }

        NoFencesService.Repository.DeviceInfo ILocalDBService<NoFencesService.Repository.DeviceInfo>.Get(long id)
        {
            return localDBContext.DevicesInfo.Where(m => m.Id == id).FirstOrDefault();
        }

        List<NoFencesService.Repository.DeviceInfo> ILocalDBService<NoFencesService.Repository.DeviceInfo>.List()
        {
            return localDBContext.DevicesInfo.ToList();
        }

        NoFencesService.Repository.DeviceInfo ILocalDBService<NoFencesService.Repository.DeviceInfo>.Add(NoFencesService.Repository.DeviceInfo monitoredPath)
        {
            var response = localDBContext.DevicesInfo.Add(monitoredPath);
            localDBContext.SaveChanges();
            return response;
        }

        NoFencesService.Repository.DeviceInfo ILocalDBService<NoFencesService.Repository.DeviceInfo>.Update(NoFencesService.Repository.DeviceInfo path)
        {
            throw new NotImplementedException();
        }

        public DeviceInfo GetByMountPoint(string mountPoint)
        {
            return localDBContext.DevicesInfo.Where(d => d.DeviceMountUnit.Equals(mountPoint)).FirstOrDefault();
        }
    }
}
