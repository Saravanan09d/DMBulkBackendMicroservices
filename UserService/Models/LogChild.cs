﻿using System.ComponentModel.DataAnnotations.Schema;

namespace UserService.Models
{
    public class LogChild
    {
        public int ID { get; set; }

        [ForeignKey("ParentID")]
        public int ParentID { get; set; }
        public LogParent Parent { get; set; }
        public string ErrorMessage { get; set; }
        public string Filedata { get; set; }
        public string ErrorRowNumber { get; set; }
    }
}
