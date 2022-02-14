using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using XAccount.Models;


using System.IO;//fileinfo
using Microsoft.AspNetCore.Hosting;
using System.Net.Http.Headers;
//导入导出到excell epplus
using OfficeOpenXml;
using OfficeOpenXml.Table;


using Newtonsoft.Json;
using Newtonsoft.Json.Linq;     //JObject

using XAccount.Helpers;
using XAccount.Services;
using XAccount.Data;
using XAccount.Entities;
using Microsoft.EntityFrameworkCore;
using System.Dynamic;
using Microsoft.AspNetCore.Mvc.ViewFeatures;


namespace XAccount.Controllers
{
    public static class impFunctions
    {
        public static ExpandoObject ToExpando(this object anonymousObject)
        {
            IDictionary<string, object> anonymousDictionary = HtmlHelper.AnonymousObjectToHtmlAttributes(anonymousObject);
            IDictionary<string, object> expando = new ExpandoObject();
            foreach (var item in anonymousDictionary)
                expando.Add(item);
            return (ExpandoObject)expando;
        }
    }

    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;

        private IWebHostEnvironment _hostingEnvironment;
        private IUserService _userService;

        private DataContext _db;



        public AdminController(DataContext context,ILogger<AdminController> logger, IWebHostEnvironment hostingEnvironment, IUserService dbUser)
        {
            _db = context;

            _logger = logger;

            _hostingEnvironment = hostingEnvironment;
            _userService = dbUser;
        }

        public IActionResult Index()
        {
            //掩饰 json 使用，先生成字符串，再用3种方法解析额

            //先创建一个匿名对象转化成字符串
            var o = new
            {
                a = 1,
                b = "Hello, World!",
                c = new[] { 1, 2, 3 },
                d = new Dictionary<string, int> { { "x", 1 }, { "y", 2 } }
            };
            var json = JsonConvert.SerializeObject(o);

            {
                //第一种方法　匿名类
                var anonymous = new { a = 0, b = String.Empty, c = new int[0], d = new Dictionary<string, int>() };
                var o2 = JsonConvert.DeserializeAnonymousType(json, anonymous);

                Console.WriteLine(o2.b);
                Console.WriteLine(o2.c[1]);
            }
            {//第二种做法（匿名类）：
                var o3 = JsonConvert.DeserializeAnonymousType(json, new { a = 0, c = new int[0], d = new Dictionary<string, int>() });
                Console.WriteLine(o3.d["y"]);
                //DeserializeAnonymousType 只是借助这个匿名对象参数(anonymous) 反射类型而已，也就是说它和反序列化结果并非同一个对象。正如 o3 那样，我们也可以只提取局部信息。
            }
            {
                //第三种做法（索引器）
                var o2 = JsonConvert.DeserializeObject(json) as JObject;

                string t = (string)o2["e"];

                Console.WriteLine("jobject 序列化");
                Console.WriteLine((int)o2["a"]);
                Console.WriteLine((string)o2["b"]);
                Console.WriteLine(o2["c"].Values().Count());
                Console.WriteLine((int)o2["c"][0]);
                Console.WriteLine((int)o2["c"][1]);
                Console.WriteLine((int)o2["d"]["y"]);

                //转换
                Int64[] items = new Int64[o2["c"].Count()];
                for (int i = 0; i < o2["c"].Count(); i++)
                {
                    items[i] = (int)o2["c"][i];
                }
                var values = o2["d"].ToObject<Dictionary<string, Int32>>();
                foreach (var item in values)
                {
                    string a = item.Key;
                    Int32 b = item.Value;
                }
                //Int64[] players = (Int64[])o2["c"].ToArray(typeof(Int64));
                if (o2["name"] == null)
                {
                    int a = 0; a++;
                }

                /*
                 * 实际上，我们也可以直接反序列化为 JObject，然后通过索引器直接访问。JObject、JProperty 等都继承自 JToken，它重载了基元类型转换操作符，我们可以直接得到实际结果
                */
            }


            return View();
        }
        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "在控制器中设置 视图中获得.";

