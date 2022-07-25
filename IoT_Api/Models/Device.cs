using BsonData;
using System.Linq;

namespace System
{
    public partial class Document
    {
        public string Name { get { return GetString("name"); } set => Push("name", value); }
        public string Version { get { return GetString("version"); } set => Push("version", value); }
        public string Model { get { return GetString("model"); } set => Push("model", value); }

        public Document Device
        {
            get => GetDocument("device");
            set => Push("device", value);
        }
        public bool Busy { get => GetValue<bool>("busy"); set => Push("busy", value); }
    }
}

namespace Models
{
    public class Device : Document
    {
        public static List<Document> GetUsersDevices()
        {
            var users = DB.GetCollection<Actors.User>();
            var lst = new List<Document>();

            foreach (var doc in users.Select())
            {
                var user = doc.ChangeType<Actors.User>();
                var device = user.Device;
                device.ObjectId = device.Model + device.Version;
                lst.Add(device);
            }
            return lst;
        }

        public static void Process(Document context)
        {
            var id = context.ObjectId;

            if (DB.Devices.Find(id, null) == null)
            {
                DB.Devices.Insert(context);
            }

            if (context.Unit != null)
            {
                DeviceData.Save(context.Clone());
            }
        }
    }

}

namespace Actors
{
    using Models;
    partial class User
    {
        Device _device;
        public Device Device
        {
            get
            {
                if (_device == null)
                {
                    _device = this.GetDocument<Device>("device");
                    this.Push("device", _device);
                }
                return _device;
            }
        }

        public object GetAllDevice(Document context)
        {
            return Ok(this.Device);
        }
    }

    partial class Admin
    {
        public object GetAllDevice(Document context)
        {
            return Ok(Models.Device.GetUsersDevices());
        }

        public object GetAllUser(Document context)
        {
            return Ok(DB.Users.Select());
        }

        public object SetDeviceUser(Document context)
        {
            var user = DB.Users.Find(context.Name, null);
            if (user == null) return Error400;

            string? msg = "Not found";
            DB.Devices.FindAndUpdate(context.ObjectId, device => {
                if (device.GetValue<bool>("busy"))
                {
                    msg = "Device is busy";
                    return;
                }
                user.Device = device;
                DB.Users.Update(user);

                device.Busy = true;
                msg = null;
            });

            return msg == null ? Ok() : Error(100, msg);
        }
    }
}

namespace System
{
    partial class DB
    {
        static Collection? _devices;
        static public Collection Devices
        {
            get
            {
                if (_devices == null)
                {
                    _devices = GetCollection<Models.Device>();
                }
                return _devices;
            }
        }
    }

}
