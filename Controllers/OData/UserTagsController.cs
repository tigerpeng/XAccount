using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860
using XAccount.Data;
using XAccount.Entities;
using Newtonsoft.Json.Linq;     //JObject

using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNet.OData.Query;

namespace XAccount.Controllers.OData
{
    [EnableQuery]
    [ODataRoutePrefix("UserTags")]
    public class UserTagsController : ODataController
    {
        private DataContext _db;

        public UserTagsController(DataContext context)
        {
            _db = context;
        }

        [EnableQuery]
        public IQueryable<UserTag> Get()
        {
            return _db.UserTags;
        }


        [EnableQuery]
        public SingleResult<UserTag> Get([FromODataUri] int key)
        {
            IQueryable<UserTag> result = _db.UserTags.Where(p => p.Id == key);
            return SingleResult.Create(result);
        }
    }
}
