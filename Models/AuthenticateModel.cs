using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace XAccount.Models
{
    public class AuthenticateModel
    {
        [Required]
        public long Phone { get; set; }

        public string Password { get; set; }
    }



    public class LoginRetModel
    {
        public string Token { get; set; }

        public long Uid { get; set; }
    }

}