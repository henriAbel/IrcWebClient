using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace IrcWebApplication.Models
{
    public class LoginModel
    {
        [Required]
        [StringLength(9, MinimumLength=2)]        
        [RegularExpression("([a-zA-Z0-9 .&'-]+)", ErrorMessage = "Enter only alphabets and numbers")]
        public string Name { get; set; }

        [Required]
        public string Server { get; set; }

        [Required]
        public string Channel { get; set; }
        
        public int? Port { get; set; }

        [DisplayName("Stay logged in after closing browser")]
        public bool StayLogged { get; set; }
        
        [DisplayName("Channel password")]
        public string ChannelPassword { get; set; }

        [DisplayName("Server password")]
        public string ServerPassword { get; set; }

        [HiddenInput(DisplayValue = false)]
        public string Uuid { get; set; }

    }
}