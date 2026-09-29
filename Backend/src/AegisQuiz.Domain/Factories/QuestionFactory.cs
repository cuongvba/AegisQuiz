using System;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Domain.Factories
{
    // [Design Pattern: Factory] — học từ quiz-service
    // Biến chuỗi "type" từ Excel/API thành đúng loại QuestionBase class
    public static class QuestionFactory
    {
        public static QuestionBase CreateQuestion(string type, string rawQuestionContent)
        {
            var cleanType = type?.Trim().ToUpperInvariant() ?? "";

            QuestionBase question = cleanType switch
            {
                "SINGLE"           => new SingleChoiceQuestion(),
                "MULTI"            => new MultiChoiceQuestion(),
                "TRUE_FALSE"       => new TrueFalseQuestion(),
                "MULTI_TRUE_FALSE" => new MultiTrueFalseQuestion(), // GDPT 2025: Đúng/Sai đa mệnh đề a) b) c) d)
                "SHORT_ANSWER"     => new ShortAnswerQuestion(),
                "FILL_BLANK"       => new FillBlankQuestion(),
                "ORDERING"         => new OrderingQuestion(),
                "MATCHING"         => new MatchingQuestion(),
                "MARCHING"         => new MatchingQuestion(), // alias Excel template
                "ESSAY"            => new EssayQuestion(),
                _ => throw new ArgumentException($"Không hỗ trợ loại câu hỏi: {type}")
            };

            question.Content = rawQuestionContent;
            question.Id = Guid.NewGuid();
            return question;
        }

        // Chuẩn hóa chuỗi type từ nhiều nguồn (Excel, API, DB cũ) → internal enum
        public static string NormalizeType(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "SINGLE";
            var h = raw.Trim().ToUpperInvariant();

            // Kiểm tra Đúng/Sai đa mệnh đề TRƯỚC "MULTI" để không bị nuốt thành MULTI
            if (h.Contains("MULTI_TRUE") || h.Contains("MULTI_TF") || h.Contains("TF_CLUSTER")
                || h.Contains("DUNG_SAI_DA_Y") || h.Contains("DUNG SAI DA Y") || h.Contains("MULTI TRUE"))
                                                                   return "MULTI_TRUE_FALSE";
            if (h.Contains("MULTI"))                               return "MULTI";
            if (h.Contains("SINGLE"))                              return "SINGLE";
            if (h.Contains("TRUE") || h.Contains("FALSE")
                || h.Contains("DUNG") || h.Contains("SAI"))       return "TRUE_FALSE";
            if (h.Contains("SHORT") || h.Contains("TRA LOI NGAN")) return "SHORT_ANSWER";
            if (h.Contains("FILL") || h.Contains("DIEN"))         return "FILL_BLANK";
            if (h.Contains("ORDER") || h.Contains("SAP XEP"))     return "ORDERING";
            if (h.Contains("MATCH") || h.Contains("MARCH")
                || h.Contains("NOI"))                              return "MATCHING";
            if (h.Contains("ESSAY") || h.Contains("TU LUAN"))     return "ESSAY";

            return "SINGLE";
        }
    }
}
