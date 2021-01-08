using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Data.Models
{
    public class AnswerPostFullRequest
    {
        [Required]
        public int? QuestionId { get; set; }

        [Required]
        public string Content { get; set; }

        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime Created { get; set; }
    }
}
