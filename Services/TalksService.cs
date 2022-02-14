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


//签名
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;



//using Nethereum.Hex.HexConvertors.Extensions;
//using Nethereum.Signer;
//using Nethereum.Util;
//using Nethereum.Signer.Crypto;


using Microsoft.Extensions.Configuration;

using System.Threading.Tasks;


/*
 * 2021.3.24 最终确认 付费流程
 * 
 * 1 请求者创建会话
 *      同时客户端每分钟发送一次心跳签名
 * 2 应答者刷新会话
 * 
 * 3 应答者结束会话
 * 
 * 
 * 
 
 */

namespace XAccount.Services
{
    public interface ITalksService
    {
        bool IsSignlingServer();
        Task<TalksResponsetModel>       TalksCreate(TalksRequestModel model);
        Task<TalksResponsetModel>       TalksRefresh(TalksFinishModel model);
        Task<TalksResponsetModel>       TalksEnd(TalksFinishModel model);

        //定时修正数据
        Task                            TalksTimeFix();                         //修正聊天异常
        Task                            GameResultFix();                        //修正游戏异常
        //
        Task<dynamic> GetTalksChargeDetails(long uid,bool income);
    }


    public class TalksService : ITalksService
    {
        private DataContext             _context;
        private readonly HttpContext    _httpcontext;
        private IUserService            _userService;
        private IConfiguration          _config;

