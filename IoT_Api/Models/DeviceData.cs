using BsonData;

namespace System
{
    public partial class Document
    {
        public double Data { get { return GetValue<double>("data"); } set => Push("data", value); }
        public string? Unit { get { return GetString("unit"); } set => Push("unit", value); }
        public DateTime? Time { get { return GetDateTime("time"); } set => Push("time", value); }

        public bool CheckAlert()
        {
            return Data > 100;
        }
    }
}

namespace Actors 
{ 
    partial class Account
    {
        public virtual Document Alert(object value)
        {
            return CreateApiResponse(300, "Fire warning", value);
        }
        public object GetAlert(Document context)
        {
            var user = DB.Users.Find(context.ObjectId, null).ChangeType<User>();
            var device = user.Device;
            if (DB.Alert.Find(device.Model + device.Version, null) != null)
            {
                return Alert(user);
            }

            return Ok();
        }
    }

    partial class User
    {
        public object GetAllData(Document context)
        {

            List<Document> datas = new List<Document>();
            var device = this.Device;
            device.ObjectId = device.Model + device.Version;
            foreach(var data in DB.GetDataCollection(this.Device).Select())
            {
                datas.Add(data);
            }

            return Ok(datas);
        }

        public object GetDevice(Document context)
        {
            return Ok(this.Device);
        }
    }
}

namespace Models
{
    public class DeviceData : Document
    {
        public static Collection GetDeviceCollection(string id)
        {
            return DB.Data.GetCollection(id);
        }

        public static void Save(Document context)
        {
            var time = DateTime.Now;
            context.Time = time;
            if (context.CheckAlert())
            {
                DB.Alert.InsertOrUpdate(context.Clone());
            }
            var id = context.Pop<string>("_id");
            GetDeviceCollection(id).Insert(time.Ticks.ToString(), context);
        }


    }
}

namespace System
{
    using Models;
    partial class DB
    {
        static Collection _alert;
        static public Collection Alert
        {
            get
            {
                if (_alert == null)
                {
                    _alert = Data.GetCollection("Alerts");
                }
                return _alert;
            }
        }
        static Database? _data;
        public static Database Data
        {
            get
            {
                if (_data == null)
                {
                    _data = new Database("Data")
                        .Connect(Main.ConnectionString)
                        .StartStorageThread();
                }
                return _data;
            }
        }
        public static Collection GetDataCollection(Device device)
        {
            return Data.GetCollection(device.ObjectId);
        }
    }
}