            ViewData["Penghong"] = "我是.....";
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        //导出到excell
        public IActionResult ExportWeek()
        {
            //参考
            //http://www.jb51.net/article/100509.htm

            //获得数据
            var query = _userService.GetWithDraws();


            int a = query.Count();

            string sWebRootFolder = _hostingEnvironment.WebRootPath;
            string sFileName = $"{Guid.NewGuid()}.xlsx";
            FileInfo file = new FileInfo(Path.Combine(sWebRootFolder, sFileName));
            using (ExcelPackage package = new ExcelPackage(file))
            {
                // 添加worksheet
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("主播工资表");
                //添加头
                worksheet.Cells[1, 1].Value = "开户行";
                worksheet.Cells[1, 2].Value = "开户行地址";
                worksheet.Cells[1, 3].Value = "银行账户";
                worksheet.Cells[1, 4].Value = "银行户名";
                worksheet.Cells[1, 5].Value = "金额(元)";


                int i = 2;
                //添加值
                foreach (var item in query)
                {
                    worksheet.Cells["A" + i].Value = item.BankName;
                    worksheet.Cells["B" + i].Value = item.BankAddress;
                    worksheet.Cells["C" + i].Value = item.BankAccount;
                    worksheet.Cells["D" + i].Value = item.BankUserName;
                    worksheet.Cells["E" + i].Value = item.Money;

                    i++;
                }

                ////添加值
                //worksheet.Cells["A2"].Value = 1000;
                //worksheet.Cells["B2"].Value = "LineZero";
                //worksheet.Cells["C2"].Value = "http://www.cnblogs.com/linezero/";

                //worksheet.Cells["A3"].Value = 1001;
                //worksheet.Cells["B3"].Value = "LineZero GitHub";
                //worksheet.Cells["C3"].Value = "https://github.com/linezero";
                //worksheet.Cells["C3"].Style.Font.Bold = true;

                package.Save();
            }
            return File(sFileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }


        //导出到excell
        public IActionResult ExportMoth()
        {
            //参考
            //http://www.jb51.net/article/100509.htm

            //获得数据
            var query = _userService.GetWithDrawsMonth();


            int a = query.Count();

            string sWebRootFolder = _hostingEnvironment.WebRootPath;
            string sFileName = $"{Guid.NewGuid()}.xlsx";
            sFileName = "/finace/" + sFileName;
            FileInfo file = new FileInfo(Path.Combine(sWebRootFolder, sFileName));
            using (ExcelPackage package = new ExcelPackage(file))
            {
                // 添加worksheet
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("主播工资表");
                //添加头
                worksheet.Cells[1, 1].Value = "开户行";
                worksheet.Cells[1, 2].Value = "开户行地址";
                worksheet.Cells[1, 3].Value = "银行账户";
                worksheet.Cells[1, 4].Value = "银行户名";
                worksheet.Cells[1, 5].Value = "金额(元)";


                int i = 2;
                //添加值
                foreach (var item in query)
                {
                    worksheet.Cells["A" + i].Value = item.BankName;
                    worksheet.Cells["B" + i].Value = item.BankAddress;
                    worksheet.Cells["C" + i].Value = item.BankAccount;
                    worksheet.Cells["D" + i].Value = item.BankUserName;
                    worksheet.Cells["E" + i].Value = item.Money;

                    i++;
                }

                ////添加值
                //worksheet.Cells["A2"].Value = 1000;
                //worksheet.Cells["B2"].Value = "LineZero";
                //worksheet.Cells["C2"].Value = "http://www.cnblogs.com/linezero/";

                //worksheet.Cells["A3"].Value = 1001;
                //worksheet.Cells["B3"].Value = "LineZero GitHub";
                //worksheet.Cells["C3"].Value = "https://github.com/linezero";
                //worksheet.Cells["C3"].Style.Font.Bold = true;

                package.Save();
            }
            return File(sFileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        //      //从excell 导入
        //[HttpPost]
        //     public IActionResult Import(IFormFile excelfile)
        //     {
        ////http://www.jb51.net/article/100509.htm

        //    string sWebRootFolder = _hostingEnvironment.WebRootPath;
        //    string sFileName = $"{Guid.NewGuid()}.xlsx";
        //    FileInfo file = new FileInfo(Path.Combine(sWebRootFolder, sFileName));
        //    try
        //    {
        //        using (FileStream fs = new FileStream(file.ToString(), FileMode.Create))
        //        {
        //            excelfile.CopyTo(fs);
        //            fs.Flush();
        //        }
        //        using (ExcelPackage package = new ExcelPackage(file))
        //        {
        //            StringBuilder sb = new StringBuilder();
        //            ExcelWorksheet worksheet = package.Workbook.Worksheets[1];
        //            int rowCount = worksheet.Dimension.Rows;
        //            int ColCount = worksheet.Dimension.Columns;
        //            bool bHeaderRow = true;
        //            for (int row = 1; row <= rowCount; row++)
        //            {
        //                for (int col = 1; col <= ColCount; col++)
        //                {
        //                    if (bHeaderRow)
        //                    {
        //                        sb.Append(worksheet.Cells[row, col].Value.ToString() + "\t");
        //                    }
        //                    else
        //                    {
        //                        sb.Append(worksheet.Cells[row, col].Value.ToString() + "\t");
        //                    }
        //                }
        //                sb.Append(Environment.NewLine);
        //            }
        //            return Content(sb.ToString());
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Content(ex.Message);
        //    }
        //}



        //导出到excell
        public IActionResult ExportThisWeek()
        {
            //参考
            //http://www.jb51.net/article/100509.htm

            //获得数据
            var query = _userService.GetWithDrawsThisWeek();


            int a = query.Count();

            string sWebRootFolder = _hostingEnvironment.WebRootPath;
            string sFileName = $"{Guid.NewGuid()}.xlsx";
            FileInfo file = new FileInfo(Path.Combine(sWebRootFolder, sFileName));
            using (ExcelPackage package = new ExcelPackage(file))
            {
                // 添加worksheet
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("主播工资表");
                //添加头
                worksheet.Cells[1, 1].Value = "开户行";
                worksheet.Cells[1, 2].Value = "开户行地址";
                worksheet.Cells[1, 3].Value = "银行账户";
                worksheet.Cells[1, 4].Value = "银行户名";
                worksheet.Cells[1, 5].Value = "金额(元)";


                int i = 2;
                //添加值
                foreach (var item in query)
                {
                    worksheet.Cells["A" + i].Value = item.BankName;
                    worksheet.Cells["B" + i].Value = item.BankAddress;
                    worksheet.Cells["C" + i].Value = item.BankAccount;
                    worksheet.Cells["D" + i].Value = item.BankUserName;
                    worksheet.Cells["E" + i].Value = item.Money;

                    i++;
                }

                ////添加值
                //worksheet.Cells["A2"].Value = 1000;
                //worksheet.Cells["B2"].Value = "LineZero";
                //worksheet.Cells["C2"].Value = "http://www.cnblogs.com/linezero/";

                //worksheet.Cells["A3"].Value = 1001;
                //worksheet.Cells["B3"].Value = "LineZero GitHub";
                //worksheet.Cells["C3"].Value = "https://github.com/linezero";
                //worksheet.Cells["C3"].Style.Font.Bold = true;

                package.Save();
            }
            return File(sFileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }


        //导出到excell
        public IActionResult ExportThisMoth()
        {
            //参考
            //http://www.jb51.net/article/100509.htm

            //获得数据
            var query = _userService.GetWithDrawsThisMonth();


            int a = query.Count();

            string sWebRootFolder = _hostingEnvironment.WebRootPath;
            string sFileName = $"{Guid.NewGuid()}.xlsx";
            sFileName = "/finace/" + sFileName;
            FileInfo file = new FileInfo(Path.Combine(sWebRootFolder, sFileName));
            using (ExcelPackage package = new ExcelPackage(file))
            {
                // 添加worksheet
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("主播工资表");
                //添加头
                worksheet.Cells[1, 1].Value = "开户行";
                worksheet.Cells[1, 2].Value = "开户行地址";
                worksheet.Cells[1, 3].Value = "银行账户";
                worksheet.Cells[1, 4].Value = "银行户名";
                worksheet.Cells[1, 5].Value = "金额(元)";


                int i = 2;
                //添加值
                foreach (var item in query)
                {
                    worksheet.Cells["A" + i].Value = item.BankName;
                    worksheet.Cells["B" + i].Value = item.BankAddress;
                    worksheet.Cells["C" + i].Value = item.BankAccount;
                    worksheet.Cells["D" + i].Value = item.BankUserName;
                    worksheet.Cells["E" + i].Value = item.Money;

                    i++;
                }

                ////添加值
                //worksheet.Cells["A2"].Value = 1000;
                //worksheet.Cells["B2"].Value = "LineZero";
                //worksheet.Cells["C2"].Value = "http://www.cnblogs.com/linezero/";

                //worksheet.Cells["A3"].Value = 1001;
                //worksheet.Cells["B3"].Value = "LineZero GitHub";
                //worksheet.Cells["C3"].Value = "https://github.com/linezero";
                //worksheet.Cells["C3"].Style.Font.Bold = true;

                package.Save();
            }
            return File(sFileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }


        //查看会话双方的通话质量
        [HttpGet]
        public IActionResult Network(int? id) {

            if (id == null)
            {
                id = 0;
                //return NotFound();
            }

            /* 老代码是 1分钟同意一次，生成序列
                        var record = _db.ReportNetworks.Where(t => t.Record == id);
                        if (record != null)
                        {
                            var seconds= record.Select(a =>a.SN).ToArray();
                            var sends = record.Select(a => a.SendV).ToArray();
                            var receive = record.Select(a => a.ReceiveV).ToArray();
                            var obj = new
                            {
                                labels = seconds,
                                datasets = new[] {
                            new {
                                type = "line",
                                data = sends,
                                backgroundColor = "transparent",
                                borderColor = "#007bff",
                                pointBorderColor = "#007bff",
                                pointBackgroundColor = "#007bff",
                                fill = false
                                // pointHoverBackgroundColor: '#007bff',
                                // pointHoverBorderColor    : '#007bff'
                            },
                            new {
                                type = "line",
                                data = receive,//new int[] { 60, 80, 70, 67, 80, 77, 100 },
                                backgroundColor = "tansparent",
                                borderColor = "#ced4da",
                                pointBorderColor = "#ced4da",
                                pointBackgroundColor = "#ced4da",
                                fill = false
                                // pointHoverBackgroundColor: '#ced4da',
                                // pointHoverBorderColor    : '#ced4da'
                            }
                        }
                            };


                            ViewData["network"] = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
                        }
                        */

            //新代码是一秒钟生成一次
            //传匿名对象给VIEW
            dynamic dataObject = new ExpandoObject();

            var record = _db.ReportNetworks.FirstOrDefault(t => t.Record == id && t.SN==0);
            if (record != null)
            {
                var tmp = record.ProduceV.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(a => Convert.ToInt32(a)).ToArray();
                    //tmp = record.ProduceV.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(a => Convert.ToInt32(a)).ToArray();
                dataObject.Produce = Newtonsoft.Json.JsonConvert.SerializeObject(tmp);

                int count = tmp.Length;
                Int32[] seq = new Int32[count];
                for (Int32 i = 0; i < count; ++i)
                {
                    seq[i] = i;
                }
                dataObject.labels = Newtonsoft.Json.JsonConvert.SerializeObject(seq);


                tmp = record.ReceiveV.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(a => Convert.ToInt32(a)).ToArray();
                dataObject.Receive = Newtonsoft.Json.JsonConvert.SerializeObject(tmp);

            }
            else
            {
                dataObject.labels   = Newtonsoft.Json.JsonConvert.SerializeObject(new Int32[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });
                dataObject.Produce  = Newtonsoft.Json.JsonConvert.SerializeObject(new Int32[] { 100, 100, 100, 100, 100, 100, 100, 100, 100 });
                dataObject.Receive  = Newtonsoft.Json.JsonConvert.SerializeObject(new Int32[] { 50, 50, 50, 50, 50, 50, 50, 50, 50 });
            }

            ViewBag.Data = dataObject;

            return View();
        }

        public IActionResult TalksRecord() {

            List<TalksRecord> list = _db.TalksRecords.OrderByDescending(t=>t.Id).Take(100).ToList();
            return View(list);
        }

        public IActionResult Users() {

            //传匿名对象结合 注意顶部的class ToExpando
            //https://www.dotnetfunda.com/articles/show/2655/binding-views-with-anonymous-type-collection-in-aspnet-mvc

            dynamic usr = _db.Users.Where(c => c.Id != 0 && c.Closed == 0)
                              .Include(x => x.pTalks).Where(x => x.pTalks != null && x.pTalks.DisplayOrder > 0 && x.pTalks.TalkingId == 0)
                              .OrderByDescending(c => c.Online)
                              .ThenByDescending(c => c.pTalks.DisplayOrder)
                              .Include(x => x.Labels).ThenInclude(y => y.Tag)
                              .ToList()
                              .Select(c => new
                              {
                                  Id = c.Id,
                                  Name = c.Name,
                                  Sex = c.Sex,
                                  Birthday = c.Birthday.ToString("yyyy-MM-dd"),
                                  Price = (c.pTalks == null ? 200 : c.Price),
                                  Coins=c.Coins,
                                  Beans=c.Beans,
                                  Scores=c.Scores,
                                  Career = c.Career,
                                  Phone=c.Phone,
                                  RegIp=c.RegIp,
                                  Labels = c.Labels.OrderBy(e => e.Id).Select(e => e.Tag).Select(e => e.Name).ToArray(),
                                  Desp = c.Desp,
                                  Location = c.Location,
                                  Online = c.Online
                              }).AsEnumerable().Select(c => c.ToExpando());

            return View(usr);
        }
    }
}