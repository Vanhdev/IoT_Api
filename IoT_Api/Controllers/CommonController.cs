using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc;

namespace IoT_Api.Controllers
{

    [Route("api/common")]
    [ApiController]
    public class CommonController : BaseApiController
    {
        public object Post(object info)
        {
            return info == null ? null : Execute(info);
        }
    }
}
