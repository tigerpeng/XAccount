using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;

using XAccount.Models;
using XAccount.Services;
using XAccount.Helpers;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace XAccount.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class TalksController : Controller
    {
        private ITalksService           _talksService;
        private IWebHostEnvironment _hostingEnvironment;

        public TalksController(ITalksService talkService, IWebHostEnvironment env)
        {
            _talksService       = talkService;
            _hostingEnvironment = env;

        }

        // POST api/values
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]TalksRequestModel model)
        {
            try
            {
                if (model == null)
                    return BadRequest("参数不能为空!");

                var result = await _talksService.TalksCreate(model);
                return Ok(result);
            }catch (AppException ex){
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPut]
        public async Task<IActionResult> Put([FromBody] TalksFinishModel model)
        {
            try
            {
                if (model == null)
                    return BadRequest("参数不能为空!");

                var result = await _talksService.TalksRefresh(model);
                return Ok(result);
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpDelete]
        public async Task<IActionResult> Delete([FromBody]TalksFinishModel model)
        {
            try
            {
                if (model == null)
                    return BadRequest("参数不能为空!");

                ////只有经过内部服务器才能调用该接口
                //if(!_talksService.IsSignlingServer())
                //    return BadRequest("需要服务器权限-请通过信令服务器签名");

                var result = await _talksService.TalksEnd(model);
                return Ok(result);
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }

            //try
            //{
            //    //string str = "12345_15";
            //    //string[] sArray = str.Split('_');
            //    if (model == null)
            //        return BadRequest("参数不能为空!");

            //    //解析获得
            //    string[] sArray = model.Message.Split('_');
            //    long recordId   = Int64.Parse(sArray[0]);
            //    int minutes     = Int32.Parse(sArray[1]);

            //    _talksService.TalksEnd(model.Result,recordId,minutes,model.Signature,model.PublicKey);
            //    return Ok("ok");
            //}catch (AppException ex){
            //    return BadRequest(ex.Message);
            //}
        }





        [AllowAnonymous]
        [HttpGet]
        [Route("income")]
        public async Task<IActionResult> QueryIncomeDetals()
        {
            try
            {
                long myID = Convert.ToInt64(User.Identity.Name);

                var result = await _talksService.GetTalksChargeDetails(myID, true);
                return Ok(result);
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("expense")]
        public async Task<IActionResult> QueryExpensesDetals()
        {
            try
            {
                long myID = Convert.ToInt64(User.Identity.Name);

                var result = await _talksService.GetTalksChargeDetails(myID, false);
                return Ok(result);
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }


        }
    }
}
