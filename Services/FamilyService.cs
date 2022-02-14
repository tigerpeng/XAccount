using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

using Microsoft.Extensions.Configuration;


using XAccount.Data;
using XAccount.Entities;
using XAccount.Models;
using XAccount.Helpers;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;     //JObject

namespace XAccount.Services
{
    public interface IFamilyService
    {
        IEnumerable<FamilyModel> GetAll(bool noMy=false);

        FamilyModel GetMyAddFamily();

        Family FamilyCreate(Family model);
        Family FamilyUpdate(long fid, Family model);
        Family GetMyOwnerFamily();

        FamilyScoreInfoModel ScoreInfo();
        FamilyScoreInfoModel ScoreUp(TransferModel model);
        FamilyScoreInfoModel ScoreDown(TransferModel model);

        //积分转账
        void TransferScore(TransferModel model);

        dynamic MembersInfo();

        Family  FamilyJoin(long fid);
        void    LeaveFamily(bool force=false);

        bool   FamilyExsit(long fid);
    }


    public class FamilyService: IFamilyService
    {
        private DataContext             _context;
        private readonly HttpContext    _httpcontext;
        private IConfiguration          _config;
        private IWebHostEnvironment     _hostingEnvironment;
        private IUserService            _userService;

        public FamilyService(DataContext context,IUserService userService, IHttpContextAccessor httpContextAccessor, IConfiguration config, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _context.Database.EnsureCreated();

            _userService = userService;

            _httpcontext = httpContextAccessor.HttpContext;
            _config = config;
            _hostingEnvironment = hostingEnvironment;

            //_config.GetValue<string>("appSettings:Name")
        }
        private string GetPictureUrl(long fid) {
            string webRootPath = _hostingEnvironment.WebRootPath;
            string fullPath = Path.Combine(webRootPath, "img/f/"+ fid.ToString() + ".jpg");
            if (!File.Exists(fullPath)) {
                return "/img/f/0.jpg"; 
            }

            return  "/img/f/" + fid.ToString() + ".jpg"; 
        }
        // s.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"),
        public IEnumerable<FamilyModel> GetAll(bool noMy)
        {
            long myFamilyId = 0;
            long uid = Convert.ToInt64(_httpcontext.User.Identity.Name);

            if (noMy) {//不含自己的家族
                var usr = _context.Users.SingleOrDefault(x => x.Id == uid);
                if (usr != null)
                    myFamilyId = usr.FamilyId;
            }


            var all = _context.Familys
                                .Where(x=>x.DisplayOrder>0 && x.Id!= myFamilyId)
                                .Take(100)
                                .OrderByDescending(x=>x.Id)
                                .Select(c => new FamilyModel
                                {
                                    Id = c.Id,
                                    Name = c.Name,
                                    Desp = c.Desp,
                                    CreateTime = c.CreateTime.ToString("yyyy-MM-dd"),
                                    Picture=""// GetPictureUrl(c.Id)
                                }).ToList();

            return all.AsEnumerable();
        }
        //理论上一个人只能加入一个家族
        public Family GetMyOwnerFamily()
        {
            long uid = Convert.ToInt64(_httpcontext.User.Identity.Name);
            var  ownerfamily = _context.Familys
                            .Where(x => x.MasterId == uid)
                            .FirstOrDefault();//放在 select 后面

            if (ownerfamily != null)
                return ownerfamily;
            else
                return new Family { Id=0};
        }
        public FamilyModel GetMyAddFamily()
        {
            long uid = Convert.ToInt64(_httpcontext.User.Identity.Name);
            var usr = _context.Users.SingleOrDefault(x => x.Id == uid);
            if (usr != null)
            {
                var f = _context.Familys.SingleOrDefault(x => x.Id == usr.FamilyId);

                if(f!=null)
                    return new FamilyModel
                    {
                        Id = f.Id,
                        Name = f.Name,
                        Desp = f.Desp,
                        CreateTime = f.CreateTime.ToString("yyyy-MM-dd"),
                        Picture = GetPictureUrl(f.Id)
                    };
            }
            return null;
        }
        public FamilyModel FindFamily(long fid)
        {
            var f = _context.Familys.SingleOrDefault(x => x.Id == fid);

            if(f!=null)
            return new FamilyModel
            {
                Id = f.Id,
                Name = f.Name,
                Desp = f.Desp,
                CreateTime = f.CreateTime.ToString("yyyy-MM-dd"),
                Picture = GetPictureUrl(f.Id)
            };
            return null;
        }

