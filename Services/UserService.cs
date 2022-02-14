using System;
using System.IO;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Microsoft.EntityFrameworkCore;//include
using XAccount.Entities;
using XAccount.Helpers;
using XAccount.Data;
using XAccount.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

//webclient
using System.Net.Http;
using System.Net;
using System.Web;
using System.Text.RegularExpressions;//字符串分割
using Microsoft.Extensions.Configuration;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;     //JObject

using System.Threading.Tasks;
using System.Threading;

namespace XAccount.Services
{


    public interface IUserService
    {
        LoginRetModel LoginWithPhone(long phone, string passwordz);
        bool SendVerifyCode(long phone);

        //设置用户上线下限状态
        void UserOnline(long uid, int online);

        User UpdateProfile(long uid,JObject jusr);
        IEnumerable<User> GetAll();


        public dynamic GetWorld();
        dynamic GetXRWorldInfo(long roomID, string password);
        dynamic GetMyProfile();
        dynamic GetAllProfile();
        dynamic GetNewUser();


        long GetUserIdFromIdOrPhone(long xid);
        //账户相关 
        long BalanceChange(long uid, int currency, int amount, string summary,long rid,int type=0,string remark="");
        Task<int> BalanceChangeAsync(long uid, int currency, int amount, string summary, long rid, int type = 0, string remark = "",long from=1);

        //转账
        Task<int> TransferCurrency(TransferModel model);
        void    TransferAutoConfirm();
        void    TransferConfirm(TransferConfirmModel model);
        dynamic GetOnTheWayCoins(long uid,bool agent=false);
        dynamic Balance(int type);
        dynamic Social();


        void WithDraw(WithDrawModel model);


        //IEnumerable<TalksActorModel> GetTalksActor();
        dynamic GetODataUsers();
        dynamic GetODataUsers(int tagId);

        //财务结算
        IEnumerable<WithDraw> GetWithDraws();
        IEnumerable<WithDraw> GetWithDrawsMonth();
        //测试 本周数据
        IEnumerable<WithDraw> GetWithDrawsThisWeek();
        IEnumerable<WithDraw> GetWithDrawsThisMonth();
    }

    public class UserService : IUserService
    {
        private DataContext _context;
        private readonly HttpContext _httpcontext;
        private readonly AppSettings _appSettings;
        private IConfiguration _config;
        private IWebHostEnvironment _hostingEnvironment;

        //校验码
        //手机短信字典
        class CheckCode
        {
            public int RandomCode { get; set; }
            public DateTime CreateTime { get; set; } = DateTime.Now;        //30秒内不重发短信
            public int Count { get; set; } = 1;
        }
        private static Dictionary<long, CheckCode> _phone_check = null;
        private static readonly object SequenceLock = new object();

        //private static Mutex mutex_;

        public UserService(DataContext context, IHttpContextAccessor httpContextAccessor, IOptions<AppSettings> appSettings, IWebHostEnvironment hostingEnvironment, IConfiguration config)
        {
            _context = context;
            //_context.Database.EnsureCreated();
            _httpcontext = httpContextAccessor.HttpContext;
            _appSettings = appSettings.Value;
            _hostingEnvironment = hostingEnvironment;
            _config = config;


            lock (SequenceLock)
            {
                init_user_service();
            }
        }

        private void init_user_service()
        {
            //静态变量初始化
            //手机验证码
            if (_phone_check == null)
            {
                _phone_check = new Dictionary<long, CheckCode>();

                var check = new CheckCode();
                check.RandomCode = 119;
                _phone_check.Add(119, check);


                //增加内置机器人
                if (_context.Users.Count() < 50)
                {
                    make_robot();

                }else {//

                    var lst = _context.Users.AsNoTracking().Where(t => t.Id >= 1412100 && t.Id < 1512100).Select(c => new {
                                            id=c.Id,
                                            token=c.Token,
                                            profile = new {
                                                            birthday = c.Birthday.ToString("yyyy-MM-dd"),
                                                            avatar = c.Avatar,
                                                            sex = c.Sex,
                                                            name = c.Name,
                                                            id = c.Id
                                                            }
                                                }
                                            ).ToList();

                    //json 写入文件
                    var json2 = JsonConvert.SerializeObject(lst);
                    {
                        ////读取随机网名
                        string webRootPath = _hostingEnvironment.WebRootPath;//根目录 wwwroot
                        string txtPath = Path.Combine(webRootPath, "txt/");
                        string robots = Path.Combine(txtPath, "robots.json");
                        StreamWriter sw = new StreamWriter(robots);
                        //开始写入
                        sw.Write(json2);
                        //清空缓冲区
                        sw.Flush();
                        //关闭流
                        sw.Close();
                    }


                }//
            }

        }

