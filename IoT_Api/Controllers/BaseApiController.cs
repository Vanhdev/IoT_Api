using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using BsonData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc;

namespace System.Web
{
    public class App
    {
        public static string Domain { get; set; }
    }
}

namespace IoT_Api.Controllers
{
    using Actors;
    using System.Web;

    public class BaseApiController : ControllerBase
    {
        protected virtual object Execute(object requestContext)
        {
            //if (App.Domain == null)
            //{
            //    var uri = this.Request.RequestUri;
            //    App.Domain = uri.Scheme + "://" + uri.Host;
            //    if (uri.Port != 0)
            //    {
            //        App.Domain += string.Format($":{uri.Port}");
            //    }
            //}

            if (requestContext == null) { return null; }
            var context = Document.Parse(requestContext.ToString());

            var url = context.Pop<string>("#url");
            if (url == null) return null;

            var s = url.Split('/');
            var token = context.Token;

            var actor = new User();
            if (!string.IsNullOrEmpty(token))
            {
                actor = Account.FindActorByToken(token);
                if (actor == null)
                {
                    return tokenError;
                }
            }

            var actionName = s[1].ToLower();
            var method = actor.GetType().FindMethod(actionName, typeof(Document));

            try
            {
                if (method != null)
                {
                    context = context.ValueContext.ChangeType<Document>();

                    var res = method.Invoke(actor, new object[] { context }) as System.Document;
                    res?.Push("#url", url.ToLower().Replace('/', '_'));

                    return res;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return unknowError;
        }

        static Document unknowError = new Document { Code = 400, Message = "Unknow Error." };
        static Document tokenError = new Document { Code = 100, Message = "Token Invalid." };
    }

}
