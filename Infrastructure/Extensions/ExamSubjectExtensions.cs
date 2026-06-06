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
            { ExamSubject.LiteratureInEnglish, "englishlit" },
            { ExamSubject.Economics, "economics" },
            { ExamSubject.Commerce, "commerce" },
            { ExamSubject.ChristianReligiousStudies, "crk" },
            { ExamSubject.IslamicReligiousStudies, "irk" },
            { ExamSubject.History, "history" }
        };

        public static string ToAlocSlug(this ExamSubject subject)
        {
            if (Slugs.TryGetValue(subject, out var slug)) return slug;
            throw new ArgumentOutOfRangeException(nameof(subject),
                $"Subject {subject} is not configured for ALOC mapping.");
        }
    }
}