        private void make_robot()
        {
            long UsereId = 10001;
            var  lst = new List<object>();
            long baseId = 1412100;
            for (int i = 0; i < 1200; i++)
            {
                UsereId = baseId + i;

                //保证机器人的范围 1412100--1512100
                if (UsereId >= 1512100)
                    break;

                string u_token = CreateBeareTokenString(UsereId, 365);

                Random ran = new Random();
                //随机用户名
                int mobilelast = ran.Next(10000, 90000);
                string u_name ="用户"+(mobilelast%10000).ToString("d4");

                //随机头像
                int RandKey     = ran.Next(1, 10);
                string u_avatar = RandKey.ToString() + ".jpg";

                //随机性别
                int u_sex       = ran.Next(0, 1);

                //随机年龄
                DateTime dt     = DateTime.Now;
                int yearP       = ran.Next(18, 39);
                int monthP      = ran.Next(1, 12);
                int dayP        = ran.Next(1, 30);
                dt = dt.AddYears(-yearP);
                dt = dt.AddMonths(-monthP);
                dt = dt.AddDays(-dayP);

                //文件
                var p = new { id = UsereId, name = u_name, sex = u_sex, avatar = u_avatar, bitrhday = dt.ToString(("yyyy-MM-dd")) };
                var u = new { id = UsereId, token = u_token, profile = p };
                lst.Add(u);

                //数据库
                var usr = new User
                {
                    Id = UsereId,
                    Birthday = dt,
                    Token = u_token,
                    Name = u_name,
                    Phone = UsereId,
                    Avatar = u_avatar,
                    Sex = u_sex,
                    Coins = 100000,
                    pGame = new Mahjong { }
                };
                _context.Users.Add(usr);
            }
            _context.SaveChanges();

            //json 写入文件
            var json2 = JsonConvert.SerializeObject(lst);
            {
                ////读取随机网名
                string webRootPath = _hostingEnvironment.WebRootPath;//根目录 wwwroot
                string txtPath = Path.Combine(webRootPath, "txt/");
                string robots = Path.Combine(txtPath, "robots.json");
                StreamWriter sw = new StreamWriter(robots);
                //开始写入
                sw.Write(json2);
                //清空缓冲区
                sw.Flush();
                //关闭流
                sw.Close();
            }
        }
        //放弃
        private void make_robot_from_file()
        {
            StreamReader fileRead;
            string line;
            {
                //读取随机网名
                string webRootPath = _hostingEnvironment.WebRootPath;//根目录 wwwroot
                string txtPath = Path.Combine(webRootPath, "txt/");
                //string nameMale = Path.Combine(txtPath, "name_male.txt");
                string nameFeMale = Path.Combine(txtPath, "name_female.txt");
                //女性机器人
                fileRead = new StreamReader(nameFeMale);
            }

            long UsereId = 10001;
            var lst = new List<object>();
            long baseId = 1412100;
            for (int i = 0; i < 100; i++)
            {
                UsereId = baseId + i;

                //保证机器人的范围 1412100--1512100
                if (UsereId >= 1512100)
                    break;

                string u_token = CreateBeareTokenString(UsereId, 365);

                //获得默认用户名
                line = fileRead.ReadLine();
                if (string.IsNullOrEmpty(line))
                    line = "匿名用户";
                string u_name = line;

                //随机头像
                Random ran = new Random();
                int RandKey = ran.Next(1, 10);
                string u_avatar = RandKey.ToString() + ".jpg";

                //随机性别
                int u_sex = ran.Next(0, 1);

                //随机年龄
                DateTime dt = DateTime.Now;
                int yearP = ran.Next(18, 60);
                int monthP = ran.Next(1, 12);
                int dayP = ran.Next(1, 30);
                dt = dt.AddYears(-yearP);
                dt = dt.AddMonths(-monthP);
                dt = dt.AddDays(-dayP);

                //文件
                var p = new { id = UsereId, name = u_name, sex = u_sex, avatar = u_avatar, bitrhday = dt.ToString(("yyyy-MM-dd")) };
                var u = new { id = UsereId, token = u_token, profile = p };
                lst.Add(u);

                //数据库
                var usr = new User
                {
                    Id = UsereId,
                    Birthday = dt,
                    Token = u_token,
                    Name = u_name,
                    Phone = UsereId,
                    Avatar = u_avatar,
                    Sex = u_sex,
                    Coins = 100000,
                    pGame = new Mahjong { }
                };
                _context.Users.Add(usr);
            }
            _context.SaveChanges();

            //json 写入文件
            var json2 = JsonConvert.SerializeObject(lst);
            {
                ////读取随机网名
                string webRootPath = _hostingEnvironment.WebRootPath;//根目录 wwwroot
                string txtPath = Path.Combine(webRootPath, "txt/");
                string robots = Path.Combine(txtPath, "robots.json");
                StreamWriter sw = new StreamWriter(robots);
                //开始写入
                sw.Write(json2);
                //清空缓冲区
                sw.Flush();
                //关闭流
                sw.Close();
            }
        }
        //判断是否为信令服务器
        private bool IsTrackerServer()
        {
            //return true;
            //获得 ip 地址信息
            string ss = _httpcontext.Connection.RemoteIpAddress.ToString();

            string ips = _config.GetValue<string>("appSettings:tracker_server");
            if (ips.IndexOf(ss, StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            return true;
        }

        public void UserOnline(long uid, int online)
        {
            ////根据ip地址判断
            //if (!IsTrackerServer())
            //    return;


            var user = _context.Users.FirstOrDefault(t => t.Id == uid);
            if (user == null)
            {
                return;
                throw new AppException("账号不存在");
            }
               

            if (user.Online >= 0)
            {
                user.Online = online;
                _context.Users.Update(user);
                _context.SaveChanges();

                if (online>0)
                    On_UserOnline(uid);
                else if (0 == online)
                    On_UserOffOnline(uid);
            }
            else {
                //账户被禁用

            }
        }
        //用户上线
        private void On_UserOnline(long uid)
        {//清理数据

        }
        //用户下线
        private void On_UserOffOnline(long uid)
        {//清理数据

        }


        public long GetUserIdFromIdOrPhone(long xid)
        {
            if (xid > 10000000000 && xid < 20000000000)
            {
                var usr = _context.Users.SingleOrDefault(t => t.Phone == xid);
                if (usr == null)
                    throw new AppException("用户不存在---手机号不存在");

                return usr.Id;
            }
            return xid;
        }

        public async Task<int> TransferCurrency(TransferModel model)
        {
            long uid = Convert.ToInt64(_httpcontext.User.Identity.Name);

            //默认赚给自己
            if (model.To == 0 && model.Type == 2)
                model.To = uid;

            model.To = GetUserIdFromIdOrPhone(model.To);


            var usr = await _context.Users.FirstOrDefaultAsync(t => t.Id == model.To);
            if (usr == null)
                throw new AppException("对方账户不存在 目标账户:" + model.To.ToString());

            if (model.Amount <= 0)
                throw new AppException("转账金额不正确!");

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    float loss = 0;
                    string summary = "转账";
                    if (model.Type == 221) {
                        //自己金豆转金币 可以转给任意人 满100才可以
                        loss = _config.GetValue<float>("Finace:TransferBean2Coin");

                        int newCoins= (int)(model.Amount * loss);

                        await BalanceChangeAsync(uid, 2, -model.Amount,  "金豆转金币-转出", model.To, 0, "无手续费");    //转出账户
                        await BalanceChangeAsync(model.To, 1, newCoins, "金豆转金币-转入", uid, 0, "");

                        return model.Amount;
                    }else if (model.Type == 1)
                    {
                        loss = _config.GetValue<float>("Finace:TransferLossCoin");
                        summary = "金币转账";
                    }
                    else if (model.Type == 2)
                    {
                        loss = _config.GetValue<float>("Finace:TransferLossBean");
                        summary = "金豆转账";

                        //满100元才可转移
                        int  minBean=_config.GetValue<int>("Finace:WithdrawMin");

                        int tmp = model.Amount;
                            tmp = tmp / minBean;
                            tmp *= minBean;

                        if (tmp != model.Amount)
                        {
                            string tips = "转账金额不正确!" + "必须是"+ minBean.ToString()+"的整数倍";
                            throw new AppException(tips);
                        }

                    }else if (model.Type == 3)
                    {
                        loss = _config.GetValue<int>("Finace:TransferLossScore");
                        summary = "积分转账";
                    }


                    int fee = (int)(model.Amount * loss);
                    //转出账户
                    int result = await BalanceChangeAsync(uid, model.Type, -model.Amount, summary + "-转出", model.To, 0, model.Remarks);
                    if(result<=0)
                        throw new AppException("转出账户资金不足");
                    //转出账户
                    result = await BalanceChangeAsync(uid, model.Type, -fee, summary + "-转账手续费", 1, 0, "系统手续费-转账");
                    if (result <= 0)
                        throw new AppException("转出账户资金不足");


                    //转入账户
                    result =await BalanceChangeAsync(model.To, model.Type, model.Amount, summary + "-转入", uid, 0, model.Remarks);
                    result =await BalanceChangeAsync(1, model.Type, fee, summary + "-转入", uid, 0, "系统手续费");

                    transaction.Commit();
                }
                catch (Exception e)
                {
                    //Console.WriteLine(e.Message);
                    transaction.Rollback();

                    throw new AppException(e.Message + "转账失败 您的账户未发生变化");
                }
            }
            return model.Amount;
        }

