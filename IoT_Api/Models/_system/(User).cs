using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Actors
{
    public partial class User : Account
    {
        public Document Error400 => new Document { Code = 400, Message = "Not Found" };
    }
}

namespace Actors
{
    using BsonData;

    public partial class Account
    {
        static protected BsonDataMap<Type> _actorMap;
        static public BsonDataMap<User> Users { get; private set; } = new BsonDataMap<User>();

        static public void LoadActorConfig(string path)
        {
            Console.WriteLine("Load actor config: " + path);

            _actorMap = new BsonDataMap<Type>();
            _actorMap.Add("account", typeof(Account));
            _actorMap.Add("admin", typeof(Admin));

            try
            {
                foreach (var fi in new System.IO.DirectoryInfo(path).GetFiles())
                {
                    var dll = Assembly.LoadFile(fi.Name + "Actor" + ".dll");
                    foreach (var type in dll.GetTypes())
                    {
                        if (type.FullName.Contains("Actor"))
                        {
                            _actorMap[type.Name] = type;
                        }
                    }
                }
            }
            catch
            {
            }
        }
        static public void RegisterActors(string space, params string[] typenames)
        {
            if (_actorMap == null) { _actorMap = new BsonDataMap<Type>(); }
            foreach (var name in typenames)
            {
                _actorMap.Add(name.ToLower(), Type.GetType(space + "." + name));
            }
        }
        static public User CreateActor(Account acc)
        {
            var time = DateTime.Now;
            var token = acc.UserName.JoinMD5(time.Ticks);

            var type = _actorMap[acc.Role];
            if (type == null)
            {
                type = typeof(User);
            }

            acc.Push("#token", token);
            var user = Activator.CreateInstance(type) as User;
            user.Copy(acc);

            user.Push("#login-time", time);

            Users[token] = user;
            return user;
        }
        static public User AddExternalUser(Document context)
        {
            var type = _actorMap[context.GetString("Role")];

            var user = Activator.CreateInstance(type) as User;
            user.Copy(context);

            Users[context.GetString("#token")] = user;
            return user;
        }
        static public User FindActorByToken(string token)
        {
            return Users[token];
        }
    }
}
