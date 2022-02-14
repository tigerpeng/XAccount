using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using XAccount.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace XAccount.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class WorldController : Controller
    {
        private IUserService _userService;

        public WorldController(IUserService userService)
        {
            _userService = userService;
        }


        //匿名对象 ，集合的使用
        //https://www.cnblogs.com/sntetwt/p/4878299.html 
        // GET: api/values
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(_userService.GetWorld());


            //xr虚拟世界海报
            var worlds = new[]
                {
                    new {id=1,  name = "元宇宙大厅", gameServer="game.ournet.club:2345",avServer=2020, path = "#",picture="/static/img/r/0.jpg" ,createTime ="2022-01-01",desp ="游戏大厅"},
                    new {id=2,  name = "测试房间",   gameServer="192.168.0.100:2345",avServer=2020, path = "#",picture="/static/img/r/0.jpg" ,createTime ="2022-01-01",desp ="局域网软件调试"},
                    new {id=3,  name = "俩娃",      gameServer="game.ournet.club:2345",avServer=2020, path = "#",picture="/static/img/r/2.jpg" ,createTime ="2022-01-01",desp ="本地相亲"},
                    new {id=101,name = "卡五星",    gameServer="game.ournet.club:2345",avServer=2020,  path = "/tea_house_list/",picture="/static/img/r/1.jpg" ,createTime ="2022-01-01",desp ="多人棋牌游戏"}
                };


            return Ok(worlds);
        }

        // GET api/values/5
        [AllowAnonymous]
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            var world = new { id = 1, name = "标题一", path = "内容一", picture = "", desp = "" };

            return Ok(world);
        }



        [AllowAnonymous]
        [HttpGet]
        [Route("playerinfo")]
        public IActionResult PlayerInfo([FromQuery(Name = "rid")] int roomID, [FromQuery(Name = "pwd")] string password)
        {

           return Ok(_userService.GetXRWorldInfo(roomID, password));

            //long fromID = Convert.ToInt64(User.Identity.Name);
            ////xr用户合法信息
            //var player = new {rid=roomID, uid = 101010, name = "合法用户", avatar = "def", birthday="2000-1-1"};
            ////errMsg 字段返回错误信息
            //return Ok(player);
        }



        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
