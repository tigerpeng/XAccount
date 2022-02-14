using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;


using XAccount.Data;
using XAccount.Entities;
using XAccount.Models;
using XAccount.Helpers;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;     //JObject

namespace XAccount.Services
{
    public interface IMahjongService
    {
        MjProfileModel GetProfile(long uid);

        void AddScore(long xid, int scroe);
        void Transfer(long from, long toXID, int score);


        //游戏抵押筹码和结算
        MJRecordModel CheckChips(long bossid,string symbol, int maxScore,float tableFee, string desp,long[] gambles);
        void SpliteChips(long record, string result, List<Gamble> gambles);
        //void SpliteChips(long recID, int[] scores);

        dynamic GetAllDesks();
    }


    public class MahjongService : IMahjongService
    {
        private DataContext _context;

        private readonly HttpContext _httpcontext;
        private IUserService _userService;



        public MahjongService(DataContext context, IUserService userService, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _context.Database.EnsureCreated();

            _userService = userService;

            _httpcontext = httpContextAccessor.HttpContext;

        }


        //上分  xid=用户或者是手机号
        public void AddScore(long xid,int scroe)
        {
            User usr=null;
            if(IsPhoneNo(xid))
                usr = _context.Users.Include(x => x.pGame).FirstOrDefault(t => t.Phone == xid);
            else
                usr = _context.Users.Include(x => x.pGame).FirstOrDefault(t => t.Id == xid);

            if (usr != null)
            {
                if (usr.pGame == null)
                    usr.pGame = new Mahjong { };

                    usr.Scores += scroe;

                _context.Users.Update(usr);
                _context.SaveChanges();
            }
        }
        //转账
        public void Transfer(long from,long toXID,int score)
        {
            User usrTo = null;
            if (IsPhoneNo(toXID))
                usrTo = _context.Users.Include(x => x.pGame).FirstOrDefault(t => t.Phone == toXID);
            else
                usrTo = _context.Users.Include(x => x.pGame).FirstOrDefault(t => t.Id == toXID);

            User usrFrom= _context.Users.Include(x => x.pGame).FirstOrDefault(t => t.Id == from);
            if (usrFrom!=null && usrFrom.pGame!=null&& usrTo!=null)
            {
                if (usrTo.pGame == null)
                    usrTo.pGame = new Mahjong { };

                if (usrFrom.Scores >= score)
                {
                    usrFrom.Scores -= score;
                    usrTo.Scores += score;

                    _context.Users.Update(usrFrom);
                    _context.Users.Update(usrTo);
                    _context.SaveChanges();

                    //todo 生成转账记录 

                }
            }
        }

        //查询自己的个人信息
        public MjProfileModel GetProfile(long uid)
        {
            if (uid == 0)
                uid = Convert.ToInt64(_httpcontext.User.Identity.Name);

            //var myID = Convert.ToInt64(_httpcontext.User.Identity.Name);
            var usr = _context.Users.Include(x => x.pGame).FirstOrDefault(t => t.Id == uid);
            if (usr == null)
                throw new AppException("账号不存在,UserId:"+ uid.ToString());

            var profile = new MjProfileModel
            {
                Id = usr.Id,
                Name = usr.Name,
                Avatar = usr.Avatar
            };
            if (usr.pGame != null)
            {
                profile.Score = usr.Scores;

                profile.Win = usr.pGame.Win;
                profile.Draw = usr.pGame.Draw;
                profile.Lose = usr.pGame.Lose;
            }

            return profile;
        }


