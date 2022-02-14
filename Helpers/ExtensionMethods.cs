using System.Collections.Generic;
using System.Linq;
using XAccount.Entities;

namespace XAccount.Helpers
{
    public static class ExtensionMethods
    {
        public static IEnumerable<User> WithoutPasswords(this IEnumerable<User> users) {
            return users.Select(x => x.WithoutPassword());
        }

        public static User WithoutPassword(this User user) {
            user.Password = null;
            return user;
        }
    }

    enum CurrencyType
    {
        Coins = 1
        ,Beans=2
        ,Scores=3
    }

    enum AccountChangeType
    {
      System = 1                    //官方赠送
    , SystemBuyFamily
    , TalksPrepaid=10               //预付话费
    , TalksPaid                     //支付付话费
    , TalksReturn                   //返还
    , TalksConsume                  //聊天消费
    , TalksCharge                   //聊天赚钱
    , TalksDevote                   //聊天贡献

    , ChipsPawn = 20                //筹码抵押
    , ChipsReturn                   //筹码返还


    , Recharge              //充值所得
    , RechargeGift          //充值赠送 自己
    , ActivityGift          //活动赠送
    , Talks                 //聊天 消耗 , 收益
    , Invited               //邀请 奖励
    , TalksGift             //他人聊天  我提成 邀请 收益-赚
    , RechargeTaGift        //他人充值  我提成 邀请 收益-冲
    , Exchange              //金币金豆互换
    , AddCoins              //系统加币
    , AddBeans              //系统加金豆

    , RewardRegister            //奖励用户注册
    , RewardCompleteMaterial    //奖励完善资料
    , RewardBuyCoin             //促销奖励金币

    , WithDraw           //提现 11
    , FamilyBeanCovert   //家族 提取金豆到用户账号

    ,SystemReturn               //系统故障返还
    ,LeaveFamily                //离开家族
    ,ScoreUp                    //上分
    ,ScoreDown                  //下分
    ,ScoreTransfer              //积分转账
    }
}