using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;

using XAccount.Data;
using XAccount.Entities;
using XAccount.Services;
using XAccount.Helpers;

using Newtonsoft.Json.Linq;     //JObject

using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNet.OData.Query;

namespace XAccount.Controllers.OData
{
    [EnableQuery]
    [ODataRoutePrefix("Users")]
    public class Teachers : ODataController
    {
        private IUserService _userService;
        private IHostingEnvironment _hostingEnvironment;
        private DataContext _db;

        public Teachers(DataContext context,IUserService userService, IHostingEnvironment hostingEnvironment)
        {
            _db = context;

            _userService = userService;
            _hostingEnvironment = hostingEnvironment;
        }

        //[EnableQuery]
        //[ODataRoute]
        //public IQueryable<User> Get()
        //{
        //    return _db.Users;
        //}
        //[EnableQuery]
        //[ODataRoute] 
        //public SingleResult<User> Get([FromODataUri] int key)
        //{
        //    IQueryable<User> result = _db.Users.Where(p => p.Id == key);
        //    return SingleResult.Create(result);
        //}

        [HttpGet]
        [EnableQuery]
        [ODataRoute]                //必须有
        public IActionResult Get()
        {
            try
            {
                //var profiles = _userService.GetTalksActor();
                var profiles = _userService.GetODataUsers();


                return Ok(profiles);
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet]
        [EnableQuery]
        //[ODataRoute]
        [ODataRoute("({id})")]
        public IActionResult GetAddress([FromODataUri] int id)
        {
            var profiles = _userService.GetODataUsers(id);
            return Ok(profiles);
        }

    }
}