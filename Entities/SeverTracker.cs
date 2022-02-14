using System;

using System.Globalization;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Runtime.Serialization;


namespace XAccount.Entities
{
    public class SeverTracker
    {
        [Key]
        public long     Id { get; set; }

        public string   Password { get; set; }                                  //tracker 登录db系统密码
        public string   Location { get; set; }
        public string   IPPort { get; set; }
        public int      Online { get; set; }                                    //在线状态
        public int      UsersCurrent { get; set; }                              //当前在线用户
        public int      UsersMax { get; set; }                                  //最大在线用户

        //资源利用率
        public float UsageCpu { get; set;}
        public float UsageNetWork { get; set;}
        public float UsageDisk { get; set; }

    }
}