        //转账确认  代理确认 代理撤销   用户申诉
        public void TransferConfirm(TransferConfirmModel model)
        {
            long uid = Convert.ToInt64(_httpcontext.User.Identity.Name);

            var bill = _context.OnTheWayCoins.FirstOrDefault(t => t.Id == model.RecordId);
            if (bill == null)
                throw new AppException("转账账单不存在!");


            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {

                    if (Math.Abs(model.Action) == 2 && uid == bill.AgentId)
                    {//代理

                        if (model.Action == 2)
                        {//确认
                            BalanceChange(bill.UserId, 1, bill.Amount, "", bill.Id, 0, "在途金币到账");
                            bill.Status = 0;
                        }
                        else
                        {//必须在24小时后才能撤回
                            TimeSpan ts = DateTime.Now - bill.CreateTime;
                            if (ts.Hours < 24)
                                throw new AppException("24小时后方可撤回!");

                            if (bill.Status != 1)
                                throw new AppException("改笔转账不可撤回,请联系客服!");

                            bill.Status = -2;
                        }

                        bill.AgentTime = DateTime.Now;
                        _context.OnTheWayCoins.Update(bill);
                        _context.SaveChanges();
                    }
                    else if (Math.Abs(model.Action) == 3 && uid == bill.UserId)
                    {//用户

                        bill.Status = 3;
                        bill.UserTime = DateTime.Now;

                        _context.OnTheWayCoins.Update(bill);
                        _context.SaveChanges();
                    }

                    transaction.Commit();
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    throw new AppException(e.Message + " 请求失败");
                }
            }

        }


        //自动到账或自动撤销 
        public void TransferAutoConfirm()
        {
            var usrOntheWay = _context.OnTheWayCoins.Where(t => t.Status == 1).Select(t => t.Id).ToArray();

            using (var transaction = _context.Database.BeginTransaction())
            {
                try {
                    foreach (long rid in usrOntheWay)
                    {
                        var bill = _context.OnTheWayCoins.FirstOrDefault(t => t.Id == rid);
                        if (bill != null)
                        {
                            TimeSpan ts = DateTime.Now - bill.CreateTime;
                            if (ts.Hours > 1)
                            {   //自动到账
                                BalanceChange(bill.UserId, 1, bill.Amount, "", bill.Id, 0, "在途金币到账");

                            } else if (ts.Hours > 24)
                            {
                                BalanceChange(bill.AgentId, 1, bill.Amount, "", bill.Id, 0, "在途金币撤回 到账");
                            }

                            bill.SystemTime = DateTime.Now;
                            bill.Status = 0;
                        }
                        _context.OnTheWayCoins.Update(bill);
                    }

                    _context.SaveChanges();
                    transaction.Commit();
                } catch (Exception e) {
                    transaction.Rollback();
                }
            }
        }


        public dynamic Balance(int type)
        {
            long uid = Convert.ToInt64(_httpcontext.User.Identity.Name);
            var bill = _context.UserBalanceChanges.Where(t => t.UserId == uid && t.Currency == type)
                .OrderByDescending(a => a.Id)
                .Take(30)
                .Select(c => new {
                    Time = c.CreateTime.ToString("MM-dd HH:mm"),
                    Amount = c.Amount,
                    Balance = c.Balance,
                    Remarks = c.Summary
                }
                ).ToList();

            return bill.AsEnumerable();

        }

        private int getAction(int status, DateTime dt)
        {
            return (status == 1 && ((DateTime.Now - dt).Hours < 1)) ? 1 : 0;
        }
        private string getStatus(int amount, int status) {
            string ret = amount.ToString() + "枚";
            if (status == 0)
                ret += "(完成)";
            else if (status == 1)
                ret += "(在途)";
            else if (status == -2)
                ret += "(撤销中)";
            else if (status == 3)
                ret += "(申述中)";
            else if (status == 4)
                ret += "(已冻结)";

            return ret;
        }
        public dynamic GetOnTheWayCoins(long uid, bool agent)
        {
            DateTime end = DateTime.Now.AddHours(-48);

            if (agent)
            {
                var coins = _context.OnTheWayCoins.Where(t => t.AgentId == uid && t.CreateTime > end)
                                .OrderByDescending(a => a.Id)
                                .Select(c => new
                                {
                                    Id = c.Id,
                                    Time = c.CreateTime.ToString("MM-dd HH:mm"),
                                    Peer = c.UserId,
                                    Status = getStatus(c.Amount, c.Status),
                                    Action = getAction(c.Status, c.CreateTime)
                                }).ToList();

                return coins.AsEnumerable();
            }
            else
            {
                var coins = _context.OnTheWayCoins.Where(t => t.UserId == uid && t.CreateTime > end)
                                  .OrderByDescending(a => a.Id)
                                  .Select(c => new {
                                      Id = c.Id,
                                      Time = c.CreateTime.ToString("MM-dd HH:mm"),
                                      Peer = c.AgentId,
                                      Status = getStatus(c.Amount, c.Status),
                                      Action = (int)((c.Status == -2 && true) ? 1 : 0)
                                  }).ToList();

                return coins.AsEnumerable();
            }


        }


