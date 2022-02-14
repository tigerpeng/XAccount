using System;
using Microsoft.EntityFrameworkCore;
using XAccount.Entities;


namespace XAccount.Data
{
    public class DataContext:DbContext
    {
        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //约束
            modelBuilder.Entity<User>()
                        .Property(x => x.Name)
                        .HasMaxLength(20);

            modelBuilder.Entity<Family>()
                        .Property(x => x.Name).HasMaxLength(20);

            //多字段做主键 必须在这里声明
            modelBuilder.Entity<ReportNetwork>()
                        .HasKey(e => new { e.Record, e.SN });


            ////  Do：一对一关系模型
            //modelBuilder.Entity<TalksProfile>().HasOne(l => l.User).WithOne(l => l.pTalks)
            //    .HasForeignKey<TalksProfile>(l => l.UserId);



            //演示数据
            modelBuilder.Entity<User>()
                .HasData(
                new User { Id = 1, Name = "系统", Phone = 1 ,FamilyId=1, Coins = 1000000 },
                new User { Id = 2020, Name = "tracker服务器#1",Career = "password:12345", Price = 0, Sex = 0, Phone = 2020, Coins = 0, Online = 1 },
                new User { Id = 10001, Name = "浅笑",   Career ="小学老师", Price = 100,Sex =0,     Phone = 10001 , Coins = 1000000, Online = 1},
                new User { Id = 10002, Name = "糖糖",   Career = "幼师", Price = 100, Sex = 0,      Phone = 10002 , Coins = 1000000, Online = 1},
                new User { Id = 10003, Name = "小青", Career = "设计师", Price = 100, Sex =0,Phone = 10003 , Coins = 1000000, Online = 1},
                new User { Id = 10004, Name = "何小仙儿",   Career = "无业", Price = 0, Sex = 0, Phone = 10004 , Coins = 1000000, Online = 1},
                new User { Id = 10005, Name = "喵喵",   Career = "律师", Price = 0, Sex = 0, Phone = 10005 , Coins = 1000000, Online =1},
                new User { Id = 10006, Name = "詹大卫",  Career = "软件工程师", Price = 0, Sex = 1, Phone = 10006 , Coins = 1000000, Online = 1},
                new User { Id = 10007, Name = "晨哥", Career = "心理咨询师", Price = 0, Sex =0, Phone = 10007 , Coins = 1000000, Online = 1},
                new User { Id = 10008, Name = "娇气的小奶气包",   Career = "幼师", Price = 100, Sex =0,       Phone = 10008 , Coins = 1000000, Online =1},
                new User { Id = 10009, Name = "桃桃",   Career = "公务员", Price = 100, Sex = 1,     Phone = 10009 , Coins = 1000000, Online = 1,},
                new User { Id = 10010, Name = "华哥", Career = "工人", Price = 0, Sex = 1,          Phone = 10010 , Coins = 1000000, Online = 1},
                new User { Id = 10011, Name = "711社区超市", Career = "商家", Price = -100, Desp ="每人每天限参观5分钟,5分钟后自动掉线，谢谢", Phone = 10010, Coins = 1000000, Online = 1},
                new User { Id = 10012, Name = "张永生水果店", Career = "商家", Price = -200, Desp = "羊毛不多,每周10分钟 5公里内", Phone = 10010, Coins = 1000000, Online = 1},
                new User { Id = 10013, Name = "花漫里棋牌室", Career = "商家", Price = -160, Desp = "欢迎大家来我店交友娱乐 限本小区 1公里内", Phone = 10010, Coins = 1000000, Online = 1},
                new User { Id = 10014, Name = "23分炸鸡汉堡店", Career = "商家", Price = -78, Desp = "23分钟送到，欢迎光临，限每月1小时 5公里范围", Phone = 10010, Coins = 1000000, Online = 1},
                new User { Id = 1512100, Name = "第一个用户", Phone = 1234567});


