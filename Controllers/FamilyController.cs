using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;

//for pic upload
using System.IO;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;     //JObject

using XAccount.Models;
using XAccount.Services;
using XAccount.Helpers;
using XAccount.Entities;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace XAccount.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class FamilyController : Controller
    {
        private IFamilyService _familyService;
        private IWebHostEnvironment _hostingEnvironment;

        public FamilyController(IFamilyService talkService, IWebHostEnvironment hostingEnvironment)
        {
            _familyService = talkService;
            _hostingEnvironment = hostingEnvironment;
        }


        // GET: api/values
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Get()
        {
            var addF    = _familyService.GetMyAddFamily();
            var otherF  = _familyService.GetAll(true);//不含我的家族
            var o = new
            {
                My = addF,
                Other = otherF
            };
            return Ok(o);
        }

        // GET: api/values
        [AllowAnonymous]
        [HttpGet]
        [Route("All")]
        public IActionResult GetAll()
        {
            try
            {
                var f = _familyService.GetAll(false);
                return Ok(f);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }

        [HttpGet]
        [Route("Owner")]
        public IActionResult GetOwner()
        {
            try
            {
                var f = _familyService.GetMyOwnerFamily();
                return Ok(f);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }


        // GET api/values/5
        [AllowAnonymous]
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public IActionResult Post([FromBody]Family model)
        {
            try
            {
                if(model==null)
                    throw new AppException("参数不能为空");

                var f = _familyService.FamilyCreate(model);
                return Ok(f);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public IActionResult Put(long id, [FromBody]Family model)
        {
            try
            {
                var f = _familyService.FamilyUpdate(id,model);
                return Ok(f);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }


        // PUT api/values/5
        [HttpPut("join/{id}")]
        public IActionResult Put(long id)
        {
            try
            {
                var f = _familyService.FamilyJoin(id);
                return Ok(f);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }

        [AllowAnonymous]
        [HttpGet("membersinfo")]
        public IActionResult membersinfo()
        {
            try
            {
                var f = _familyService.MembersInfo();
                return Ok(f);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }


        [HttpGet("scoreinfo")]
        public IActionResult scoreinfo()
        {
            try
            {
                var f = _familyService.ScoreInfo();
                return Ok(f);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }

        //上分
        [HttpPut("scoreup")]
        public IActionResult scoreup([FromBody] TransferModel model)
        {
            try
            {
                var f = _familyService.ScoreUp(model);
                return Ok(f);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }

        //下分
        [HttpPut("scoredown")]
        public IActionResult scoredown([FromBody] TransferModel model)
        {
            try
            {
                var f = _familyService.ScoreDown(model);
                return Ok(f);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }

        /// <summary>
        ///  用户转帐金币
        /// </summary>
        [HttpPut]
        [Route("Transfer")]
        public IActionResult Transfer([FromBody] TransferModel model)
        {

            try
            {
                 _familyService.TransferScore(model);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok("");
        }



        // DELETE api/values/5
        [HttpDelete("{id}")]
        public IActionResult Delete(long id)
        {
            try
            {
                _familyService.LeaveFamily((id>0?true:false));
                return Ok("");
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }

        /// <summary>
        ///  更新家族海报
        /// </summary>
        [HttpPost, DisableRequestSizeLimit]
        [Route("Poster")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public ActionResult FamilyPoster()
        {
            try
            {
                long fid= Convert.ToInt64(Request.Form["fid"].ToString());
                string avatarFileName = fid.ToString() + ".jpg";

                if (_familyService.FamilyExsit(fid))
                {
                    var file = Request.Form.Files[0];
                    string folderName = "img/f/";
                    string webRootPath = _hostingEnvironment.WebRootPath;
                    string newPath = Path.Combine(webRootPath, folderName);
                    if (!Directory.Exists(newPath))
                    {
                        Directory.CreateDirectory(newPath);
                    }
                    if (file.Length > 0)
                    {
                        string fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                        //string fullPath = Path.Combine(newPath, fileName);
                        string fullPath = Path.Combine(newPath, avatarFileName);
                        using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            file.CopyTo(stream);
                        }
                    }

                    string familyFile = folderName + avatarFileName;

                    //返回图片路径
                    JObject ojbReturn = new JObject();
                    ojbReturn.Add("relative_url", familyFile);
                    return Ok(ojbReturn);
                }
                

            }catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
                //return Json("Upload Failed: " + ex.Message);
            }

            return BadRequest("error");
        }



        //class end
    }
}
