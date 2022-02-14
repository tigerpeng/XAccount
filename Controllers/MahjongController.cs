using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using XAccount.Services;
using XAccount.Models;
using XAccount.Entities;
using XAccount.Helpers;
using Newtonsoft.Json.Linq;     //JObject

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace XAccount.Controllers
{
    [Route("api/[controller]")]
    public class MahjongController : Controller
    {
        private IMahjongService _mahjong;

        public MahjongController(IMahjongService service)
        {
            _mahjong = service;

        }

        [HttpGet]
        [Route("Profile")]
        public ActionResult Profile()
        {
            try
            {
                long myID = Convert.ToInt64(User.Identity.Name);
                MjProfileModel mjProfile = _mahjong.GetProfile(myID);
                return Ok(mjProfile);
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// 转账
        /// </summary>
        /// <returns>返回资金状况</returns>
        [HttpPost]
        [Route("Transfer")]
        public IActionResult Transfer([FromBody] TransferModel tr )
        {
            try
            {
                long myID = Convert.ToInt64(User.Identity.Name);
                //System.Math.Abs();
                _mahjong.Transfer(myID, tr.To, tr.Amount);

                return Ok("");
                //return Json(profile);
            }
            catch (AppException ex)
            {
                // return error message if there was an exception
                return BadRequest(ex.Message);
            }
        }


        /// <summary>
        /// 检资 锁定用户资金  返回 记录 id和信息
        /// </summary>
        /// <returns>返回资金状况</returns>
        [HttpPost]
        [Route("CheckChips")]
        public IActionResult CheckChips([FromBody] JObject json)
        {
            try
            {
                //玩家id
                long[] gambles = new Int64[json["players"].Count()];
                for (int i = 0; i < json["players"].Count(); i++)
                {
                    gambles[i] = (int)json["players"][i];
                }
                //本局抵押
                long    bossid  =(long)json["bossid"];
                string  symbol  =(string)json["symbol"];
                int     pawn    =(int)json["pawn"];                                   //需要抵押的数量
                float   feeType =(float)json["tablefee"];                             //茶费
                string  desp    =(string)json["desp"];                                //本局说明
                
                var record = _mahjong.CheckChips(bossid,symbol,pawn, feeType,desp,gambles);

                return Ok(record);
                //return Json(profile);
            }
            catch (AppException ex)
            {
                // return error message if there was an exception
                return BadRequest(ex.Message);
            }
            catch (System.Exception ex)
            {
                // return error message if there was an exception
                return BadRequest(ex.Message);
            }
        }



        /*
         {"pawn":10,"players":[10001,10002,10003,10004]}
         {"record":1,"scores":[-3,1,1,1]}
         */
        /// <summary>
        /// 分割输家的筹码
        /// </summary>
        /// <returns>返回资金状况</returns>
        [HttpPut]
        [Route("SpliteChips")]
        public IActionResult SpliteChips([FromBody] JObject json)
        {
            try
            {
                //玩家
                List<Gamble> gambles = new List<Gamble>();
                for (int i = 0; i < json["scores"].Count(); i++)
                {
                    Gamble g = json["scores"][i].ToObject<Gamble>();

                    gambles.Add(g);
                }
                long rid        = (long)json["record"];                         //记录id
                string result   = (string)json["result"];                       //结果

                _mahjong.SpliteChips(rid, result,gambles);

                return Ok("筹码分割完成！");
            }
            catch (AppException ex)
            {
                // return error message if there was an exception
                return BadRequest(ex.Message);
            }
            catch (System.Exception ex)
            {
                // return error message if there was an exception
                return BadRequest(ex.Message);
            }
        }


        //返回所有茶馆的房间信息  desk=room 一个房间一张桌子
        // teahouse desk
        //[AllowAnonymous]
        [HttpGet]
        [Route("Desks")]
        public IActionResult Desks()
        {
            try
            {
                var desks= _mahjong.GetAllDesks();

                return Ok(desks);
            }
            catch (AppException ex)
            {
                // return error message if there was an exception
                return BadRequest(ex.Message);
            }
            catch (System.Exception ex)
            {
                // return error message if there was an exception
                return BadRequest(ex.Message);
            }
        }


        

        // GET: api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            _mahjong.AddScore(10001, 10000);
            _mahjong.AddScore(10002, 10000);
            _mahjong.AddScore(10003, 10000);
            _mahjong.AddScore(10004, 10000);
            _mahjong.AddScore(1234567, 10000);

            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
