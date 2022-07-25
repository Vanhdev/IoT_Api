using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Actors
{
    public partial class Admin : User
    {
        public object CreateUser(Document context)
        {
            var res = CreateAccount(context);
            var code = res.Code;
            if (code != 0)
            {
                res.Message = code == -2 ? "Role invalid" : "Account exists";
                return res;
            }
            return Ok();
        }
        public object RemoveUser(Document context)
        {
            var doc = DB.Main[context.ObjectId];
            if (doc == null)
            {
                return Error400;
            }
            var db = DB.Main.GetCollection(doc.Role);
            db.Delete(doc);

            return Ok();
        }
    }
}