        //家族长相关 创建家族需要消耗金币
        public Family FamilyCreate(Family model)
        {
            //读取配置--创建家族需要消耗的金币
            int coinMust= _config.GetValue<int>("Family:CreateCoin_"+ model.Type.ToString());
            if(coinMust==0)
                throw new AppException("没有这种类型 type=" + model.Type.ToString());

            long uid = Convert.ToInt64(_httpcontext.User.Identity.Name);
            //更行自己的家族
            var usr = _context.Users.SingleOrDefault(x => x.Id == uid);
            if (usr == null)
                throw new AppException("您账户不存在");

            var f = _context.Familys.SingleOrDefault(x => x.MasterId == uid);
            if (f!=null)
                throw new AppException("每人仅限创建一个,您已经拥有一个");


            if (_userService.BalanceChange(uid, (int)CurrencyType.Coins, -coinMust, "创建家族", 0, (int)AccountChangeType.SystemBuyFamily)>0)
            {
                //必须为正数
                model.JoinAward = Math.Abs(model.JoinAward);
                model.Tickets = Math.Abs(model.Tickets);
                model.TeaFee = Math.Abs(model.TeaFee);
                model.TransferFee = Math.Abs(model.TransferFee);
                model.TalksFee = Math.Abs(model.TeaFee);
                if (model.TalksFee > 1)
                    model.TalksFee = 1;




                //下面的属性 必须系统设置
                model.MasterId = uid;
                model.Beans = 0;
                model.BeanRate = 0.5F;      //提成
                model.Scores = coinMust;    //将用户购买家族的金币--转换成家族总积分（类似于保证金，但是不退换给用户）
                model.DisplayOrder = 1;

                _context.Familys.Add(model);
                _context.SaveChanges();



                usr.FamilyId = model.Id;
                _context.Users.Update(usr);
                _context.SaveChanges();


                //默认创建10桌 3 人 2 人各5桌
                _context.FamilyDesks.Add(new Desk { familyID = model.Id, brokerage = model.TeaFee, players = 3, total = 3 , buyhorse=1});
                _context.FamilyDesks.Add(new Desk { familyID = model.Id, brokerage = model.TeaFee, players = 3, total = 2});

                _context.FamilyDesks.Add(new Desk { familyID = model.Id, brokerage = model.TeaFee, players = 2, total = 3, buyhorse = 1 });
                _context.FamilyDesks.Add(new Desk { familyID = model.Id, brokerage = model.TeaFee, players = 2, total = 2 });
                _context.SaveChanges();

                return model;
            }
            else
                throw new AppException("金币不足,请充值!创建家族需要消耗金币:" + coinMust.ToString());
        }

        public Family FamilyUpdate(long fid, Family model)
        {
            long uid = Convert.ToInt64(_httpcontext.User.Identity.Name);
            var family = _context.Familys.SingleOrDefault(x => x.Id == fid);
            if (family == null)
                throw new AppException("家族不存在 家族编号:" + fid.ToString());
            else if (family.MasterId != uid)
                throw new AppException("您不是家族长,无法修改家族信息");

            if (model.Name!= null)
                family.Name = model.Name;
            if (model.Desp != null)
                family.Desp = model.Desp;

            family.Tickets      = Math.Abs(model.Tickets);
            family.JoinAward    = Math.Abs(model.JoinAward);
            family.TeaFee       = Math.Abs(model.TeaFee);
            family.TransferFee  = Math.Abs(model.TransferFee);

            family.TalksFee     = Math.Abs(model.TalksFee);
            if (family.TalksFee > 1)
                family.TalksFee = 1;

            _context.Familys.Update(family);
            _context.SaveChanges();

            return family;
        }
        public bool FamilyExsit(long fid)
        {
            long uid = Convert.ToInt64(_httpcontext.User.Identity.Name);
            var family = _context.Familys.SingleOrDefault(x => x.Id == fid);
            if (family!=null && family.MasterId == uid)
                return true;
            return false;
        }
        public void LeaveFamily(bool force )
        {
            long uid = Convert.ToInt64(_httpcontext.User.Identity.Name);
            var usr = _context.Users.SingleOrDefault(x => x.Id == uid);
            if (usr != null)
            {
                if(usr.FamilyId>0&&usr.Scores>0&&!force)
                    throw new AppException("您账户上尚有"+ usr.Scores.ToString()+"积分, 离开会清空积分,建议您先下分,再离开!强制离开请选择ok(确定)");


                //解除和家族的绑定关系
                usr.FamilyId = 0;

                if(usr.Scores > 0)
                _userService.BalanceChange(uid, (int)CurrencyType.Scores, -usr.Scores, "清空积分", 0, (int)AccountChangeType.LeaveFamily);

                _context.Users.Update(usr);
                _context.SaveChanges();
            }

        }