            ///世界
            modelBuilder.Entity<World>().HasData(
                new World { Id = 1, Name = "元宇宙空间测试", GameServer= "game.ournet.club:2345", AvServer = "2020",Desp="公开测试"},
                new World { Id = 2, Name = "本地测试", GameServer = "192.168.0.100:2345", AvServer = "2020", Desp = "本地局域网测试" },
                new World { Id = 3, Name = "北上广-中高端相亲", GameServer = "game.ournet.club:2345", AvServer = "2020", Desp = "相亲世界,尚未开放" },
                new World { Id = 101, Name = "卡五星", GameServer = "game.ournet.club:2345", AvServer = "2020",Desp = "多人棋牌游戏,尚未开放" }
                );

            modelBuilder.Entity<WorldUser>().HasData(
                    new WorldUser { Id = 1, WorldID = 3, UserID = 10001 },
                    new WorldUser { Id = 2, WorldID = 3, UserID = 10002 },
                    new WorldUser { Id = 3, WorldID = 3, UserID = 10003 },
                    new WorldUser { Id = 4, WorldID = 3, UserID = 10004 },
                    new WorldUser { Id = 5, WorldID = 3, UserID = 10005 }
                );



            ////标签
            modelBuilder.Entity<ProfileTalks>().HasData(
                new ProfileTalks { UserId = 10001},
                new ProfileTalks { UserId = 10002},
                new ProfileTalks { UserId = 10003},
                new ProfileTalks { UserId = 10004},
                new ProfileTalks { UserId = 10005},
                new ProfileTalks { UserId = 10006},
                new ProfileTalks { UserId = 10007},
                new ProfileTalks { UserId = 10008},
                new ProfileTalks { UserId = 10009},
                new ProfileTalks { UserId = 10010},
                new ProfileTalks { UserId = 10011},
                new ProfileTalks { UserId = 10012},
                new ProfileTalks { UserId = 10013},
                new ProfileTalks { UserId = 10014});

            //标签
            modelBuilder.Entity<Tag>().HasData(
                //new Tag { Id = 1, Name = "全部", Color = "green", OutLine = 0, Audit = 1, Desp = "恋爱/婚姻/家庭" },
                //new Tag { Id = 2, Name = "免费", Color = "red", OutLine = 0, Audit = 1,   Desp = "恋爱/婚姻/家庭" },
                //new Tag { Id = 3, Name = "收费", Color = "green", OutLine = 0, Audit = 1, Desp = "恋爱/婚姻/家庭" },
                //new Tag { Id = 4, Name = "赚钱", Color = "green", OutLine = 0, Audit = 1, Desp = "恋爱/婚姻/家庭" },
                new Tag { Id = 5, Name = "帅哥", Color = "green", OutLine = 1, Audit = 1, Desp = "恋爱/婚姻/家庭" },
                new Tag { Id = 6, Name = "美女", Color = "red", OutLine = 1, Audit = 1, Desp = "恋爱/婚姻/家庭" },

                new Tag { Id = 101, Name = "婚恋相亲", Color = "green", Audit =1, Desp = "恋爱/婚姻/家庭" },
                new Tag { Id = 102, Name = "红娘", Color = "green",    Audit = 1, Desp = "婚恋类" },
                new Tag { Id = 103, Name = "心理咨询", Color = "green", Audit = 1, Desp = "咨询类" },
                new Tag { Id = 104, Name = "家教辅导", Color = "green", Audit = 1, Desp = "教育类" },
                new Tag { Id = 105, Name = "装修设计", Color = "green", Audit = 1, Desp = "装修建材工人" },
                new Tag { Id = 106, Name = "棋牌游戏", Color = "green", Audit = 1, Desp = "在线小游戏" });

