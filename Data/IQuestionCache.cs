using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Data.Models;

namespace backend.Data
{
    public interface IQuestionCache
    {
        QuestionGetSingleResponse Get(int questionId);

        void Remove(int questionId);

        void Set(QuestionGetSingleResponse question);
    }
}
