using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace XAccount.Models
{
    public class UserOnlineModel
    {
        public long Uid { get; set; }
        public int  Online { get; set; }
    }

    public class UserProfileModel {
        public long     Id { get; set; }
        public string   Name { get; set; }
        public string   Avatar { get; set; }
        public int      Coins { get; set; }
        public int      Beans { get; set; }
        public int      Scores { get; set;}

        //game
        public int Win { get; set; } = 10;
        public int Draw { get; set; } = 0;
        public int Lose { get; set; } = 10;
    }

    //移动到mahjong 文件
    public class MjProfileModel
    {
        public long Id { get; set; }
        public string Name { get; set; } = "匿名用户";
        public string Avatar { get; set; }

        public int Score { get; set; } = 0;
        public int Win { get; set; } = 10;
        public int Draw { get; set; } = 0;
        public int Lose { get; set; } = 10;
    }

    public class MJRecordModel
    {
        public long Id { get; set; }
        public List<long> Poors { get; set; }           //筹码不足的用户
    }

    public class TransferModel {
        public  long    To { get; set; }
        public  int     Amount { get; set; }
        public  int     Type { get; set; } = 0;
        public  string  Remarks { get; set; }
    }

    public class FamilyScoreInfoModel
    {
        public int BalanceFamily { get; set; } = 0;
        public int BalanceUser { get; set; } = 0;
    }

    public class TransferConfirmModel
    {
        public long RecordId { get; set; }
        public int  Action { get; set; }
    }

    /*
    //付费咨询
    public class TalksActorModel {
        public long     Uid { get; set; }
        public string   Name { get; set; }
        public string   Avatar { get; set; }
        public int      Sex { get; set; }
        public string   Birthday { get; set; }
        public int      Price { get; set; }
        public string   Desp { get; set; }
        public string   Location { get; set; }
        public int      Online { get; set; }
        public ICollection<string> Labels { get; set; }
    }
    */

    public class TalksRequestModel {
        public long     RecordId { get; set; }
        public long     PeerId  { get; set; }
        public string   ReqPubKey { get; set; }
    }
    //付费咨询返回信息
    public class TalksResponsetModel
    {
        public long RecordId { get; set; }
        public int  Price { get; set; }
        public int  Minutes { get; set; }
        public string PayPubKey { get; set; }
    }

    public class TalksFinishModel
    {
        public string   KeyPub { get; set; }
        public string   Message { get; set; }
        public string   Signature { get; set; }
    }


    //family
    public class FamilyModel {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Picture { get; set; }
        public string Desp { get; set; }
        public string CreateTime { get; set; }
    }

    public class WithDrawModel {

        public int Beans { get; set; }                                  //金豆数量
        public string BankAccount { get; set; } = "";                   //银行账号
        public string BankUserName { get; set; } = "";                  //银行用户名
        public string BankName { get; set; } = "";                      //银行名称
        public string BankAddress { get; set; } = "";                   //分行地址
    }
    
}
