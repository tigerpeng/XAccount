using System;
using System.Globalization;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace XAccount.Helpers
{
    // Custom exception class for throwing application specific exceptions (e.g. for validation) 
    // that can be caught and handled within the application
    public class AppException : Exception
    {
        public AppException() : base() { }

        public AppException(string message) : base(message) { }

        public AppException(string message, params object[] args)
            : base(String.Format(CultureInfo.CurrentCulture, message, args))
        {
        }
    }

    public class AppSettings
    {
        public string Secret { get; set; }
        public string Name { get; set; }
    }



    enum CoinsType
    {
        Coins = 1,          //金币
        Beans = 2,            //金豆
        Scores = 3,           //积分
    };

}