        //检查用户资金是否够
        public MJRecordModel CheckChips(long fid, string symbol, int pawn, float tableFee, string desp, long[] gambles)
        {
            //结算货币类型
            int scoreType = 0;
            if ("COIN" == symbol)
                scoreType = (int)CurrencyType.Coins;
            else if("SCR"==symbol)
                scoreType = (int)CurrencyType.Scores;
            

            var ret = new MJRecordModel { Id = 0, Poors = new List<long>()};
            var record = new MahjongRecord
            {
                Symbol=scoreType,   //货币类型
                Chips= pawn,        //每人入场筹码
                FamilyId = fid,
                Desp = desp,
                PlayerNumber = gambles.Count(),
                TableFee= tableFee,
                Gambles = new List<Gamble>()
            };

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    int total = 0;
                    for (int i = 0; i < gambles.Count(); ++i)
                    {
                        long uid = gambles[i];
                        var usr = _context.Users.Include(x => x.pGame).FirstOrDefault(t => t.Id == uid);
                        if (usr != null)
                        {
                            if (usr.pGame == null)
                            {
                                usr.pGame = new Mahjong { };
                                _context.Users.Update(usr);
                            }

                            //保证所有玩家是同一个家族才能使用积分
                            if (scoreType == (int)CurrencyType.Scores && fid > 0 && usr.FamilyId != fid)
                                throw new AppException("玩家不在同一个组织,不能使用积分!");

                            var g = new Gamble
                            {
                                UserId  = usr.Id
                                //Chip    = pawn
                            };


                            //抛出异常 表示资金不足
                            try {
                                if (_userService.BalanceChange(usr.Id, scoreType, -pawn, "筹码抵押", 0, (int)AccountChangeType.ChipsPawn) > 0)
                                {
                                    record.Gambles.Add(g);
                                    total++;
                                }
                                else
                                    ret.Poors.Add(uid);
                            }catch (Exception e){
                                ret.Poors.Add(uid);
                            }

                        }else
                        {//用户不存在，搞毛线？
                            ret.Poors.Add(uid);
                        }
                    }


                    if (total == gambles.Count())
                    {
                        _context.MJRecords.Add(record);
                        _context.SaveChanges();
                        ret.Id = record.Id;
                    }


                    transaction.Commit();
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    throw new AppException(e.Message + " 请求失败");
                }
            }

