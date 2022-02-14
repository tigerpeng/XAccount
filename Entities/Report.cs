using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace XAccount.Entities
{
    //多字段做主键 必须在 OnModelCreating 声明
    public class ReportNetwork
    {
        //双主键或多主个键
        [Key, Column(Order = 1)]
        public int Record { get; set; }

        [Key, Column(Order = 1)]
        public int SN{ get; set; }

        public string   ProduceV { get; set; } = "";                  //每个字段存300个数据
        public string   LostV { get; set; } = "";
        public string   BufferV { get; set; } = "";                   //当时的BUFFER 状态 调试性能用
        public string   ReceiveV { get; set; } = "";

        public string   ProduceA { get; set; } = "";
        public string   LostA { get; set; } = "";
        public string   BufferA { get; set; } = "";
        public string   ReceiveA { get; set; } = "";
    }



    //在途资金  代理给用户充值 代理确认 可以取消 用户可以申述冻结
    public class OnTheWayCoin {
        [Key]
        public long Id { get; set; }


        public long UserId { get; set; }
        public long AgentId { get; set; }

        public int Amount { get; set; } = 0;

        public DateTime CreateTime { get; set; } = DateTime.Now;

        public DateTime AgentTime { get; set; }  = DateTime.Now;
        public DateTime UserTime { get; set; }   = DateTime.Now;
        public DateTime SystemTime {get;set; }   = DateTime.Now;

        public string Remarks { get; set; }
        public int Status { get; set; } = 0;            //1 在途
                                                        //2 代理确认       -2   代理撤回 
                                                        //3 用户申述
                                                        //4 系统冻结        -1   已经解决
    }
}