            modelBuilder.Entity<UserTag>().HasData(
                new UserTag {Id=1, UserID = 10001, TagID = 101 },
                new UserTag {Id=2, UserID = 10001, TagID = 102 },
                new UserTag {Id=3, UserID = 10002, TagID = 101 },
                new UserTag {Id=4, UserID = 10002, TagID = 103 },
                new UserTag {Id=5, UserID = 10003, TagID = 104 },
                new UserTag {Id=6, UserID = 10003, TagID = 105 },
                new UserTag {Id=7, UserID = 10004, TagID = 106 },
                new UserTag {Id=8, UserID = 10004, TagID = 101 },
                new UserTag {Id=9, UserID = 10005, TagID = 102 },
                new UserTag {Id=10,UserID = 10005, TagID = 103 },
                new UserTag {Id=11,UserID = 10006, TagID = 104 },
                new UserTag {Id=12,UserID = 10006, TagID = 105 },
                new UserTag {Id=13,UserID = 10007, TagID = 106 },
                new UserTag {Id=14,UserID = 10007, TagID = 103 },
                new UserTag {Id=15,UserID = 10008, TagID = 104 },
                new UserTag {Id=16,UserID = 10008, TagID = 105 },
                new UserTag {Id=17,UserID = 10009, TagID = 101 },
                new UserTag {Id=18,UserID = 10010, TagID = 106 });

            modelBuilder.Entity<OnTheWayCoin>().HasData(
                new OnTheWayCoin { Id = 1, UserId = 10002,AgentId = 10001, CreateTime = DateTime.Now, Status = -2 },
                new OnTheWayCoin { Id = 2, UserId = 10002,AgentId = 10001, CreateTime = DateTime.Now, Status = 1 },
                new OnTheWayCoin { Id = 3, UserId = 10003,AgentId = 10002, CreateTime = DateTime.Now, Status = 1 },
                new OnTheWayCoin { Id = 4, UserId = 10003,AgentId = 10002, CreateTime = DateTime.Now, Status = 1 },
                new OnTheWayCoin { Id = 5, UserId = 10004,AgentId = 10002, CreateTime = DateTime.Now, Status = 0 },
                new OnTheWayCoin { Id = 6, UserId = 10004,AgentId = 10002, CreateTime = DateTime.Now, Status = 0 });

            //代理人资格
            modelBuilder.Entity<AgentInfo>().HasData(
                new AgentInfo { UserId = 10001, Approve = 0 },
                new AgentInfo { UserId = 10002, Margin = 50000, CreditAmount = 50000, Approve = 1 });


            //家族
            modelBuilder.Entity<Family>().HasData(
                new Family { Id=1,MasterId=1,Name="system_club",DisplayOrder=0});
        }


        //世界
        public DbSet<World>                     Worlds { get; set; }

        //用户表
        public DbSet<User>                      Users { get; set; }
        public DbSet<AgentInfo>                 AgentInfos { get; set; }        //代理信息
        public DbSet<Marriage>                  Marriages { get; set; }         //婚姻状态
        public DbSet<UserBalanceChange>         UserBalanceChanges { get; set; }


        //绑定用户标签
        public DbSet<Tag>                       Tags { get; set; }
        public DbSet<UserTag>                   UserTags { get; set; }

        //核心应用
        public DbSet<TalksRecord>               TalksRecords { get; set; }
        public DbSet<MahjongRecord>             MJRecords { get; set; }


        //家族  ->/茶馆/组织/群
        public DbSet<Family>                    Familys { get; set; }
        public DbSet<FamilyIncome>              FamilyIncomes { get; set; }
        public DbSet<Desk>                      FamilyDesks { get; set; }
      


        //购买金币的订单
        public DbSet<OrderCoin>                 OrderCoins { get; set; }
        //在途资金
        public DbSet<OnTheWayCoin>              OnTheWayCoins { get; set; }


        //提现表
        public DbSet<WithDraw>                  WithDraws { get; set; }

        public DbSet<ReportNetwork>             ReportNetworks { get; set; }        
    }
}