        public TalksService(DataContext context,IUserService userService,IHttpContextAccessor httpContextAccessor, IConfiguration config)
        {
            _context = context;
            _context.Database.EnsureCreated();

            _userService = userService;

            _httpcontext = httpContextAccessor.HttpContext;
            _config = config;

        }
        public bool IsSignlingServer()
        {
            string ss = _httpcontext.Connection.RemoteIpAddress.ToString();

            string ips = _config.GetValue<string>("appSetting.SignalingServer");
            if (ips.IndexOf(ss,StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            return true;
        }
        //member function
        //建立会谈/刷新会话扣费 请求者建立会话
        public async Task<TalksResponsetModel> TalksCreate(TalksRequestModel model) {
            TalksResponsetModel result = new TalksResponsetModel { };
            TalksRecord bill=null;

            long uid = Convert.ToInt64(_httpcontext.User.Identity.Name);
            if (model.RecordId!=0)
            {
                //应答者验证记录存在 //AsNoTracking().
                bill = await _context.TalksRecords.FirstOrDefaultAsync(t => t.Id == model.RecordId);
                if (bill == null)
                    throw new AppException("会话不存在 recordId:" + model.RecordId.ToString());

                result.RecordId = bill.Id;
                result.Price = bill.Price;
                result.PayPubKey = bill.PayPubKey;

                if (Math.Abs(result.Price) > 0)
                    result.Minutes = bill.PayMaxCoins / Math.Abs(result.Price);
                else
                    result.Minutes = 60 * 24;//无限制

                if (bill.ResId == uid)
                {   //应答者获取记录信息
                    bill.ResIP = _httpcontext.Connection.RemoteIpAddress.ToString();
                    if (bill.Price < 0)
                        bill.PayPubKey = model.ReqPubKey;

                    result.PayPubKey = bill.PayPubKey;

                    _context.TalksRecords.Update(bill);
                    await _context.SaveChangesAsync();
                }
                return result;
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    //请求者刷币
                    int  price = 0;
                    User pay_real = null;
                    User fee_real = null;


                    User usrRes = await _context.Users.AsNoTracking().Include(x => x.pTalks).FirstOrDefaultAsync(t => t.Id == model.PeerId);
                    if (usrRes == null || usrRes.pTalks == null)
                        throw new AppException("对方未开通收费咨询服务 userId:" + model.PeerId.ToString());
                    User usrReq = _context.Users.Include(x => x.pTalks).FirstOrDefault(t => t.Id == uid);
                    if (usrReq == null)
                        throw new AppException("用户不存在 userId:" + uid.ToString());
                    else
                    {
                        price = usrRes.Price;

                        if (price >= 0)
                        {
                            pay_real = usrReq;
                            fee_real = usrRes;
                        }else {
                            pay_real = usrRes;
                            fee_real = usrReq;
                        }
                           

                        if (pay_real.Coins < Math.Abs(price))
                            throw new AppException("账户余额不足以支付1分钟的收费,请充值后再尝试连接!");
                    }

                    bill = new TalksRecord
                    {
                        ReqId = uid,
                        ReqIP = _httpcontext.Connection.RemoteIpAddress.ToString(),
                        PayPubKey = model.ReqPubKey,
                        ResId = model.PeerId,
                        FamilyId = fee_real.FamilyId,
                        PayMaxCoins = pay_real.Coins,
                        TimeRefresh = DateTime.Now,
                        Price = price
                    };
                    _context.TalksRecords.Add(bill);


                    await _context.SaveChangesAsync();


                    //预付话费的id           2021.3.24 ---->放弃预付费方式
                    //await _userService.BalanceChangeAsync(pay_real.Id, (int)CurrencyType.Coins, -lockCoins, "预付话费", bill.Id, (int)AccountChangeType.TalksPrepaid);

                    //返回数据
                    result.RecordId = bill.Id;
                    result.Price = bill.Price;
                    if (bill.Price != 0)
                        result.Minutes = bill.PayMaxCoins / Math.Abs(bill.Price);
                    else
                        result.Minutes = 24 * 60;


                    transaction.Commit();
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    throw new AppException("创建会话失败!"+ e.Message);
                }
            }

            return result;
        }

        //刷新会话，只存储不计算
        public async Task<TalksResponsetModel> TalksRefresh(TalksFinishModel model) {
            TalksResponsetModel result = new TalksResponsetModel { };
            long uid = Convert.ToInt64(_httpcontext.User.Identity.Name);
            string[] sArray = model.Message.Split('_');
            long recordId   = Int64.Parse(sArray[0]);
            int minutes     = Int32.Parse(sArray[1]);


            var bill = await _context.TalksRecords.FirstOrDefaultAsync(t => t.Id == recordId);
            if (bill == null)
                throw new AppException("会话不存在 recordId:" + recordId.ToString());

            long fee_id = 0;
            if (bill.Price >= 0)
                fee_id = bill.ResId;
            else
                fee_id = bill.ReqId;


            //收费方和系统才有资格刷新数据
            if ((uid >= 2000 && uid <= 3000) || uid == fee_id)
            {
                if(model.KeyPub!= bill.PayPubKey)
                    throw new AppException("public key is incorrect:" + model.KeyPub);

                if (bill.Settle != 1)
                {
                    bill.TimeRefresh = DateTime.Now;         //记录时间
                    bill.Minutes = minutes;              //记录收费方已经验证签名的时间数（分钟数）

                    _context.TalksRecords.Update(bill);
                    await _context.SaveChangesAsync();

                    result.RecordId = bill.Id;
                    result.Price = bill.Price;
                    if (bill.Price == 0)
                        result.Minutes = 60 * 24;
                    else
                        result.Minutes = bill.PayMaxCoins / Math.Abs(bill.Price);
                }
                else
                {
                    result.RecordId = -bill.Id;
                    result.Price = bill.Price;
                    result.Minutes = 0;
                }
            }

            return result;
        }


        //add for test
        private static byte[] FromHexString(string hex)
        {
            var numberChars = hex.Length;
            var hexAsBytes = new byte[numberChars / 2];
            for (var i = 0; i < numberChars; i += 2)
                hexAsBytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);

            return hexAsBytes;
        }