        public Family FamilyJoin(long fid)
        {
            var family = _context.Familys.SingleOrDefault(x => x.Id == fid);
            if (family == null )
                throw new AppException("家族不存在 家族编号:" + fid.ToString());

            long uid = Convert.ToInt64(_httpcontext.User.Identity.Name);
            var usr  = _context.Users.SingleOrDefault(x => x.Id == uid);

            if (usr != null)
            {
                if(usr.Scores>0&&usr.FamilyId!= fid)
                    throw new AppException("您账户有积分,请先下分再加入!");


                usr.FamilyId = fid;

                //if (usr.pGame == null)
                //    usr.pGame = new Mahjong {  };

                //必须清空用户积分
                usr.Scores = 0;

                if (family.JoinAward > 0&& family.Scores> family.JoinAward)
                {
                    usr.Scores = family.JoinAward;
                    family.Scores -= family.JoinAward;
                    _context.Familys.Update(family);
                }

                _context.Users.Update(usr);
                _context.SaveChanges();
            }

            return family;
        }


        public FamilyScoreInfoModel ScoreInfo()
        {
            FamilyScoreInfoModel ret = new FamilyScoreInfoModel();

            long uid = Convert.ToInt64(_httpcontext.User.Identity.Name);
            var usr = _context.Users.SingleOrDefault(x => x.Id == uid);
            if (usr != null)
            {
                ret.BalanceUser = usr.Scores;
            }

            var family = _context.Familys.SingleOrDefault(x => x.MasterId == uid);
            if (family != null) {
                ret.BalanceFamily = family.Scores;

                //var members = _context.Users.Where(x => x.FamilyId == family.Id).Select(c=> new {
                //                id=c.Id,
                //                name=c.Name,
                //                phone=c.Phone,
                //                score=c.Scores
                //                }).ToList();

                //return new { Scores= ret, Members = members };
            }


            return ret;
        }
        public dynamic MembersInfo() {
            long uid = Convert.ToInt64(_httpcontext.User.Identity.Name);
            var family = _context.Familys.SingleOrDefault(x => x.MasterId == uid);
            if (family != null)
            {

                var members = _context.Users.Where(x => x.FamilyId == family.Id).Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    sex=c.Sex,
                    phone = c.Phone,
                    online=c.Online,
                    score = c.Scores
                }).ToList();