            return ret;
        }

        public void SpliteChips(long recID,string result, List<Gamble> gbls)
        {
            int totalFee = 0;
            var rec = _context.MJRecords.Include(x => x.Gambles).FirstOrDefault(t => t.Id == recID && t.Settle == 0);

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    if (rec != null && rec.Gambles != null && rec.Gambles.Count() == gbls.Count())
                    {
                        //int totalWin = 0;
                        int totalLose = 0;
                        //先计算全部输家的钱
                        for (int i = 0; i < gbls.Count(); ++i)
                        {
                            rec.Gambles[i].Piao = gbls[i].Piao;
                            rec.Gambles[i].Gang = gbls[i].Gang;
                            rec.Gambles[i].Hu = gbls[i].Hu;


                            int myScore = gbls[i].Piao + gbls[i].Gang + gbls[i].Hu;
                            if (myScore < 0)
                            {
                                //扣除茶费
                                //计算每个人应付茶费
                                int teaFee = 0;
                                if (rec.TableFee > 1)
                                    teaFee = (int)rec.TableFee;                             //茶费模式 坐下即付费

                                myScore -= teaFee;
                                totalFee += teaFee;

                                //最多输掉全部筹码
                                if (Math.Abs(myScore) > rec.Chips)
                                    myScore = -rec.Chips;

                                totalLose += (myScore+ teaFee);


                                //to do 扩展其他记录
                                //更新用户分数表 自动赎回筹码 统计胜负平战绩
                                var uid = rec.Gambles[i].UserId;
                                var usr = _context.Users.Include(x => x.pGame).FirstOrDefault(t => t.Id == uid);
                                if (usr != null && usr.pGame != null)
                                {
                                    if (myScore > 0)
                                        usr.pGame.Win += 1;
                                    else if (myScore < 0)
                                        usr.pGame.Lose += 1;
                                    else
                                        usr.pGame.Draw += 1;
                                    //本局结束剩余的筹码
                                    int addScore = rec.Chips + myScore;
                                    if (addScore > 0)
                                    {
                                        rec.Gambles[i].Broke = -teaFee;
                                        //还原账户自己
                                        //usr.pGame.Score += addScore;
                                        int scoreType = rec.Symbol;
                                        _userService.BalanceChange(usr.Id, scoreType, addScore, "筹码返还",rec.Id,(int)AccountChangeType.ChipsReturn, usr.Id.ToString());

                                    }
                                    _context.Users.Update(usr);
                                }
                            }
                        }

                        //再计算全部赢家的钱
                        for (int i = 0; i < gbls.Count(); ++i)
                        {
                            int myScore = gbls[i].Piao + gbls[i].Gang + gbls[i].Hu;
                            if (myScore >= 0)
                            {
                                int teaFee = 0;
                                if (rec.TableFee > 1)
                                    teaFee = (int)rec.TableFee;                             //茶费模式 坐下即付费
                                else 
                                    teaFee = (int)(myScore * rec.TableFee);                 //提成模式 赢钱即提成

                                totalLose += teaFee;
                                myScore -= teaFee;

                                totalFee += teaFee;

                                if (myScore >= Math.Abs(totalLose))
                                {
                                    myScore = Math.Abs(totalLose);
                                    totalLose = 0;
                                }
                                else {
                                    totalLose += myScore;
                                }

                                var uid = rec.Gambles[i].UserId;
                                var usr = _context.Users.Include(x => x.pGame).FirstOrDefault(t => t.Id == uid);
                                if (usr != null && usr.pGame != null)
                                {
                                    if (myScore > 0)
                                        usr.pGame.Win += 1;
                                    else if (myScore < 0)
                                        usr.pGame.Lose += 1;
                                    else
                                        usr.pGame.Draw += 1;

                                    //本局结束剩余的筹码
                                    int addScore = rec.Chips + myScore;

                                    //筹码扣除或者加上分数后 返还账户
                                    //扣除茶费
                                    if (addScore > 0)
                                    {
                                        rec.Gambles[i].Broke = -teaFee;
                                        //还原账户自己
                                        //usr.pGame.Score += addScore;
                                        int scoreType = rec.Symbol;
                                        _userService.BalanceChange(usr.Id, scoreType, addScore, "筹码返还", (int)AccountChangeType.ChipsReturn);

                                    }
                                    _context.Users.Update(usr);
                                }
                            }
                        }


                        //完成时 计算
                        rec.Result          = result;
                        rec.Brokerage       = totalFee;      //本局系统总佣金 
                        rec.FinishedTime    = DateTime.Now;
                        rec.Settle = 1;


                        //如果是茶馆积分模式 计算茶馆收益
                        if (rec.FamilyId > 0&&rec.Symbol== (int)CurrencyType.Scores)
                        {
                            var f = _context.Familys.SingleOrDefault(x=>x.Id== rec.FamilyId);
                            if (f != null)
                            {
                                f.Scores += totalFee;
                                _context.Familys.Update(f);
                            }
                        }


                        _context.MJRecords.Update(rec);
                        _context.SaveChanges();
                    }



                    transaction.Commit();
                }catch (Exception e)
                {
                    transaction.Rollback();

                    //结算异常
                    rec.Settle = -1;
                    _context.MJRecords.Update(rec);
                    _context.SaveChanges();

                    throw new AppException(e.Message + " 请求失败");
                }
            }
        }

        //对出错的记录  返回用户账户的抵押 
        public void RetrunChips()
        {
            //过去2小时 尚未结算的游戏记录认定为错误记录
            var gameEnd = DateTime.Now.AddHours(-2);
            var rds = _context.MJRecords.Include(x => x.Gambles).Where(t => t.Settle == 0 && t.FinishedTime < gameEnd);


            //rds.ForEach(a => a.Settle = -1);
            //_context.SaveChanges();
        }

        //检查输入的是否是手机号
        private bool IsPhoneNo(long xid)
        {
            if (xid > 10000000000 && xid < 20000000000)
                return true;
            else
                return false;
        }


        //返回所有的桌子 客户端根据falimiid 自动分类
        public dynamic GetAllDesks()
        {
            var clubs = _context.FamilyDesks.ToList();

            return clubs;
        }


    }
}