        //重要填写验证算法
        private bool VerifySingatue(string pubKey,string message,string signature)
        {
            return true;

            ////using (ECDsaCng dsa = new ECDsaCng())
            ////{
            ////    dsa.HashAlgorithm = CngAlgorithm.Sha256;
            ////    var key = dsa.Key.Export(CngKeyBlobFormat.EccPublicBlob);

            ////    int a = 0;a++;
            ////}


            //pubKey = "03AED487614E43D7205BAAB033E47495C8F10FBB740C116972313079358B5B5A33";
            //var keyPub= FromHexString(pubKey);
            //var data = Encoding.ASCII.GetBytes(message);
            //var signature2 = Convert.FromBase64String(signature);


            //using (ECDsa dsa = ECDsa.Create())
            //{
            //    //Ecc.ImportSubjectPublicKeyInfo();
            //    dsa.ImportSubjectPublicKeyInfo(keyPub, out _);
            //    //dsa.ImportPkcs8PrivateKey(keyPub, out _);
            //    //ECParameters
            //    // dsa.ImportParameters(Pkcs8ToParameters(pubKey));

            //    // the stuff in your current using
            //    var success=dsa.VerifyData(data, signature2, HashAlgorithmName.SHA256);
            //    if (success)
            //    {
            //        Console.WriteLine("Verified");
            //    }
            //    else
            //    {
            //        Console.WriteLine("Failed");
            //    }
            //}


            //CngKey cngKey = CngKey.Import(keyPub, CngKeyBlobFormat.EccPublicBlob);
            //ECDsaCng eCDsaCng = new ECDsaCng(cngKey);

            //bool result = eCDsaCng.VerifyData(data, signature2);

            //var cert = new X509Certificate2(keyPub);
            //var ecdsa = cert.GetECDsaPublicKey();
            //ECDsa Ecc = ECDsa.Create();
            ////var result  = Encoding.ASCII.GetBytes(signature);
            ////ECDsa Ecc = ECDsa.Create();
        }
        public async Task<TalksResponsetModel> TalksEnd(TalksFinishModel model)
        {
            TalksResponsetModel result = new TalksResponsetModel { };

            long uid=0;
            string[] sArray = model.Message.Split('_');
            long recordId = Int64.Parse(sArray[0]);
            int minutes = Int32.Parse(sArray[1]);

            bool sysFix = minutes == 1440 ? true : false;

            if(!sysFix)
                uid = Convert.ToInt64(_httpcontext.User.Identity.Name);



            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var bill = await _context.TalksRecords.FirstOrDefaultAsync(t => t.Id == recordId);
                    if (bill == null)
                        throw new AppException("会话不存在 recordId:" + recordId.ToString());
                    if (bill.Settle == 1)
                        throw new AppException("会话已经完结 recordId:" + recordId.ToString());
                    bool ret = VerifySingatue(bill.PayPubKey, model.Message, model.Signature);// 校验是否签名是否正确
                    if (!ret)
                        throw new AppException("数字签名校验不通过 recordId:" + recordId.ToString());

                    long pay_id = 0;
                    long fee_id = 0;
                    if (bill.Price >= 0)
                    {
                        pay_id = bill.ReqId;
                        fee_id = bill.ResId;
                    }
                    else
                    {
                        pay_id = bill.ResId;
                        fee_id = bill.ReqId;
                    }

                    //只有系统和收费方有资格结算 
                    if (!sysFix && (uid != fee_id||(uid < 2000 && uid > 3000)))
                        return result;


                     //计算会话时间
                     bill.TimeEnd = sysFix ? bill.TimeRefresh : DateTime.Now;

                    TimeSpan ts = bill.TimeEnd - bill.TimeCreate;
                    int mins = (int)ts.TotalMinutes;
                    mins = Math.Min(mins, minutes);  //取最小值
                    bill.Minutes = mins;

                    //计算实际花费
                    int spending = Math.Abs(mins * bill.Price);
                    if (bill.Price > 0 && spending > bill.PayMaxCoins)
                        spending = bill.PayMaxCoins;

                    //用户扣费
                   int retCoins=await _userService.BalanceChangeAsync(pay_id, (int)CurrencyType.Coins, -spending, "语音付费", bill.Id, (int)AccountChangeType.TalksPaid);
                    if (retCoins <= 0)
                        spending -= retCoins;

                    bill.Beans = spending;


                    //计算家族收益//分割赚取的金豆
                    var f = await _context.Familys.SingleOrDefaultAsync(x => x.Id == bill.FamilyId);
                    if (f != null && spending > 0)
                    {
                        int fIncome = (int)(spending * f.TalksFee);
                        spending -= fIncome;
                        if (spending >= 0)
                        {
                            f.Beans += fIncome;
                            //家族收益明细
                            FamilyIncome fDetail = new FamilyIncome
                            {
                                FamilyId = bill.FamilyId,
                                AnchorId = fee_id,
                                Beans = fIncome,
                                Remarks = "咨询贡献",
                                Type = (int)AccountChangeType.TalksDevote
                            };
                            _context.FamilyIncomes.Add(fDetail);
                        }
                    }
                    if (spending >= 0)
                        await _userService.BalanceChangeAsync(fee_id, (int)CurrencyType.Beans, spending, "赚取话费", bill.Id, (int)AccountChangeType.TalksCharge);



                    //最后的分钟数和最后的签名锁定交易的合法性   recordId_FinalMinutes  签名 FinalSignature
                    bill.FinalMinutes = minutes;
                    bill.Settle = (sysFix ? 2:1);               //系统修正还是收费确认
                    _context.TalksRecords.Update(bill);
                    await _context.SaveChangesAsync();

                    result.RecordId = -bill.Id;

                    transaction.Commit();
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    throw new AppException("创建会话失败!" + e.Message);
                }
            }

