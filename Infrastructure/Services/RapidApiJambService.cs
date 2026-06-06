using System.Text.Json.Serialization;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Core.Domain.Enums;
using Trailblazers.Backend.Infrastructure.Extensions;

namespace Trailblazers.Backend.Infrastructure.Services
{
    public class RapidApiJambService(IHttpClientFactory httpClientFactory, ILogger<RapidApiJambService> logger)
        : IJambApiService
    {
        public async Task<IEnumerable<ExamQuestion>> FetchQuestionsAsync(ExamSubject subject, int year, int limit)
        {
            logger.LogInformation(
                "Initiating JAMB API request to fetch past questions. Subject: {Subject}, Year: {Year}, Limit: {Limit}",
                subject, year, limit);

            var apiKey = Environment.GetEnvironmentVariable("RAPID_API_KEY");
            var baseUrl = Environment.GetEnvironmentVariable("RAPID_API_BASE_URL");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogError("Required environment variable RAPID_API_KEY is missing.");
                throw new InvalidOperationException("Environment variable RAPID_API_KEY is missing or empty.");
            }

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                logger.LogError("Required environment variable RAPID_API_BASE_URL is missing.");
                throw new InvalidOperationException("Environment variable RAPID_API_BASE_URL is missing or empty.");
            }

            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("AccessToken", apiKey);

            // Construct the query URL
            var url = $"{baseUrl}?subject={Uri.EscapeDataString(subject.ToAlocSlug())}&year={year}";

            try
            {
                logger.LogInformation(
                    "Sending GET request to external JAMB API.\n[URL]: {Url}\n[Subject]: {Subject}\n[Year]: {Year}",
                    url, subject, year);
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("External JAMB API request failed.\n[Status Code]: {StatusCode}\n[URL]: {Url}",
                        response.StatusCode, url);
                    throw new HttpRequestException($"External JAMB API responded with status {response.StatusCode}");
                }

                var apiResult = await response.Content.ReadFromJsonAsync<AlocApiResponse>();
                if (apiResult?.Data == null)
                {
                    logger.LogWarning(
                        "No data returned or deserialization failed for external API.\n[Subject]: {Subject}\n[Year]: {Year}\n[URL]: {Url}",
                        subject, year, url);
                    return [];
                }

                logger.LogInformation(
                    "Successfully fetched and deserialized {Count} past questions from external API.\n[Subject]: {Subject}\n[Year]: {Year}\n[URL]: {Url}",
                    apiResult.Data.Count, subject, year, url);

                return apiResult.Data.Select(dto =>
                {
                    // Parse options
                    var options = new Dictionary<char, string>();
                    if (dto.Option != null)
                    {
                        if (!string.IsNullOrWhiteSpace(dto.Option.A)) options['A'] = dto.Option.A;
                        if (!string.IsNullOrWhiteSpace(dto.Option.B)) options['B'] = dto.Option.B;
                        if (!string.IsNullOrWhiteSpace(dto.Option.C)) options['C'] = dto.Option.C;
                        if (!string.IsNullOrWhiteSpace(dto.Option.D)) options['D'] = dto.Option.D;
                    }

                    // If options are empty, create fallback options so domain entity doesn't throw validation error
                    if (options.Count == 0)
                    {
                        options['A'] = "Option A";
                        options['B'] = "Option B";
                        options['C'] = "Option C";
                        options['D'] = "Option D";
                    }

                    // Parse CorrectOption char
                    char correctOption = 'A';
                    if (!string.IsNullOrWhiteSpace(dto.Answer))
                    {
                        var cleanAns = dto.Answer.Trim().ToUpperInvariant();
                        if (cleanAns.Length > 0 && cleanAns[0] >= 'A' && cleanAns[0] <= 'D')
                        {
                            correctOption = cleanAns[0];
                        }
                    }

                    // Extract and clean passage if present
                    string? passage = string.IsNullOrWhiteSpace(dto.Section) ? null : dto.Section.Trim();

                    return new ExamQuestion(
                        Guid.NewGuid(),
                        subject,
                        year,
                        dto.QuestionText ?? "Question text missing",
                        correctOption,
                        options,
                        imageUrl: string.IsNullOrWhiteSpace(dto.Image) ? null : dto.Image.Trim(),
                        comprehensionPassage: passage
                    );
                }).ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Abrupt fault in external JAMB API client integration.\n[Error Message]: {Message}\n[Subject]: {Subject}\n[Year]: {Year}\n[URL]: {Url}",
                    ex.Message, subject, year, url);
                throw;
            }
        }

        // ALOC API Response DTOs
        private class AlocApiResponse
        {
            [JsonPropertyName("status")] public int Status { get; }

            [JsonPropertyName("message")] public string? Message { get; }

            [JsonPropertyName("data")] public List<AlocQuestionDto>? Data { get; }
        }

        private class AlocQuestionDto
        {
            [JsonPropertyName("question")] public string? QuestionText { get; }

            [JsonPropertyName("option")] public AlocOptionsDto? Option { get; }

            [JsonPropertyName("answer")] public string? Answer { get; }

            [JsonPropertyName("section")] public string? Section { get; }

            [JsonPropertyName("image")] public string? Image { get; }
        }

        private class AlocOptionsDto
        {
            [JsonPropertyName("a")] public string? A { get; }

            [JsonPropertyName("b")] public string? B { get; }

            [JsonPropertyName("c")] public string? C { get; }

            [JsonPropertyName("d")] public string? D { get; }
        }
    }
}
