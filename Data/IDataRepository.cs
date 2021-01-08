using System.Collections.Generic;
using System.Threading.Tasks;

using backend.Data.Models;

namespace backend.Data
{
    public interface IDataRepository
    {
        Task<QuestionGetSingleResponse> GetQuestion(int questionId);

        Task<IEnumerable<QuestionGetManyResponse>> GetQuestions();

        Task<IEnumerable<QuestionGetManyResponse>> GetQuestionsWithAnswers();

        Task<IEnumerable<QuestionGetManyResponse>> GetQuestionsBySearch(string search);

        Task<IEnumerable<QuestionGetManyResponse>> GetQuestionsBySearchWithPaging(string search, int pageNumber, int pageSize);

        Task<IEnumerable<QuestionGetManyResponse>> GetUnansweredQuestions();

        Task<IEnumerable<QuestionGetManyResponse>> GetUnansweredQuestionsWithPaging(int pageNumber, int pageSize);

        Task<bool> QuestionExists(int questionId);

        Task<QuestionGetSingleResponse> PostQuestion(QuestionPostFullRequest question);

        Task<QuestionGetSingleResponse> PutQuestion(int questionId, QuestionPutRequest question);

        Task DeleteQuestion(int questionId);

        Task<AnswerGetResponse> GetAnswer(int answerId);

        Task<AnswerGetResponse> PostAnswer(AnswerPostFullRequest answer);
    }
}