            return result;
        }


        private async Task FixLostReport(long uid)
        {

            var today = DateTime.Now.AddMinutes(-5);

            long[] bills = await _context.TalksRecords.Where(t => t.Settle == 0 && (t.ReqId == uid || t.ResId == uid) && today > t.TimeRefresh)
                                                .Select(g => g.Id).ToArrayAsync();

            foreach (var rid in bills)
            {
                TalksFinishModel model=new TalksFinishModel { };
                model.Message= rid.ToString() + "_" + "1440";       //模拟用户签名

                model.Signature = "";

                var result = await TalksEnd(model);

                //Console.WriteLine(num);
            }

        }

        public async Task TalksTimeFix()
        {
            var today = DateTime.Now.AddMinutes(-5);

            long[] bills = await _context.TalksRecords.Where(t => t.Settle == 0 && today > t.TimeRefresh)
                                                .Select(g => g.Id).ToArrayAsync();

            foreach (var rid in bills)
            {
                TalksFinishModel model = new TalksFinishModel { };
                model.Message = rid.ToString() + "_" + "1440";       //模拟用户签名
                model.Signature = "";

                var result = await TalksEnd(model);
            }
        }

        //
        public async Task GameResultFix()
        {
            int minutes = _config.GetValue<int>("Timer:MahjongFixMinutes");

            var today = DateTime.Now.AddMinutes(-minutes);

            long[] records = await _context.MJRecords.Where(t => t.Settle == 0 && today > t.CreateTime)
                                                .Select(g => g.Id).ToArrayAsync();

            if (records.Count() < 1)
                return ;

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    foreach (var rid in records)
                    {

                        var record = await _context.MJRecords.Include(x => x.Gambles).FirstOrDefaultAsync(t => t.Id == rid);

                        if (record.Gambles != null && record.Chips>0)
                        {
                            foreach (var g in record.Gambles)
                            {
                                await _userService.BalanceChangeAsync(g.UserId, record.Symbol, record.Chips, "退还筹码", record.Id, (int)AccountChangeType.SystemReturn, "系统故障-自动恢复");
                            }

                            record.Settle = -1;
                            _context.MJRecords.Update(record);
                            await _context.SaveChangesAsync();
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
        }
         

        //查看用户的 聊天/咨询 收费明细
        public async Task<dynamic> GetTalksChargeDetails(long uid, bool income)
        {
            //首先修正数据 用户没有及时上报导致会谈未关闭
            await FixLostReport(uid);
            //返回
            if (income)
            {
                var query = await (from a in _context.TalksRecords
                             where a.Settle >= 1 && (a.Price>0?a.ResId ==uid:a.ReqId == uid)
                             orderby a.TimeCreate descending
                             select new 
                             {
                                 Id = a.Id,
                                 peer = (a.Price > 0 ? a.ReqId  : a.ResId ),
                                 timeStart = a.TimeCreate.ToString("MM-dd HH:mm"), //"yyyy-MM-dd HH:mm:ss"
                                 timeEnd = a.TimeEnd.ToString("MM-dd HH:mm"), //"yyyy-MM-dd HH:mm:ss"
                                 price = a.Price,
                                 amount = a.Beans
                             }).Take(10).ToListAsync();

                return query;
            }
            else {
                var query = await  (from a in _context.TalksRecords
                             where a.Settle >= 1 && (a.Price > 0 ? a.ReqId == uid : a.ResId == uid)
                             orderby a.TimeCreate descending
                             select new 
                             {
                                 Id = a.Id,
                                 peer = (a.Price > 0 ? a.ResId : a.ReqId),
                                 timeStart = a.TimeCreate.ToString("MM-dd HH:mm"), //"yyyy-MM-dd HH:mm:ss"
                                 timeEnd = a.TimeEnd.ToString("MM-dd HH:mm"), //"yyyy-MM-dd HH:mm:ss"
                                 price = a.Price,
                                 amount = a.Beans
                             }).Take(10).ToListAsync();

                return query;
            }
        }


    }
}
