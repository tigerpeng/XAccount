using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;


using System.IO;
using System.Data;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;

using Microsoft.EntityFrameworkCore;//include
using XAccount.Entities;
using XAccount.Helpers;
using XAccount.Data;
using XAccount.Models;
using Newtonsoft.Json.Linq;     //JObject


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860
namespace XAccount.Controllers
{
    [Route("api/[controller]")]
    public class ToolsController : Controller
    {
        private IWebHostEnvironment _hostingEnvironment;
        private DataContext _db;


        public ToolsController(DataContext context, IWebHostEnvironment env)
        {
            _db = context;
            _hostingEnvironment = env;
        }

        [HttpPut("report")]
        public async Task<IActionResult> Put([FromBody] JObject model)
        {
            try
            {
                int record = (int)model["record"];
                int sn = (int)model["sn"];
                var report = await _db.ReportNetworks.FirstOrDefaultAsync(t => t.Record == record && t.SN == sn);
                if (report == null)
                {
                    report = new ReportNetwork
                    {
                        Record = record,
                        SN = sn
                    };
                    if(model["produceV"]!=null)
                        report.ProduceV = (string)model["produceV"];
                    if (model["lostV"] != null)
                        report.LostV= (string)model["lostV"];
                    if (model["bufferV"] != null)
                        report.BufferV = (string)model["bufferV"];
                    if (model["receiveV"] != null)
                        report.ReceiveV = (string)model["receiveV"];

                    if (model["produceA"] != null)
                        report.ProduceA = (string)model["produceA"];
                    if (model["lostA"] != null)
                        report.LostA = (string)model["lostA"];
                    if (model["bufferA"] != null)
                        report.BufferA = (string)model["bufferA"];
                    if (model["receiveA"] != null)
                        report.ReceiveA = (string)model["receiveA"];

                    _db.ReportNetworks.Add(report);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    if (model["receiveV"] != null)
                    {
                        report.ReceiveV = (string)model["receiveV"];

                        if (model["receiveA"] != null)
                            report.ReceiveA = (string)model["receiveA"];
                    }
                    else
                    {
                        if (model["produceV"] != null)
                            report.ProduceV = (string)model["sendV"];
                        if (model["lostV"] != null)
                            report.LostV = (string)model["lostV"];
                        if (model["bufferV"] != null)
                            report.BufferV = (string)model["bufferV"];

                        if (model["produceA"] != null)
                            report.ProduceA = (string)model["produceA"];
                        if (model["lostA"] != null)
                            report.LostA = (string)model["lostA"];
                        if (model["bufferA"] != null)
                            report.BufferA = (string)model["bufferA"];
                    }
                    _db.ReportNetworks.Update(report);
                    await _db.SaveChangesAsync();
                }
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok("");
        }



        /// <summary>
        ///  上传文件  上传用户自定义头像 支持多个文件
        /// </summary>
        ///<remarks>
        /// Sample request:
        /// 
        ///     POST /api/account/UploadFiles HTTP/1.1
        ///     Host: localhost:5001
        ///     Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6IjEzIiwibmJmIjoxNTIzODAyOTMwLCJleHAiOjE1MjQ0MDc3MzAsImlhdCI6MTUyMzgwMjkzMH0.ZF3Rb4ckWs1GkZ915bQ5IJy7_iypKFysuGgjkpxtMjs
        ///     Cache-Control: no-cache
        ///     Content-Type: multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW
        ///
        ///     ------WebKitFormBoundary7MA4YWxkTrZu0gW
        ///     Content-Disposition: form-data; name="file"; filename="WechatIMG71.png"
        ///     Content-Type: image/png
        ///
        ///     ------WebKitFormBoundary7MA4YWxkTrZu0gW--
        /// 
        ///  </remarks>
        /// <response code="200">成功 </response>
        /// <response code="400">错误 返回错误信息（bady 中）</response>   
        [HttpPost, DisableRequestSizeLimit]
        [Route("UploadFiles")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public ActionResult UploadFiles()
        {
            try
            {
                //遍历多个文件
                var files = Request.Form.Files;
                for (int i = 0; i < files.Count; i++)
                {
                    var f = files[i];

                    string webRootPath = _hostingEnvironment.WebRootPath;           //网站根目录
                    string newPath = Path.Combine(webRootPath, "upload/files/");    //存放路径
                    if (!Directory.Exists(newPath))
                    {
                        Directory.CreateDirectory(newPath);
                    }
                    if (f.Length > 0)
                    {
                        string fileName = ContentDispositionHeaderValue.Parse(f.ContentDisposition).FileName.Trim('"');
                        string fullPath = Path.Combine(newPath, fileName);                  //使用上传的文件名

                        //long myID = Convert.ToInt64(User.Identity.Name);
                        //string avatarFileName = myID.ToString() +"_"+i.ToString()+".jpg";
                        //string fullPath = Path.Combine(newPath, avatarFileName);          //使用新文件名
                        using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            f.CopyTo(stream);
                        }

                        //更新到数据库
                        //Random ran = new Random();
                        //int RandKey = ran.Next(0, 9999);
                        //var upt = new DTOUserInfo { Avatar = "/img/a/" + avatarFileName + "?r=" + RandKey.ToString() };
                        //_db.UpdateProfile(myID, upt);
                    }
                }
                return Ok("ok");
                //return Json("Upload Successful.");
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
                //return Json("Upload Failed: " + ex.Message);
            }
        }

        // postman http://www.cnblogs.com/shimh/p/6094410.html

        // GET: api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
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
