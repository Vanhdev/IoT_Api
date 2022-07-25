using BsonData;

namespace Actors
{
    public partial class User : Account
    {
        public string Address { get => GetString(nameof(Address)); set => Push(nameof(Address), value); }

        
    }
}
namespace System
{     
    partial class DB
    {
        static Collection? _users;
        static public Collection Users
        {
            get
            {
                if (_users == null)
                {
                    _users = GetCollection<Actors.User>();
                }
                return _users;
            }
        }
    }
}