        public void WithDraw(WithDrawModel model)
        {
            long uid = Convert.ToInt64(_httpcontext.User.Identity.Name);

            int beans = _config.GetValue<int>("Finace:WithdrawMin");
            if (model.Beans < beans)
            {
                int yuan = beans / 100;
                string msg = "提现金额必须  >=";
                msg += yuan.ToString() + " 元";
                throw new AppException(msg);
            }
            double money = (double)model.Beans / 100;

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    BalanceChange(uid, (int)CoinsType.Beans, -model.Beans, "", 0, 0, "提现");    //转出账户

                    var bill = new WithDraw
                    {
                        UserId = uid,
                        Beans = model.Beans,
                        BankAccount = model.BankAccount,
                        BankUserName = model.BankUserName,
                        BankName = model.BankName,
                        BankAddress = model.BankAddress,
                        Money = money
                    };
                    _context.WithDraws.Add(bill);
                    _context.SaveChanges();

                    transaction.Commit();
                } catch (Exception e)
                {
                    transaction.Rollback();
                    throw new AppException("提现-金币 失败!");
                }
            }
        }

        //记录用户账户变化
        /*
         * currency  1金币    2金豆   3积分
         */
        public long BalanceChange(long uid, int currency, int amount, string summary, long rid, int type = 0, string remark = "")
        {
            //机器人使用系统内置账户
            if (uid < 1512100 && uid>=1412100)
                    uid = 1;


            var usr = _context.Users.SingleOrDefault(x => x.Id == uid);
            if (usr == null)
                throw new AppException("用户不存在!");

            UserBalanceChange change = new UserBalanceChange {
                UserId = uid,
                Currency = currency,
                Summary = summary,
                Amount = amount,
                RecordId = rid,
                Remark = remark,
                Type = type
            };
            if (1 == currency)
            {
                usr.Coins += amount;
                if (usr.Coins < 0)
                    throw new AppException("金币账户余额不足!");

                change.Balance = usr.Coins;
            }
            else if (2 == currency)
            {
                usr.Beans += amount;
                if (usr.Beans < 0)
                    throw new AppException("金豆账户余额不足!");
                change.Balance = usr.Beans;

            } else if (3 == currency)
            {
                usr.Scores += amount;
                if (usr.Scores < 0)
                    throw new AppException("积分账户余额不足!");
                change.Balance = usr.Scores;
            }

            _context.Users.Update(usr);
            _context.UserBalanceChanges.Add(change);

            _context.SaveChanges();
            return change.Id;
        }

        //增加减少用户的 金币  金豆 游戏积分  from 表示资金来源
        public async Task<int> BalanceChangeAsync(long uid, int currency, int amount, string summary, long rid, int type = 0, string remark = "",long from=1)
        {
            //机器人使用系统内置账户
            if (uid < 1512100 && uid >= 1412100)
                uid = 1;

            //首先检查来源账户是否资金足够
            var usr = await _context.Users.SingleOrDefaultAsync(x => x.Id == uid);
            if (usr == null)
                throw new AppException("用户不存在!");

            if (amount == 0)
                return 0;


            int result = 0;
            UserBalanceChange change = new UserBalanceChange
            {
                UserId = uid,
                Currency = currency,
                Summary = summary,
                Amount = amount,
                RecordId = rid,
                Remark = remark,
                Type = type
            };
            if (1 == currency)
            {
                //检查转出账户是否资金足够
                if (amount < 0 && usr.Coins < Math.Abs(amount))
                    return 0;


                usr.Coins += amount;
                if (usr.Coins < 0)
                    result = usr.Coins;             //throw new AppException("积分账户余额不足!");
                change.Balance = usr.Coins;
            }
            else if (2 == currency)
            {
                //检查转出账户是否资金足够
                if (amount < 0 && usr.Beans < Math.Abs(amount))
                    return 0;

                usr.Beans += amount;
                if (usr.Beans < 0)
                    result = usr.Beans;
                    
                change.Balance = usr.Beans;

            }
            else if (3 == currency)
            {
                if (amount < 0 && usr.Scores < Math.Abs(amount))
                    return 0;

                usr.Scores += amount;
                if (usr.Scores < 0)
                    result = usr.Scores;
                change.Balance = usr.Scores;
            }

            _context.Users.Update(usr);
            _context.UserBalanceChanges.Add(change);

            await _context.SaveChangesAsync();

            return result;
        }


        public dynamic GetMyProfile()
        {
            long uid = Convert.ToInt64(_httpcontext.User.Identity.Name);
            var usr = _context.Users.AsNoTracking()
                                    .Include(x => x.pGame)
                                    .Select(c => new
                                    {
                                        Id = c.Id,
                                        Name = c.Name,
                                        Coins = c.Coins,
                                        Beans = c.Beans,
                                        Scores = c.Scores,
                                    }).FirstOrDefault(t => t.Id == uid);

            return usr;
        }

        private dynamic GetMyInfo(long uid)
        {
            //long uid = Convert.ToInt64(_httpcontext.User.Identity.Name);
            var usr = _context.Users.AsNoTracking()
                                    .Include(x => x.pTalks)
                                    .Select(c => new
                                    {
                                        Id = c.Id,
                                        Mood = c.Mood,
                                        Name = c.Name,
                                        Sex = c.Sex,
                                        Birthday = c.Birthday.ToString("yyyy-MM-dd"),
                                        Price = (c.pTalks == null ? 0 : c.Price),
                                        Labels = c.Labels.Select(b => b.TagID).ToArray()
                                    }).FirstOrDefault(t => t.Id == uid);

            return usr;
        }

        public dynamic GetWorld()
        {
            var worlds = _context.Worlds.Where(t => t.Id>0)
                .OrderByDescending(a => a.DisplayOrder)
                .Take(30)
                .Select(c => new {
                    Id=c.Id,
                    Name=c.Name,
                    GameServer=c.GameServer,
                    AvServer=c.AvServer,
                    CreateTime = c.CreateTime.ToString("yyyy-MM-dd"),
                    Desp = c.Desp,
                    Path = c.Path
                }
               ).ToList();

            return worlds.AsEnumerable();
        }


        public dynamic GetXRWorldInfo(long roomID,string password)
        {
            string errorMSG = "世界不存在!";

            long uid = Convert.ToInt64(_httpcontext.User.Identity.Name);
            if (uid > 0)
            {
                var profile = _context.Users.AsNoTracking()
                        .Include(x => x.pTalks)
                        .Select(c => new
                        {
                            rid = roomID,

                            uid = c.Id,
                            name = c.Name,
                            sex = c.Sex,
                            birthday = c.Birthday.ToString("yyyy-MM-dd"),
                            avatar = c.Avatar
                        }).FirstOrDefault(t => t.uid == uid);

                return profile;
            }else
            {
                errorMSG = "用户不存在!";
             }

            
            return new { rid = 0 , errMsg = errorMSG };
        }



        public bool SendVerifyCode(long phone)
        {
            //随机数
            Random ran = new Random();
            int RandKey = ran.Next(100000, 999999);

            if (!_phone_check.ContainsKey(phone))
            {
                var code = new CheckCode();
                code.RandomCode = RandKey;
                code.CreateTime = DateTime.Now;
                code.Count = 1;

                _phone_check.Add(phone, code);
            }
            else
            {
                TimeSpan ts = DateTime.Now - _phone_check[phone].CreateTime;
                if (ts.Seconds >= 30)
                {
                    _phone_check[phone].CreateTime = DateTime.Now;
                    _phone_check[phone].Count++;    //增加计数器
                    _phone_check[phone].RandomCode = RandKey;
                }
                else
                {
                    RandKey = 0;//不发送验证码消息
                }
            }

            if (_phone_check[phone].Count > 10)
            {
                return false;
            }


            if (RandKey>0)
            {
                //调用短信网关发送短信
                string to = phone.ToString();
                string msg = String.Format("您正在登录 【{0}】 验证码:{1},切勿将验证码泄露于他人,本条验证码有效期15分钟!", _appSettings.Name, RandKey.ToString());

                //SendSMS_chuanglan(to, msg);
                SendSMS_banlinig(to, msg);
            }



            //清除短信
            //_phone_check.Clear();
            foreach (var item in _phone_check.ToList())
            {
                TimeSpan ts = DateTime.Now - item.Value.CreateTime;
                if (ts.TotalMinutes > 15)
                {
                    _phone_check.Remove(item.Key);
                }
            }

            //if (_phone_check.Count() > 100)
            //{
            //}

            return true;
        }


        //手机登录
        public LoginRetModel LoginWithPhone(long phone, string password)
        {
            int check_code = 0;
            try
            {
                if (phone > 2000 && phone < 3000)
                {   //服务器 单独处理
                    var usr = _context.Users.SingleOrDefault(x => x.Phone == phone);
                    if (usr != null && usr.Career == password)
                    {
                        return Authenticate(phone, password);
                    }
                    else
                        throw new AppException("验证码不正确!");
                }


                //
                if (string.IsNullOrEmpty(password))
                    check_code = 0;
                else
                    check_code = Convert.ToInt32(password);

                if (_phone_check.ContainsKey(phone) && _phone_check[phone].RandomCode == check_code)
                    return Authenticate(phone, password);
                else if (string.IsNullOrEmpty(password) || (999999 == check_code && phone < 10000000000))
                {
                    return Authenticate(phone, password);
                }
                else
                {
                    throw new AppException("验证码不正确!");
                }
            }
            catch
            {
                throw new AppException("验证码不正确!");
            }
        }

        //如果用户手机号不存在自动建立账号 首次注册的用户允许验证码为空
        public LoginRetModel Authenticate(long phone, string password)
        {
            LoginRetModel ret = new LoginRetModel { Uid = 0, Token = "" };

            //asp.net 对于 ServerVariables["HTTP_X_FORWARDED_FOR"] ，asp.net core 中 可以改用 Request.Headers["X-Forwarded-For"] 
            _httpcontext.Request.Headers["X-Forwarded-For"].ToString();
            //获得 ip 地址信息
            string ipFrom = _httpcontext.Connection.RemoteIpAddress.ToString();


            var user = _context.Users.SingleOrDefault(x => x.Phone == phone);
            if (user == null)
            {
                int giftCoin= _config.GetValue<int>("Promotion:Regist");
                BalanceChange(1,(int)CoinsType.Coins,-giftCoin,"",0, (int)AccountChangeType.RewardRegister, "用户注册赠送金币");
                user = new User
                {
                    Phone = phone,
                    Name = "用户"+ (phone%10000).ToString(),
                    RegIp = ipFrom,
                    Coins = giftCoin,
                    pTalks= new ProfileTalks { }
                };

                //添加用户
                _context.Users.Add(user);
                _context.SaveChanges();//保存后才能拿到用户id
                //通知用户注册
            }
            else if (string.IsNullOrEmpty(password))
                return null;//如果手机号存在，但是登录的验证码为空 禁止登录


            user.Token = "Bearer " + CreateBeareTokenString(user.Id);
            _context.Users.Update(user);
            _context.SaveChanges();

            //避开手机号 通过插入一个指定的id 让系统自动增加用户id
            if (user.Id > 10000000000 && user.Id < 20000000000)
            {
                User usr = new User
                {
                    Id = 21234567890,
                    Phone = phone,
                    Name = "匿名用户",
                    RegIp = ipFrom
                };

                _context.Users.Add(usr);
                _context.SaveChanges();
            }


            ret.Token = user.Token;
            ret.Uid = user.Id;

            return ret;
            //return user.WithoutPassword();
        }

        public IEnumerable<User> GetAll()
        {
            return _context.Users.WithoutPasswords();
        }

        public User UpdateProfile(long uid, JObject jusr)
        {

            if (jusr["labels"] != null)
            {
                var usr_tag = _context.UserTags
                    .AsNoTracking()
                    .Where(a => a.UserID == uid);
                List<UserTag> uts = new List<UserTag>();
                foreach (var u_t in usr_tag)
                {
                    uts.Add(new UserTag() { Id = u_t.Id });
                }
                _context.UserTags.RemoveRange(uts);
                //增加
                int[] items = new int[jusr["labels"].Count()];
                for (int i = 0; i < jusr["labels"].Count(); i++)
                {
                    var label = new UserTag { UserID = uid, TagID = (int)jusr["labels"][i] };

                    _context.UserTags.Add(label);
                }
                _context.SaveChanges();
            }

            if (jusr["marriage"] != null)
            {
                bool haveInfo = true;
                var marri = _context.Marriages.SingleOrDefault(t => t.UserId == uid);
                if (marri == null) {
                    marri = new Marriage { UserId = uid };
                    haveInfo = false;
                }

                if (jusr["marriage"]["height"] != null && jusr["marriage"]["height"].Type != JTokenType.Null)
                    marri.Height = (int)jusr["marriage"]["height"];
                if (jusr["marriage"]["height"] != null && jusr["marriage"]["height"].Type != JTokenType.Null)
                    marri.Weight = (int)jusr["marriage"]["height"];

                if (jusr["marriage"]["education"] != null)
                    marri.Education = (string)jusr["marriage"]["education"];
                if (jusr["marriage"]["salary"] != null && jusr["marriage"]["salary"].Type != JTokenType.Null)
                    marri.Salary = (int)jusr["marriage"]["salary"];

                if (jusr["marriage"]["house"] != null)
                    marri.House = (string)jusr["marriage"]["house"];
                if (jusr["marriage"]["cars"] != null)
                    marri.Cars = (string)jusr["marriage"]["cars"];
                if (jusr["marriage"]["addrWork"] != null)
                    marri.AddrWork = (string)jusr["marriage"]["addrWork"];
                if (jusr["marriage"]["addrHome"] != null)
                    marri.AddrHome = (string)jusr["marriage"]["addrHome"];
                if (jusr["marriage"]["status"] != null)
                    marri.Status = (string)jusr["marriage"]["status"];

                if (jusr["marriage"]["desp"] != null)
                    marri.Desp = (string)jusr["marriage"]["desp"];
                if (jusr["marriage"]["expect"] != null)
                    marri.Expect = (string)jusr["marriage"]["expect"];
                if (jusr["marriage"]["future"] != null)
                    marri.Future = (string)jusr["marriage"]["future"];

                if (haveInfo)
                    _context.Marriages.Update(marri);
                else
                    _context.Marriages.Add(marri);

                _context.SaveChanges();
            }

            //基本信息
            var usr = _context.Users.SingleOrDefault(x => x.Id == uid);
            if (usr == null)
                return null;

            if (jusr["mood"] != null && jusr["mood"].Type != JTokenType.Null)
                usr.Mood = (string)jusr["mood"];

            if (jusr["name"] != null)
                usr.Name = (string)jusr["name"];

            if (jusr["sex"] != null)
                usr.Sex = (int)jusr["sex"];

            if (jusr["avatar"] != null)
                usr.Avatar = (string)jusr["avatar"];

            if (jusr["birthday"] != null)
            {
                string birth = (string)jusr["birthday"];
                if (string.IsNullOrEmpty(birth))
                    birth = "2000-01-01";
                DateTime dt = DateTime.ParseExact(birth, "yyyy-MM-dd", System.Globalization.CultureInfo.CurrentCulture);
                if (dt.Year > 1900)
                    usr.Birthday = dt;
            }

            //扩展信息
            if (jusr["price"] != null) {
                usr.Price = (int)jusr["price"];
            }



            _context.Users.Update(usr);
            _context.SaveChanges();
            return usr.WithoutPassword();
        }


        //辅助函数  默认7天有效
        public string CreateBeareTokenString(long Id, int days = 30)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(days),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);
            return tokenString;
        }
        //创蓝sms短信api
        private string SendSMS_chuanglan(string _to, string _msg)
        {
            string un = "N0305170";
            string pw = "ZwJTNuAiFzca58";
            string phone = _to;
            //设置您要发送的内容：其中“【】”中括号为运营商签名符号，多签名内容前置添加提交
            //string content = "【253云通讯】" + System.Net.WebUtility.UrlEncode(_msg);
            string content = System.Net.WebUtility.UrlEncode(_msg);


            string postJsonTpl = "\"account\":\"{0}\",\"password\":\"{1}\",\"phone\":\"{2}\",\"report\":\"true\",\"msg\":\"{3}\"";
            string jsonBody = string.Format(postJsonTpl, un, pw, phone, content);

            string result = doPostMethodToObj("http://smssh1.253.com/msg/send/json", "{" + jsonBody + "}");//请求地址请登录zz.253.com获取

            return result;

        }
        private static string doPostMethodToObj(string url, string jsonBody)
        {
            try
            {
                string result = String.Empty;
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                // Create NetworkCredential Object 
                NetworkCredential admin_auth = new NetworkCredential("username", "password");
                // Set your HTTP credentials in your request header
                httpWebRequest.Credentials = admin_auth;
                //// callback for handling server certificates
                //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                using (StreamWriter streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(jsonBody);
                    streamWriter.Flush();
                    streamWriter.Close();
                    HttpWebResponse httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    using (StreamReader streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        result = streamReader.ReadToEnd();
                    }
                }
                return result;

            }catch (WebException e) 
            {
                Console.WriteLine(e.Message);
                //throw AppException(FinalResponse = e.Message);
            }catch (System.Exception e)
            {
                Console.Write("\r\nRequest failed. Reason:");
                Console.WriteLine(e.Message);
            }finally
            {
            }
            return "";
        }


        private static string SendSMS_banlinig(string _to, string _msg)
        {
            string result = String.Empty;

             const string host = "http://smsbanling.market.alicloudapi.com";
             const string path = "/smsapis";
             const string method = "GET";
             const string appcode = "4f382d3ce114418997238769954740e3";

            //_to = "13521862883";
            //_msg = "breeze 验证码171717";

            string querys = "mobile="+ _to+"&msg=" + WebUtility.UrlEncode(_msg);// +"&sign=%E6%B6%88%E6%81%AF%E9%80%9A"
            string url  = host + path;
                    url = url + "?" + querys;


            try {
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(url);
                req.Method = method;
                req.Headers.Add("Authorization", "APPCODE " + appcode);//添加appcode

                //using (StreamWriter streamWriter = new StreamWriter(req.GetRequestStream()))
                //{
                //    //streamWriter.Write(jsonBody);
                //    streamWriter.Flush();
                //    streamWriter.Close();
                //}
                HttpWebResponse httpResponse = (HttpWebResponse)req.GetResponse();
                using (StreamReader streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                }
            }
            catch (WebException e)
            {
                Console.WriteLine(e.Message);
                //throw AppException(FinalResponse = e.Message);
            }catch (System.Exception e)
            {
                Console.Write("\r\nRequest failed. Reason:");
                Console.WriteLine(e.Message);
            }finally
            {
                int a = 0;a++;
            }


            return result;
        }

        //提现
        public IEnumerable<WithDraw> GetWithDraws()
        {
            //var q = from w in _context.WithDraws
            //		where w.Id > 0
            //        select new DTOFinaceReport{
            //	            UserId=w.UserId,
            //         				BankName=w.BankName,
            //         				BankAdress=w.BankAdress,
            //         				BankAccount=w.BankAccount,
            //         				BankUserName=w.BankUserName,
            //         				Money=w.Money
            //        };
            //int a = q.Count();
            //return q.ToList();


            //时间计算 参考
            //https://blog.csdn.net/liu_ben_qian/article/details/8516395
            //开始截止时间计算有误
            DateTime dt = DateTime.Now.Date; //当前时间

            DateTime startWeek = dt.AddDays(1 - Convert.ToInt32(dt.DayOfWeek.ToString("d"))); //本周周一           
            DateTime endWeek = startWeek.AddDays(6); //本周周日

            DateTime startMonth = dt.AddDays(1 - dt.Day); //本月月初
            DateTime endMonth = startMonth.AddMonths(1).AddDays(-1); //本月月末

            //DateTime endMonth = startMonth.AddDays((dt.AddMonths(1) - dt).Days - 1); //本月月末

            DateTime startQuarter = dt.AddMonths(0 - (dt.Month - 1) % 3).AddDays(1 - dt.Day); //本季度初
            DateTime endQuarter = startQuarter.AddMonths(3).AddDays(-1); //本季度末

            DateTime startYear = new DateTime(dt.Year, 1, 1); //本年年初
            DateTime endYear = new DateTime(dt.Year, 12, 31); //本年年末


            //上周
            DateTime lastWeek1 = dt.AddDays(Convert.ToInt32(1 - Convert.ToInt32(DateTime.Now.DayOfWeek)) - 7);

            DateTime thisWeek1 = dt.AddDays(1 - Convert.ToInt32(dt.DayOfWeek.ToString("d")));

            //DateTime lastWeek7 =dt.AddDays(Convert.ToInt32 (1- Convert.ToInt32(DateTime.Now.DayOfWeek)) -7).AddDays(6); 

            return _context.WithDraws.AsNoTracking().Where(t => t.CreateTime >= lastWeek1 && t.CreateTime < thisWeek1);
        }

        //提现按月
        public IEnumerable<WithDraw> GetWithDrawsMonth()
        {
            //DateTime dt=DateTime.Now.ToString("yyyy-MM-01");
            //DateTime.Parse(DateTime.Now.ToString("yyyy-MM-01")).AddMonths(1).AddDays(-1).ToShortDateString(); //上个月，减去一个月份
            //开始截止时间计算有误
            DateTime dt = DateTime.Now.Date; //当前时间

            DateTime startMonth = dt.AddDays(1 - dt.Day); //本月月初
            DateTime lastMonth = startMonth.AddMonths(-1); //本月月初

            return _context.WithDraws.AsNoTracking().Where(t => t.CreateTime >= lastMonth && t.CreateTime < startMonth);
        }


        //提现  本周
        public IEnumerable<WithDraw> GetWithDrawsThisWeek()
        {
            //时间计算 参考
            //https://blog.csdn.net/liu_ben_qian/article/details/8516395
            //开始截止时间计算有误
            DateTime dt = DateTime.Now.Date; //当前时间

            DateTime startWeek = dt.AddDays(1 - Convert.ToInt32(dt.DayOfWeek.ToString("d"))); //本周周一           
            DateTime endWeek = startWeek.AddDays(6); //本周周日
            return _context.WithDraws.Where(t => t.CreateTime >= startWeek);
        }
        //提现 按月 本月
        public IEnumerable<WithDraw> GetWithDrawsThisMonth()
        {
            //DateTime dt=DateTime.Now.ToString("yyyy-MM-01");
            //DateTime.Parse(DateTime.Now.ToString("yyyy-MM-01")).AddMonths(1).AddDays(-1).ToShortDateString(); //上个月，减去一个月份
            //开始截止时间计算有误
            DateTime dt = DateTime.Now.Date; //当前时间

            DateTime startMonth = dt.AddDays(1 - dt.Day); //本月月初
            DateTime lastMonth = startMonth.AddMonths(-1); //本月月初

            return _context.WithDraws.AsNoTracking().Where(t => t.CreateTime >= startMonth);
        }



        private void DemoData()
        {
            //bool changed = false;
            ////可以接单的用户(提现)
            //for (long uid = 10001; uid < 10015; ++uid)
            //{
            //    User usr = _context.Users.Include(x => x.pTalks).FirstOrDefault(t => t.Id == uid);
            //    if (usr != null && usr.pTalks == null)
            //    {
            //        Random ran = new Random();
            //        int newPrice = ran.Next(0,2);
            //        newPrice *= 100;

            //        //商家测试数据
            //        if (uid > 10010)
            //            newPrice = ran.Next(-200, 0);

            //        usr.pTalks = new TalksProfile { Price = newPrice };
            //        _context.Users.Update(usr);

            //        changed = true;
            //    }
            //}
            //if (changed)
            //    _context.SaveChanges();
        }
        private bool ConvertToBool(int b)
        {
            return b == 0 ? false : true;
        }

        public dynamic GetAllProfile(){
            var uid = Convert.ToInt64(_httpcontext.User.Identity.Name);
            var pubTags = _context.Tags.AsNoTracking().Where(a => a.Id > 100&& a.Audit==1)
                                        .Select(b => new {
                                                            Id      = b.Id,
                                                            Name    = b.Name,
                                                            Desp    = b.Desp
                                                        }).ToList();

            var marri = _context.Marriages.AsNoTracking().SingleOrDefault(a => a.UserId == uid);

            return new { myself = GetMyInfo(uid), Tags = pubTags.AsEnumerable(),Marriage=marri};
        }

        public dynamic GetNewUser()
        {
            var myID = Convert.ToInt64(_httpcontext.User.Identity.Name);
            var usr = _context.Users.AsNoTracking()
                              .Where(c => c.Id != myID && c.Closed == 0&& c.Online==1&&c.Price>=0)
                              .Include(x => x.pTalks).Where(x => x.pTalks != null && x.pTalks.DisplayOrder > 0 && x.pTalks.TalkingId == 0)
                              .OrderByDescending(c =>c.Id)
                              //.Include(x => x.Labels).ThenInclude(y => y.Tag)
                              //.OrderBy(c => Guid.NewGuid())     //随机选择
                              .Take(5)
                              .Select(c => new
                              {
                                  Uid = c.Id,
                                  Name = c.Name,
                                  Sex = c.Sex,
                                  Avatar = c.Avatar,
                                  Mood = c.Mood,
                                  Birthday = c.Birthday.ToString("yyyy-MM-dd"),
                                  Price = (c.pTalks == null ? 200 : c.Price),
                                  Career = c.Career,
                                  Desp = c.Desp,
                                  Location = c.Location,
                                  Online = c.Online,
                                  Labels = c.Labels.OrderBy(e => e.Id).Select(e => e.Tag).Select(e => e.Name).ToArray()
                              }).ToList();

            return usr.AsEnumerable();
        }

        public dynamic GetODataUsers() {
            var myID = Convert.ToInt64(_httpcontext.User.Identity.Name);
            var usr = _context.Users.AsNoTracking()
                              .Where(c => c.Id != myID && c.Closed == 0)
                              .Include(x => x.pTalks).Where(x => x.pTalks != null && x.pTalks.DisplayOrder > 0 && x.pTalks.TalkingId == 0)
                              .OrderByDescending(c => c.Online)
                              .ThenByDescending(c => c.pTalks.DisplayOrder)
                              .Include(x => x.Labels).ThenInclude(y => y.Tag)
                              //.OrderBy(c => Guid.NewGuid())     //随机选择
                              //.Take(20)
                              .Select(c => new 
                              {
                                  Uid = c.Id,
                                  Name = c.Name,
                                  Sex = c.Sex,
                                  Avatar = c.Avatar,
                                  Mood=c.Mood,
                                  Birthday = c.Birthday.ToString("yyyy-MM-dd"),
                                  Price = (c.pTalks == null ? 200 : c.Price),
                                  Career = c.Career,
                                  Desp = c.Desp,
                                  Location = c.Location,
                                  Online = c.Online,
                                  Labels = c.Labels.OrderBy(e => e.Id).Select(e => e.Tag).Select(e => e.Name).ToArray()
                              }).ToList();

            return usr.AsEnumerable();
        }

        //根据tag 找用户
        public dynamic GetODataUsers(int tagId)
        {
            var myID = Convert.ToInt64(_httpcontext.User.Identity.Name);
            //var usr = _context.Users.Where(c => c.Id != myID && c.Closed == 0 && (c.Labels.FirstOrDefault(b=>b.TagID ==tagId)!=null))
            //                  .Include(x => x.pTalks).Where(x => x.pTalks != null && x.pTalks.DisplayOrder > 0 && x.pTalks.TalkingId == 0)
            //                  .OrderByDescending(c => c.Online)
            //                  .ThenByDescending(c => c.pTalks.DisplayOrder)
            //                  .Include(x => x.Labels).ThenInclude(y => y.Tag)
            //                  //.OrderBy(c => Guid.NewGuid())     //随机选择
            //                  //.Take(20)
            //                  .Select(c => new
            //                  {
            //                      Uid = c.Id,
            //                      Name = c.Name,
            //                      Sex = c.Sex,
            //                      Avatar = c.Avatar,
            //                      Birthday = c.Birthday.ToString("yyyy-MM-dd"),
            //                      Price = (c.pTalks == null ? 200 : c.pTalks.Price),
            //                      Career = c.Career,
            //                      Desp = c.Desp,
            //                      Location = c.Location,
            //                      Online = c.Online,
            //                      Labels = c.Labels.OrderBy(e => e.Id).Select(e => e.Tag).Select(e => e.Name).ToArray()
            //                  }).ToList();
            //return usr.AsEnumerable();
            
            var usr2 = _context.Users.AsNoTracking()
                                        .Where(c => c.Id != myID && c.Closed == 0)
                                        .Where(u => u.Labels.Any(r => r.TagID == tagId))
                                        .Include(x => x.pTalks)
                                        .Where(x => x.pTalks != null && x.pTalks.DisplayOrder > 0 && x.pTalks.TalkingId == 0)
                                        .OrderByDescending(c => c.Online)
                                        .ThenByDescending(c => c.pTalks.DisplayOrder)
                                        .Include(x => x.Labels).ThenInclude(y => y.Tag)
                                        //.OrderBy(c => Guid.NewGuid())     //随机选择
                                        //.Take(20)
                                        .Select(c => new
                                          {
                                              Uid = c.Id,
                                              Name = c.Name,
                                              Sex = c.Sex,
                                              Avatar = c.Avatar,
                                              Birthday = c.Birthday.ToString("yyyy-MM-dd"),
                                              Price = (c.pTalks == null ? 200 : c.Price),
                                              Career = c.Career,
                                              Desp = c.Desp,
                                              Location = c.Location,
                                              Online = c.Online,
                                              Labels = c.Labels.OrderBy(e => e.Id).Select(e => e.Tag).Select(e => e.Name).ToArray()
                                          }).ToList();

            return usr2.AsEnumerable();
        }

        public dynamic Social()
        {
            var tags = _context.Tags.AsNoTracking()
                                    .Where(t => t.Audit == 1).Select(c => new {
                                    Id = c.Id,
                                    Name = c.Name,
                                    Color = c.Color,
                                    Outline =(c.OutLine == 0 ? false : true)
                                }).ToList();


            return new { Online = 0, Talking = 0, Tags = tags.AsEnumerable() };
        }


    }
}
