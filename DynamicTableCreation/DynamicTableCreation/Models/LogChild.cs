﻿using System.ComponentModel.DataAnnotations.Schema;

namespace DynamicTableCreation.Models
{
    public class LogChild
    {
        public int ID { get; set; }

        [ForeignKey("ParentID")]
        public int ParentID { get; set; }
        public LogParent Parent { get; set; }
        public string ErrorMessage { get; set; }
        public string Filedata { get; set; }
        public int ParentLogID { get; set; }
    }
}
