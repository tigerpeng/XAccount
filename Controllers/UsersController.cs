using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

//for pic upload
using System.IO;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;

using System.Linq;
using Newtonsoft.Json.Linq;     //JObject

//for google webp
using ImageProcessor;
using ImageProcessor.Plugins.WebP.Imaging.Formats;

using XAccount.Services;
using XAccount.Models;
using XAccount.Helpers;
using XAccount.Entities;
using System.Collections.Generic;

using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNet.OData.Query;

using System.Threading.Tasks;


//参考ODATA 使用  https://www.cnblogs.com/kenwoo/p/10360260.html

namespace XAccount.Controllers
{
    [Authorize]
    //[Produces("application/json")]
    //[Consumes("application/json", "multipart/form-data")]//此处为新增
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    //public class UsersController : Controller
    {
        private IUserService _userService;

        private IWebHostEnvironment _hostingEnvironment;

        public UsersController(IUserService userService, IWebHostEnvironment env)
        {
            _userService = userService;

            _hostingEnvironment = env;
        }

        [AllowAnonymous]
        [HttpPut]
        [Route("Online")]
        public IActionResult Online([FromBody] UserOnlineModel model)
        {
            //向用户手机发送验证码
            _userService.UserOnline(model.Uid, model.Online);
            return Ok("ok!");
        }


        /// <summary>
        /// 发送手机验证码
        /// </summary>
        /// <returns>The check.</returns>
        /// <param name="reg">Reg.</param>
        [AllowAnonymous]
        [HttpPost]
        [Route("SendCheck")]
        public IActionResult SendCheck([FromBody]AuthenticateModel model)
        {
            //向用户手机发送验证码
            if (_userService.SendVerifyCode(model.Phone))
                return Ok("send ok!");

            return BadRequest("OK");
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("Login")]
        public IActionResult Login([FromBody] AuthenticateModel model)  
        {
            try
            {
                if (model == null)
                    return BadRequest("参数不能为空!");

                var user = _userService.LoginWithPhone(model.Phone, model.Password);

                if (user == null)
                    return BadRequest(new { message = "MobilePhone or verifycode is incorrect" });

                return Ok(user);
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetAll()
        {
            _userService.TransferAutoConfirm();

            var users = _userService.GetAll();
            return Ok(users);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("New")]
        public IActionResult NewUser()
        {
            try
            {
                return Ok(_userService.GetNewUser());
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        ///  社区
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        [Route("Social")]
        public IActionResult Social()
        {
            try
            {
                return Ok(_userService.Social());
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [AllowAnonymous]
        [HttpGet]
        [Route("Profile")]
        public IActionResult GetProfile()
        {
            var users = _userService.GetAll();
            return Ok(users);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("AllProfile")]
        public IActionResult Labels()
        {
            var users = _userService.GetAllProfile();
            return Ok(users);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("myself")]
        public IActionResult GetMyself()
        {
            return Ok(_userService.GetMyProfile());
        }

        /// <summary>
        ///  用户提现
        /// </summary>
        [HttpPut]
        [Route("WithDraw")]
        public IActionResult WithDraw([FromBody] WithDrawModel model)
        {
            try
            {
                _userService.WithDraw(model);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok("");
        }

        /// <summary>
        ///  用户转帐金币
        /// </summary>
        [HttpPut]
        [Route("Transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferModel model)
        {

            try
            {
                await _userService.TransferCurrency(model);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok("");
        }

        /// <summary>
        ///  用户确认转帐金币  代理--》确认   用户--》申述 
        /// </summary>
        [HttpPut]
        [Route("TransferDelay")]
        public IActionResult TransferConfim([FromBody] TransferConfirmModel model)
        {
            try
            {
                _userService.TransferConfirm(model);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok("");
        }

        /// <summary>
        ///  获得在途资金
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        [Route("CoinsDelay")]
        public IActionResult CoinsDelay()
        {
            try
            {
                long uid = Convert.ToInt64(User.Identity.Name);
                var ret = new
                {
                    User = _userService.GetOnTheWayCoins(uid,false),
                    Agent = _userService.GetOnTheWayCoins(uid,true),
                };

                return Ok(ret);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        ///  金币流水
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        [Route("BanlanceCoin")]
        public IActionResult BanlanceCoin()
        {
            try
            {
                return Ok(_userService.Balance(1));
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        /// <summary>
        ///  金豆流水
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        [Route("BanlanceBean")]
        public IActionResult BanlanceBean()
        {
            try
            {
                return Ok(_userService.Balance(2));
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        /// <summary>
        ///  积分流水
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        [Route("BanlanceScore")]
        public IActionResult BanlanceScore()
        {
            try
            {
                return Ok(_userService.Balance(3));
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        ///  更新自己的信息  name avatar sex birthday .....
        /// </summary>
        [AllowAnonymous]
        [HttpPut]
        [Route("Profile")]
        public IActionResult UpdateProfile([FromBody] JObject jusr)
        {
            try
            {
                long myID = Convert.ToInt64(User.Identity.Name);

                //myID = 10002;

                var usr = _userService.UpdateProfile(myID, jusr);
                return Ok("");
                //if (usr != null)
                //    return Ok("更新用户信息成功!");
                //else
                //    return BadRequest("用户不存在!");
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }


        /// <summary>
        ///  更新自己的头像
        /// </summary>
        [HttpPost, DisableRequestSizeLimit]
        [Route("UploadAvatar")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public ActionResult PictureAuthor()
        {

            long myID = Convert.ToInt64(User.Identity.Name);
            string avatarFileName = myID.ToString() + ".jpg";

            try
            {
                var file = Request.Form.Files[0];
                string folderName = "img/a/";
                string webRootPath = _hostingEnvironment.WebRootPath;
                string newPath = Path.Combine(webRootPath, folderName);
                if (!Directory.Exists(newPath))
                {
                    Directory.CreateDirectory(newPath);
                }
                if (file.Length > 0)
                {
                    /*
                    string fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                    string fullPath = Path.Combine(newPath, avatarFileName);
                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }*/

                    string fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                    string fullPath = Path.Combine(newPath, avatarFileName);
                    // Then save in WebP format
                    using (var webPFileStream = new FileStream(fullPath, FileMode.Create))
                    {
                        using (ImageFactory imageFactory = new ImageFactory(preserveExifData: false))
                        {
                            imageFactory.Load(file.OpenReadStream())
                                        //.Format(new WebPFormat())
                                        .Resize(new System.Drawing.Size(120, 120))
                                        .Quality(80)
                                        .Save(webPFileStream);
                        }
                    }
                }

                string avatFile = folderName + avatarFileName;

                JObject jObject = new JObject();
                jObject.Add("avatar", avatFile);
                _userService.UpdateProfile(myID, jObject);


                JObject ojbReturn = new JObject();
                ojbReturn.Add("relative_url", avatFile);
                return Ok(ojbReturn);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
                //return Json("Upload Failed: " + ex.Message);
            }
        }

        /// <summary>
        ///  更新自己的头像 保存为WEBP 格式   参考 https://blog.elmah.io/convert-images-to-webp-with-asp-net-core-better-than-png-jpg-files/
        /// </summary>
        [HttpPost, DisableRequestSizeLimit]
        [Route("Avatar")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public ActionResult WebpAvatar()
        {

            long myID = Convert.ToInt64(User.Identity.Name);
            string avatarFileName = myID.ToString() + ".webp";

            try
            {
                var file = Request.Form.Files[0];
                string folderName = "img/a/";
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
                    // Then save in WebP format
                    using (var webPFileStream = new FileStream(fullPath, FileMode.Create))
                    {
                        using (ImageFactory imageFactory = new ImageFactory(preserveExifData: false))
                        {
                            imageFactory.Load(file.OpenReadStream())
                                        .Format(new WebPFormat())
                                        .Resize(new System.Drawing.Size(120,120))
                                        .Quality(80)
                                        .Save(webPFileStream);
                        }
                    }
                }

                string avatFile = folderName + avatarFileName;

                //跟新用户资料
                JObject jObject = new JObject();
                jObject.Add("avatar", avatFile);
                _userService.UpdateProfile(myID, jObject);

                //返回保存的URL
                JObject ojbReturn = new JObject();
                ojbReturn.Add("relative_url", avatFile);
                return Ok(ojbReturn);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
                //return Json("Upload Failed: " + ex.Message);
            }
        }






    }
}
