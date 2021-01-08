using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using backend.Hubs;
using backend.Data;
using backend.Data.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace backend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionsController : ControllerBase
    {
        private readonly IDataRepository _dataRepository;
        private readonly IHubContext<QuestionsHub> _questionHubContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _auth0UserInfo;
        private readonly IQuestionCache _cache;

        public QuestionsController(IDataRepository dataRepository, IHubContext<QuestionsHub> questionHubContext, IHttpClientFactory httpClientFactory,
            IConfiguration configuration, IQuestionCache questionCache)
        {
            _dataRepository = dataRepository;
            _questionHubContext = questionHubContext;
            _httpClientFactory = httpClientFactory;
            _auth0UserInfo = $"{configuration["Auth0:Authority"]}userinfo";
            _cache = questionCache;
        }

        [AllowAnonymous]
        [HttpGet("{questionId}")]
        public async Task<ActionResult<QuestionGetSingleResponse>> GetQuestion(int questionId)
        {
            var question = _cache.Get(questionId);
            if (question == null)
            {
                question = await _dataRepository.GetQuestion(questionId);
                if (question == null) return NotFound();
                _cache.Set(question);
            }
            return question;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IEnumerable<QuestionGetManyResponse>> GetQuestions(string search, bool includeAnswers, int pageNumber = 1, int pageSize = 20)
        {
            if (string.IsNullOrEmpty(search))
            {
                if (includeAnswers) return await _dataRepository.GetQuestionsWithAnswers();
                else return await _dataRepository.GetQuestions();
            }
            else return await _dataRepository.GetQuestionsBySearchWithPaging(search, pageNumber, pageSize);
        }

        [AllowAnonymous]
        [HttpGet("unanswered")]
        public async Task<IEnumerable<QuestionGetManyResponse>> GetUnansweredQuestions(int pageNumber = 1, int pageSize = 20)
        {
            return await _dataRepository.GetUnansweredQuestionsWithPaging(pageNumber, pageSize);
        }

        [HttpPost]
        public async Task<ActionResult<QuestionGetSingleResponse>> PostQuestion(QuestionPostRequest questionPostRequest)
        {
            var savedQuestion = await _dataRepository.PostQuestion(new QuestionPostFullRequest
            {
                Title = questionPostRequest.Title,
                Content = questionPostRequest.Content,
                UserId = User.FindFirst(ClaimTypes.NameIdentifier).Value,
                UserName = await GetUserName(),
                Created = DateTime.UtcNow
            });
            return CreatedAtAction(nameof(GetQuestion), new { questionId = savedQuestion.QuestionId }, savedQuestion);
        }

        [Authorize(Policy = "MustBeQuestionAuthor")]
        [HttpPut("{questionId}")]
        public async Task<ActionResult<QuestionGetSingleResponse>> PutQuestion(int questionId, QuestionPutRequest questionPutRequest)
        {
            var question = await _dataRepository.GetQuestion(questionId);
            if (question == null) return NotFound();
            questionPutRequest.Title = string.IsNullOrEmpty(questionPutRequest.Title) ? question.Title
                : questionPutRequest.Title;
            questionPutRequest.Content = string.IsNullOrEmpty(questionPutRequest.Content) ? question.Content
                : questionPutRequest.Content;
            _cache.Remove(questionId);
            return await _dataRepository.PutQuestion(questionId, questionPutRequest);
        }

        [HttpPost("answer")]
        public async Task<ActionResult<AnswerGetResponse>> PostAnswer(AnswerPostRequest answerPostRequest)
        {
            var questionExists = await _dataRepository.QuestionExists(answerPostRequest.QuestionId.Value);
            if (!questionExists) return NotFound();
            var savedAnswer = await _dataRepository.PostAnswer(new AnswerPostFullRequest
            {
                QuestionId = answerPostRequest.QuestionId,
                Content = answerPostRequest.Content,
                UserId = User.FindFirst(ClaimTypes.NameIdentifier).Value,
                UserName = await GetUserName(),
                Created = DateTime.UtcNow,
            });
            _cache.Remove(answerPostRequest.QuestionId.Value);
            await _questionHubContext.Clients.Group($"Question-{answerPostRequest.QuestionId.Value}")
                .SendAsync("ReceiveQuestion", _dataRepository.GetQuestion(answerPostRequest.QuestionId.Value));
            return savedAnswer;
        }

        [Authorize("MustBeQuestionAuthor")]
        [HttpDelete("{questionId}")]
        public async Task<ActionResult> DeleteQuestion(int questionId)
        {
            var question = await _dataRepository.GetQuestion(questionId);
            if (question == null) return NotFound();
            await _dataRepository.DeleteQuestion(questionId);
            _cache.Remove(questionId);
            return NoContent();
        }

        private async Task<string> GetUserName()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, _auth0UserInfo);
            request.Headers.Add("Authorization", Request.Headers["Authorization"].First());

            var client = _httpClientFactory.CreateClient();
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<User>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return user.Name;
            }
            else
            {
                return "";
            }
        }
    }
}
