﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace backend.Data.Models
{
    public class QuestionPostFullRequest
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Please include content for the question")]
        public string Content { get; set; }

        public string UserId { get; set; }
        public string UserName { get; set; }

        public DateTime Created { get; set; }
    }
}
