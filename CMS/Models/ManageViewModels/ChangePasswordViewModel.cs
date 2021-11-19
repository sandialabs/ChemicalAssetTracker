using Microsoft.AspNetCore.Html;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Models.ManageViewModels
{
    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public string StatusMessage { get; set; }

        // PH: added user profile information so that can be changed too
        //     this should correspond to the fields in Services.UserInfo
        // I am using HtmlStrings to prevent values from being urlencoded in the client
        public string UserName { get; set; }
        public HtmlString Email { get; set; }
        public string Password { get; set; }
        public List<string> Roles { get; set; }
        public HtmlString LastName { get; set; }
        public HtmlString FirstName { get; set; }
        public HtmlString MiddleName { get; set; }
        public HtmlString Position { get; set; }
        public HtmlString Workplace { get; set; }
        public HtmlString PhoneNumber { get; set; }
        public int HomeLocationID { get; set; }
        public string HomeLocation { get; set; }
        public bool IsChanged { get; set; }
    }
}