                return members;
            }
            return new { };
        }

        //茶馆 boss 才能上分
        public FamilyScoreInfoModel ScoreUp(TransferModel model)
        {
            FamilyScoreInfoModel ret = new FamilyScoreInfoModel();

            long peerID = _userService.GetUserIdFromIdOrPhone(model.To);
            int  amount = model.Amount;
                 amount = Math.Abs(amount); //保证是正数

            long uid = Convert.ToInt64(_httpcontext.User.Identity.Name);
            var family = _context.Familys.SingleOrDefault(x => x.MasterId == uid);
            if (family == null)
                throw new AppException("您尚未购买club,不能上分");

            ret.BalanceFamily = family.Scores;


            var  usr = _context.Users.SingleOrDefault(x => x.Id == peerID);
            if (usr != null && usr.FamilyId == family.Id)
            {//
                if (family.Scores > amount)
                {
                    family.Scores -= amount;




                    //
                    _userService.BalanceChange(usr.Id, (int)CurrencyType.Scores, amount, "上分", family.Id, (int)AccountChangeType.ScoreUp);


                    ret.BalanceFamily = family.Scores;
                    ret.BalanceUser = usr.Scores;

                    _context.Familys.Update(family);
                    _context.SaveChanges();
                }

            }else {
                if(usr == null)
                    throw new AppException("用户不存在");
                if(usr.FamilyId!= family.Id)
                    throw new AppException("对方尚未加入,无法上分");
            }

            return ret;
        }

        public FamilyScoreInfoModel ScoreDown(TransferModel model)
        {
            int amount = model.Amount;
                amount = Math.Abs(amount);

            FamilyScoreInfoModel ret = new FamilyScoreInfoModel();

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    long uid = Convert.ToInt64(_httpcontext.User.Identity.Name);
                    var usr = _context.Users.SingleOrDefault(x => x.Id == uid);
                    if (usr != null && usr.FamilyId > 0 && usr.Scores >= amount)
                    {
                        var family = _context.Familys.SingleOrDefault(x => x.Id == usr.FamilyId);
                        if (family == null)
                            throw new AppException("您尚未加入club,不能下分");

                        if (usr.Scores >= amount)
                        {
                            _userService.BalanceChange(usr.Id, (int)CurrencyType.Scores, -amount, "下分", family.Id, (int)AccountChangeType.ScoreDown);

                            family.Scores += amount;
                            _context.Familys.Update(family);
                            _context.SaveChanges();


                            ret.BalanceFamily = family.Scores;
                            ret.BalanceUser = usr.Scores;
                        }
                        else
                        {
                            throw new AppException("您积分账户余额不足,不能下分!");
                        }

                    }

                    transaction.Commit();
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    throw new AppException("创建会话失败!" + e.Message);
                }
            }

            return ret;
        }

        public void TransferScore(TransferModel model)
        {
            long uid = Convert.ToInt64(_httpcontext.User.Identity.Name);
            var usr = _context.Users.SingleOrDefault(x => x.Id == uid);
            if (usr != null && usr.FamilyId > 0)
            {
                var family = _context.Familys.SingleOrDefault(x => x.Id == usr.FamilyId);
                if (family == null)
                    throw new AppException("您尚未加入club,不能下分");


                var uidPeer=_userService.GetUserIdFromIdOrPhone(model.To);
                var usrPeer = _context.Users.SingleOrDefault(x => x.Id == uidPeer);
                if(usrPeer==null|| usrPeer.FamilyId!= family.Id)
                    throw new AppException("不在一个组织,无法使用积分转账!");

                //
                model.Amount = Math.Abs(model.Amount);

                int totalFee = 0;
                float fee = family.TransferFee;
                if (fee > 1)
                    totalFee = model.Amount + (int)fee;
                else
                    totalFee = (int)(model.Amount * (1 + fee));

                if(usr.Scores< totalFee)
                    throw new AppException("积分余额不足,不能转账!需要:"+ totalFee.ToString()+"积分");


                //开启事务
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        _userService.BalanceChange(usr.Id, (int)CurrencyType.Scores, -totalFee, "积分转出:"+usrPeer.Id.ToString(), usrPeer.Id, (int)AccountChangeType.ScoreTransfer);

                        _userService.BalanceChange(usrPeer.Id, (int)CurrencyType.Scores,model.Amount, "积分转入:" + usr.Id.ToString(), usr.Id, (int)AccountChangeType.ScoreTransfer);

                        //usr.Scores     -= totalFee;
                        //usrPeer.Scores += model.Amount;
                        //_context.Users.Update(usr);
                        //_context.Users.Update(usrPeer);
                        if (totalFee - model.Amount > 0)
                            family.Scores += totalFee - model.Amount;

                        _context.Familys.Update(family);
                        _context.SaveChanges();

                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        throw new AppException("转账失败!" + e.Message);
                    }
                }
            }

        }

        private dynamic get_score_down_list(long fid)
        {
            //&&x.Currency== (int)CurrencyType.Scores
            var ret = _context.UserBalanceChanges.Where(x=>x.RecordId==fid&&x.Type==(int)AccountChangeType.ScoreDown)
                .Select(c => new
                {
                    Time = c.CreateTime.ToString("MM-dd HH:mm"),
                    Amount = c.Amount,
                    UserID = c.UserId,
                    Remarks = c.Summary
                }).ToList();

            return ret;
        }
        //FamilyService end
    }
}
