//using Microsoft.AspNetCore.Mvc;
//using IoT_Api.Models;
//using BsonData;

//namespace IoT_Api.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class UserController : ControllerBase
//    {
//        [HttpPost]
//        public object Register(string un, string p, string a)
//        {
//            User u = new User { UserName = un, Password = p, Address = a, Role = "User" };

//            DB.Users.Insert(u);

//            return Ok(u);
//        }

//        [HttpGet("{id}")]
//        public string? GetAllDevices(string Id)
//        {
//            var user = (User)DB.Users.Find(Id, null);
//            return user.Devices.ToString();
//        }

//        [HttpGet("{id}")]
//        public string? GettAllData(string Id)
//        {
//            List<Collection>? datas = new List<Collection>();
//            var user = (User)DB.Users.Find(Id, null);

//            foreach(var device in user.Devices)
//            {
//                datas.Add(DB.GetDataCollection(device));
//            }

//            return datas.ToString();
//        }

//    }
//}
