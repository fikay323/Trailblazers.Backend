using Trailblazers.Backend.Core.Domain.Enums;

namespace Trailblazers.Backend.Infrastructure.Extensions
{
    public static class ExamSubjectExtensions
    {
        private static readonly Dictionary<ExamSubject, string> Slugs = new()
        {
            { ExamSubject.English, "english" },
            { ExamSubject.Mathematics, "mathematics" },
            { ExamSubject.Biology, "biology" },
            { ExamSubject.Chemistry, "chemistry" },
            { ExamSubject.Physics, "physics" },
            { ExamSubject.Geography, "geography" },
            { ExamSubject.CivicEducation, "civiledu" },
            { ExamSubject.Government, "government" },
            { ExamSubject.Englishlit, "englishlit" },
            { ExamSubject.Economics, "economics" },
            { ExamSubject.Commerce, "commerce" },
            { ExamSubject.Crk, "crk" },
            { ExamSubject.Irk, "irk" },
            { ExamSubject.History, "history" },
            { ExamSubject.Accounting, "accounting" },
            { ExamSubject.Insurance, "insurance" },
            { ExamSubject.Currentaffairs, "currentaffairs" }
        };

        public static string ToAlocSlug(this ExamSubject subject)
        {
            if (Slugs.TryGetValue(subject, out var slug)) return slug;
            throw new ArgumentOutOfRangeException(nameof(subject),
                $"Subject {subject} is not configured for ALOC mapping.");
        }

        public static ExamSubject? ToExamSubject(string subjectName)
        {
            if (string.IsNullOrWhiteSpace(subjectName)) return null;

            var normalized = subjectName.Replace(" ", "").Replace("-", "").ToLowerInvariant();

            return normalized switch
            {
                "english" => ExamSubject.English,
                "mathematics" or "maths" => ExamSubject.Mathematics,
                "biology" => ExamSubject.Biology,
                "chemistry" => ExamSubject.Chemistry,
                "physics" => ExamSubject.Physics,
                "geography" => ExamSubject.Geography,
                "civiceducation" or "civiledu" => ExamSubject.CivicEducation,
                "government" => ExamSubject.Government,
                "literatureinenglish" or "englishlit" or "litinenglish" => ExamSubject.Englishlit,
                "economics" => ExamSubject.Economics,
                "commerce" => ExamSubject.Commerce,
                "christianreligiousstudies" or "crk" or "crs" => ExamSubject.Crk,
                "islamicreligiousstudies" or "irk" or "irs" => ExamSubject.Irk,
                "history" => ExamSubject.History,
                "accounting" or "accounts" => ExamSubject.Accounting,
                "insurance" => ExamSubject.Insurance,
                "currentaffairs" => ExamSubject.Currentaffairs,
                _ => Enum.TryParse<ExamSubject>(subjectName, true, out var result) ? result : null
            };
        }
    }
}
