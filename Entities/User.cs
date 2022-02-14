using System;
using System.Globalization;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace XAccount.Entities
{
    public class User
    {
        [Key]
        public long     Id { get; set; }

        //靓号
        public long     PrettyId { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string   Mood { get; set; }
        [Column(TypeName = "varchar(20)")]
        public string   Name { get; set; }
        public int      Sex { get; set; } =-1;

        [IgnoreDataMember]
        public string   Password { get; set; } = "";  // Not visible in the EDM

        [Column(TypeName = "varchar(20)")]
        public string   Avatar { get; set; } = "def";
        public DateTime Birthday { get; set; } = DateTime.Parse("2000-1-1");

        [Column(TypeName = "varchar(20)")]
        public string Career { get; set; } = "";                                        //职业
        public string Location { get; set; } = "-122.131577 47.678581";

        /*
         *     lat = x.Latitude,
               lng = x.Longitude,
         */
        public int Price { get; set; } = 0;


        //账户信息
        public int Coins { get; set; } = 0;                                     //金币
        public int Beans { get; set; } = 0;                                     //金豆
        public int Scores { get; set;} = 0;                                     //积分    由家族控制切换家族后清0

        //组织
        public long FamilyId { get; set; } = 0;


        public int Online { get; set; } = 0;
        public int Closed { get; set; } = 0;                                    //封号

        //隐私信息
        [IgnoreDataMember]
        public DateTime CreateTime { get; set; } = DateTime.Now;                //创建日期
        [IgnoreDataMember]
        public string   RegIp { get; set; }
        [IgnoreDataMember]
        public long     Phone { get; set; }

        [IgnoreDataMember]
        public string Token { get; set; }


        //开通的功能
        //public int      AsExpert { get; set; } = 0;                           //开通知识付费赚钱功能

        [Column(TypeName = "varchar(256)")]
        public string Desp { get; set; } = "";                                  //个人描述


       


        //标签信息
        public ICollection<UserTag>        Labels{ get; set; }                  //1vn

        //扩展信息
        public virtual Mahjong      pGame    { get; set; }                      //游戏状态
        //开通知识付费    赚钱功能
        public virtual ProfileTalks pTalks   { get; set; }                      //付费聊天状态

        public virtual AgentInfo    pAgent  { get; set; }


        public virtual Marriage     pMarriage { get; set; }                    //征婚信息
    }


    //世界
    public class World
    {
        [Key]
        public long Id { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string Name { get; set;}


        //public string Poster { get; set; }                                    //海报 直接通过图片目录关联
        //[Column(TypeName = "varchar(20)")]
        //public string Picture { get; set; }

        [Column(TypeName = "varchar(128)")]
        public string Desp { get; set; } = "";


        [Column(TypeName = "varchar(64)")]
        public string GameServer { get; set; } = "";


        //数字 服务器id号
        [Column(TypeName = "varchar(64)")]
        public string AvServer { get; set; } = "";

        public string Path { get; set; } = "#";


        //付费信息
        public int Ticket { get; set; } =0;                                     //门票

        public int MaxPlayers { get; set; } = 20;                               //最大人数


        public User Creator { get; set; }                                       //创建者
        public DateTime CreateTime { get; set; } = DateTime.Now;                //创建日期


        public int Status { get; set; } = 1;                                     //世界状态 0 关闭 1 公开 2 私密 3 密码
        //世界密码
        [Column(TypeName = "varchar(128)")]
        public string Password { get; set; } = "";                              //凭密码进入

        //成员信息
        public ICollection<WorldUser>  Members { get; set; }                    //1vn

        //人数，最大人数，当前人数 //从服务器获得


        public int DisplayOrder { get; set; } = 1;                              //排序
    }

    //用户标签
    public class WorldUser
    {
        [ScaffoldColumn(false)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [ForeignKey("World")]
        public long WorldID { get; set; }
        public World Tag { get; set; }


        [ForeignKey("User")]
        public long UserID { get; set; }
        public User User { get; set; }
    }



    //标签
    public class Tag
    {
        [ScaffoldColumn(false)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Name { get; set; } = "";
        public string Desp { get; set; } = "";
        public int UserCounts { get; set; } = 0;

        public string Color { get; set; } = "blue";
        public int OutLine { get; set; } = 0;


        public int ClickCount { get; set; } = 0;

        public int DisplayOrder { get; set; } = 0;

        public int Audit { get; set; } = 0;     //审核通过


        public virtual List<UserTag> UserTags { get; set; }
    }
    //用户标签
    public class UserTag
    {
        [ScaffoldColumn(false)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [ForeignKey("Tag")]
        public long TagID { get; set; }
        public Tag Tag { get; set; }


        [ForeignKey("User")]
        public long UserID { get; set; }
        public User User { get; set; }
    }


    //1 v 1 
    //代理人信息  //账户有保证金才能拥有代理资格
    public class AgentInfo
    {
        [Key]
        [ForeignKey("AgentOf")]
        public long      UserId { get; set; }

        public string    Address { get; set; }
        public int       Margin  { get; set; }
        public int       CreditAmount { get; set; }
        public string    SNWechat { get; set; }
        public string    SNAlipay { get; set; }
        public string    SNQQ    { get; set; }

        public byte[]    IDCardFront { get; set; }
        public byte[]    IDCardBack { get; set; }
        public DateTime  CreateTime { get; set; } = DateTime.Now;
        public int       Approve { get; set; } = 0;

        public User      AgentOf { get; set; }
    }
    //1V1 婚恋信息
    public class Marriage
    {
        [Key]
        [ForeignKey("MarriageOf")]
        public long UserId { get; set; }

        //自然
        public int Height { get; set; }
        public int Weight { get; set; }
        [Column(TypeName = "varchar(10)")]
        public string Education { get; set; }                                   //学历

        //经济
        public int      Salary { get; set; }                                    //月收入
        [Column(TypeName = "varchar(20)")]
        public string   House { get; set; }                                     //房屋
        [Column(TypeName = "varchar(20)")]
        public string   Cars { get; set; }                                      //汽车

        //社会
        [Column(TypeName = "varchar(20)")]
        public string AddrWork { get; set; }                                    //工作地
        [Column(TypeName = "varchar(20)")]
        public string AddrHome { get; set; }                                    //户口地
        [Column(TypeName = "varchar(10)")]
        public string Status {get; set; }                                       //婚姻状态
        [Column(TypeName = "varchar(100)")]
        public string Desp { get; set; }                                        //详细描述
        [Column(TypeName = "varchar(100)")]
        public string Expect { get; set; }                                      //择偶条件  期望
        [Column(TypeName = "varchar(100)")]
        public string Future { get; set; }                                      //愿景描述

        public User MarriageOf { get; set; }
    }

    //知识付费 咨询
    public class ProfileTalks
    {
        [Key]
        [ForeignKey("ProfileOf")]
        public long UserId { get; set; }                                        //用户id

        //统计
        public long TotalCoins { get; set; } = 0;                               //总消费 由触发器产生
        public long TotalBeans { get; set; } = 0;                               //总收入  
        public int  LevelCoins { get; set; } = 0;                               //消费级别
        public int  LevelBeans { get; set; } = 0;                               //收入级别

        public long TalkingId { get; set; } = 0;                                //最后一次会话id
        public int  DisplayOrder { get; set; } = 1;                             //显示顺序

        public User ProfileOf { get; set; }
    }


    //用户账户变化
    public class UserBalanceChange
    {
        [Key]
        public long         Id { get; set; }                                    //记录id
        public long         UserId { get; set; }

        public int          Currency { get; set; } = 0;                         //coins=1 beans=2 scores=3
        public DateTime     CreateTime { get; set; } = DateTime.Now;            //创建日期
        [MaxLength(20)]
        public string       Summary { get; set; }                               //摘要

        public int          Amount { get; set; } = 0;                           //金额
        public int          Balance { get; set; } = 0;                          //余额

        //关联信息
        public long         RecordId { get; set; } = 0;                         //记录id
        public int          Type { get; set; } = 0;
        [MaxLength(50)]
        public string       Remark { get; set; }                                //备注
    }


    //在途金币，金币玩家将金币转移给其他玩家，在指定时间内可以撤回决定
    public class CoinsInTransit{
        public long     BuyerId { get; set; }
        public long     SellerId { get; set; }
        public int      Amount { get; set; } = 0;                               //在途金币数量


        public int      State { get; set; } = -1;                               //状态 0在途 1 完
        public DateTime CreateTime { get; set; } = DateTime.Now;
        //增加担保交易字段
        //Arbitration
        public int ArbSeller { get; set; } = 0;                                 //销售方 申请仲裁 
        public int ArbPurchaser { get; set; } = 0;                              //购买方 申请仲裁 
}

    //游戏麻将
    public class Mahjong
    {
        [Key]
        [ForeignKey("User")]
        public long UserId { get; set; }                                        //用户id


       //public int Score { get; set; } = 0;                                    //积分

        public int Win { get; set; } = 0;                                       //胜
        public int Draw { get; set; } = 0;                                      //平
        public int Lose { get; set; } = 0;                                      //负
        public int GameType { get; set; } = 0;                                  //游戏种类  80 麻将 82斗地主
    }

    /*
 *  第一期直接扣分 
 */

    public class MahjongRecord
    {
        [Key]
        public long         Id { get; set; }
        public int          Symbol { get; set; } = 0;                                           //结算货币类型
        public int          Chips { get; set; } = 0;                                            //本局筹码/每人
        public long         FamilyId { get; set; } = 0;
        public int          PlayerNumber { get; set; } = 2;
        public float        TableFee { get; set; } = 0;                                         //本局茶费
        public              List<Gamble> Gambles { get; set; }
        public              DateTime CreateTime { get; set; } = DateTime.Now;
        public              DateTime FinishedTime { get; set; } = DateTime.Now;
        public              int Brokerage { get; set; } = 0;                                    //系统佣金
        public              int Settle{ get; set; } = 0;                                        //是否结算完成
        public              string Result { get; set; } = "";                                   //结果
        public              string Desp { get; set; } = "";                                     //规则描述文件
    }

    public class Desk
    {
        [Key]
        public long     Id { get; set; }                                        //不用
        public int      players { get; set; } = 3;                              //玩家人数
        public string   rule { get; set; } = "ka5star";                         //规则
        public string   symbol { get; set; } = "SCR";                           //筹码=积分
        public int      excess { get; set; } = 100;                             //底分
        public int      spawn { get; set; } = 8;                                //封顶    
        public int      points { get; set; } = 8;                               //最大番数
        public float    brokerage { get; set; } = 25;                           //佣金
        public int      buyhorse { get; set; } = 0;                             //买马

        public int      total { get; set; } = 5;                                //
        public long     familyID { get; set; }                                  //所属家族
    }

    public class Gamble
    {
        [Key]
        [DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }


        public long UserId { get; set; }
        public int  Chip { get; set; } = 0;                  //筹码

        //只记录原始数据
        public int Piao { get; set; } = 0;                  //漂分
        public int Gang { get; set; } = 0;                  //杠分
        public int Hu { get; set; } = 0;                    //胡牌
        public int Broke { get; set; } = 0;                 //茶费
    }


    public class TalksRecord
    {
        [Key]
        public long Id { get; set; }                                            //记录id      
                                                                                //财务数据
                                                                                //coins 变化
        public long     ReqId { get; set; }                                     //请求方id 
        public long     ResId { get; set; }                                     //应答方id
        public int      Price { get; set; } = 0;                                //聊天单价

        public string   PayPubKey { get; set; } = "";                           //付款key
        public int      PayMaxCoins { get; set; } = 0;                          //付费方当时的最大金币数

        public string   ReqIP { get; set; } = "";                               //请求放ip
        public string   ResIP { get; set; } = "";                               //应答方ip



        //beans 变化
        //收费方id       //金豆发生变化的账户
        public int      Beans { get; set; } = 0;                                //产生的金豆


        //预备删除
        public long FamilyId { get; set; } = 0;                                 //家族


        //聊天相关数据
        public int      Minutes { get; set; } = 0;                              //刷新的时间 收费方每5分钟刷新一次

        //收费方上报的最终分钟数和付费方最终的签名 用来验证记录合法性
        public int FinalMinutes { get; set; } = 0;                              //最终刷新的时间
        public int ByteProduce  { get; set; } = 0;                              //产生的字节
        public int ByteSkip     { get; set; } = 0;                              //丢失的字节


        public DateTime TimeCreate  { get; set; } = DateTime.Now;               //开始时间
        public DateTime TimeRefresh { get; set; } = DateTime.Now;               //刷新的最新时间
        public DateTime TimeEnd     { get; set; } = DateTime.Now;               //结束时间     收款人决定最终时间

        public int      Settle { get; set; } = 0;                               //过程结束 0 默认开始    1 完成 2 机器审核通过  -1 审核异常
    }



    //家族收入明细        金币金豆收入
    public class FamilyIncome
    {
        [Key]
        public long Id { get; set; }

        public long FamilyId { get; set; } = 0;
        public long AnchorId { get; set; } = 0;


        public int Beans { get; set; } = 0;
        public int BeansBill { get; set; } = 0;

        [MaxLength(50)]
        public string Remarks { get; set; }                                   //说明
        public int Type { get; set; } = 0;
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }

    //family
    public class Family
    {
        [Key]
        public long     Id { get; set; }

        public int      Type { get; set; } = 1;

        public long     MasterId { get; set; }                                  //家族长id
        public string   Name { get; set; }                                      //名称
        [MaxLength(100)]
        public string   Desp { get; set; }                                      //描述
        //public string   Poster { get; set; }                                  //海报
        //设置
        public int      Tickets { get; set; } = 0;                              //门票
        public int      JoinAward { get; set; } = 0;                             //加入奖励
        public float    TeaFee { get; set; } = 0;                               //茶费，小数为提成，整数为人均收费
        public float    TalksFee { get; set; } = 0;                             //语聊 分成
        public float    TransferFee { get; set; } = 0;                           //积分转账手续费
        //财务数据
        public int      Scores { get; set; } = 0;                               //家族积分
        public int      Beans { get; set; } = 0;                                //家族收益           
        public float    BeanRate { get; set; } = 0.5F;                          //家族提成

        public int      DisplayOrder { get; set; } = 1;                         //显示权重
        public DateTime CreateTime { get; set; } = DateTime.Now;                //创建日期
    }



    /// <summary>
    /// 订单
    /// </summary>
    public class OrderCoin
    {
        [Key]
        public long Id { get; set; }                        //外部订单号，商户网站订单系统中唯一的订单号

        public long UserId { get; set; }                    //购买者id


        public string Subject { get; set; }                 //订单名称
        public double Amount { get; set; }                  //付款金额
        public string Desp { get; set; }                    //商品描述

        public int Coins { get; set; }                      //获得的金币数量

        //支付信息
        public string TradeNo { get; set; }
        public string BuyerId { get; set; }
        public int Sucess { get; set; } = 0;                //支付是否成功  0失败  1 成功
                                                            //创建日期
        public DateTime CreateTime { get; set; } = DateTime.Now;

        public int Status { get; set; } = 0;
        /// <summary>
        /// 支付提供商  1 阿里 2 微信  
        /// </summary>
        /// <value>The provider.</value>
		public int Provider { get; set; } = 0;

    }


    public class WithDraw { 
        [Key]
        public long Id { get; set; }

        public long UserId { get; set; }

        public int Beans { get; set; }                                  //金豆数量


        public string BankAccount { get; set; } = "";                   //银行账号
        public string BankUserName { get; set; } = "";                  //银行用户名

        public string BankName { get; set; } = "";                      //银行名称
        public string BankAddress { get; set; } = "";                   //分行地址

        public double Money { get; set; } = 0;                          //提现金额

        public int TransferStep { get; set; } = 0;                      //体现审核步骤

        public DateTime CreateTime { get; set; } = DateTime.Now;        //创建时间

    }


}