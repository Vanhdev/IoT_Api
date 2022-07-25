using BsonData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    partial class Document
    {
        public string UserName { get => GetString(nameof(UserName)); set => Push(nameof(UserName), value); }
        public string Password { get => GetString(nameof(Password)); set => Push(nameof(Password), value); }
        public string Role { get => GetString(nameof(Role)); set => Push(nameof(Role), value); }
        public string LastLogin
        {
            get => GetString(nameof(LastLogin));
            set => SetString(nameof(LastLogin), value);
        }
    }
}

namespace Actors
{
    public partial class Account : Document
    {
        #region Attributes
        #endregion

        #region API RESPONSE
        public Document CallAPI(string url, System.Document context, Func<string, Document> parse)
        {
            var request = WebRequest.Create(url);
            try
            {
                request.Method = "POST";
                request.ContentType = "application/json";


                using (var sw = new System.IO.StreamWriter(request.GetRequestStream()))
                {
                    sw.Write(context.ToString());
                }

                var response = request.GetResponse() as HttpWebResponse;
                using (var stream = response.GetResponseStream())
                {
                    using (var reader = new System.IO.StreamReader(stream, Encoding.UTF8))
                    {
                        var content = reader.ReadToEnd();
                        if (parse != null)
                        {
                            return parse(content);
                        }
                        return Parse<Document>(content);
                    }
                }
            }
            catch (Exception e)
            {
                return Error(400, e.Message);
            }
        }

        protected Document CreateApiResponse(int code, string message, object value)
        {
            var context = new Document
            {
                Code = code,
                Message = message,
                Value = value,
            };
            return context;
        }
        public virtual Document Ok(object value)
        {
            return CreateApiResponse(0, null, value);
        }
        public virtual Document Ok()
        {
            return Ok(null);
        }
        
        public virtual Document Error(int code, string message)
        {
            return CreateApiResponse(code, message, null);
        }
        #endregion
        public object Login(Document context)
        {
            var acc = context.ChangeType<Account>();

            int code = acc.TryLogin();
            

            if (code != 0)
            {
                return Error(code, null);
            }

            var user = CreateActor(acc);
            return Ok(user);
        }
        int TryLogin()
        {
            var un = UserName?.ToLower();
            if (un == null) { return 400; }

            foreach (var p in _actorMap)
            {
                var db = DB.Main.GetCollection(p.Key);
                db.Wait(null);

                var acc = db.Find<Account>(un);
                if (acc != null)
                {
                    var accPassword = acc.Password;
                    var ps = Password;
                    if (accPassword != null)
                    {
                        ps = un.JoinMD5(ps);
                    }
                    else
                    {
                        accPassword = un;
                    }
                    if (accPassword != ps) 
                    {
                        return -2;
                    }

                    this.Copy(acc);
                    this.Push(nameof(Password), accPassword != un ? "1" : "0");

                    return 0;
                }
            }

            return -1;
        }
        public object ChangePassword(Document context)
        {
            var ps = context.GetString("NewPass");
            var co = context.GetString("Confirm");
            if (ps != co)
            {
                return Error(-2, "Not match");
            }

            var un = UserName.ToLower();
            var db = DB.Main.GetCollection(Role);
            var acc = db.Find(un, null);

            var accPassword = acc.Password;
            if (accPassword != null && accPassword != un.JoinMD5(context.Password))
            {
                return Error(-1, "Password invalid");
            }
            acc.SetString(nameof(Password), un.JoinMD5(ps));
            db.Update(acc);
            return Ok();
        }
        public object Logout(Document context)
        {
            Users.Remove(this.Token);
            return Ok();
        }
        public virtual Account Create(string userName, Document profile)
        {
            var acc = CreateAccount(userName, null, this.GetType().Name);
            if (acc.Code == 0)
            {
                acc.Copy(profile);
            }
            return acc;
        }
        public static Account CreateAccount(string userName, string password, string role)
        {
            return CreateAccount(new Account
            {
                UserName = userName,
                Password = password,
                Role = role,
            });
        }
        public static Account CreateAccount(Document context)
        {
            var acc = context.ChangeType<Account>();
            var role = acc.Role;

            if (_actorMap[role] == null)
            {
                acc.Code = -2;
                return acc;
            }
            var userName = acc.UserName.ToLower();
            var password = acc.Password;
            var db = DB.Main.GetCollection(role);
            db.Wait(() =>
            {
                if (db.Find(userName, null) == null)
                {
                    if (password != null)
                    {
                        acc.Password = userName.JoinMD5(password);
                    }
                    acc.UserName = userName;
                    db.Insert(userName, acc);
                }
                else
                {
                    acc.Code = -1;
                }
            });
            return acc;
        }
        public object Execute(string name, Document context)
        {
            return this.GetType().FindMethod(name, typeof(System.Document))?
                .Invoke(this, new object[] { context });
        }
    }

    public class AutoPassword
    {
        const int UpperCase = 1;
        const int LowerCase = 2;

        public static string Generate(int length, int flags)
        {
            var lst = new List<string> { 
                "0123456789",
            };
            var s = "";

            return s;
        }
    }
}

namespace System
{
    partial class DB
    {
        static Collection _accounts;
        public static Collection Accounts
        {
            get
            {
                if (_accounts == null)
                {
                    _accounts = GetCollection<Actors.Account>();
                    if (_accounts.Count == 0)
                    {
                        _accounts.Wait(() =>
                        {
                            const string ad = nameof(Actors.Admin);
                            Actors.Account.CreateAccount(ad, ad.ToLower(), ad);
                        });
                    }
                }
                return _accounts;
            }
        }
    }
}
