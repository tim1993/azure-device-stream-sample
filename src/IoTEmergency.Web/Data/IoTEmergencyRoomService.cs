using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using System.Text.Json;

namespace IoTEmergency.Web.Data
{
    public class IoTEmergencyRoomService
    {
        private readonly RegistryManager _registryClient;
        private readonly ServiceClient _serviceClient;
        public IoTEmergencyRoomService(RegistryManager registryManager, ServiceClient serviceClient)
        {
            _registryClient = registryManager;
            _serviceClient = serviceClient;
        }

        public async IAsyncEnumerable<Twin> ListDevices()
        {
            await _registryClient.OpenAsync();
            var query = _registryClient.CreateQuery("SELECT * FROM devices");
            while (query.HasMoreResults)
            {
                var result = await query.GetNextAsTwinAsync();
                foreach (var device in result)
                {
                    yield return device;
                }
            }
        }

        public async Task<bool> InvokeDiskflooding(string deviceId, int fileSizeMb = 1024)
        {
            var c2dMethod = new CloudToDeviceMethod("FloodDisk", TimeSpan.FromMinutes(1));
            c2dMethod.SetPayloadJson(JsonSerializer.Serialize(new { size = fileSizeMb }));

            var result = await _serviceClient.InvokeDeviceMethodAsync(deviceId, "rogue", c2dMethod);
            if (result.Status == 200)
            {
                return true;
            }

            return false;
        }

    }
}
