using System.Collections.Generic;
using System.Linq;

namespace LanguagePractice.Services
{
    public class AiSiteProfile
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public bool SupportsAuto { get; set; } = true;

        public override string ToString() => Name;
    }

    public static class AiSiteCatalog
    {
        public static IReadOnlyList<AiSiteProfile> Presets { get; } = new List<AiSiteProfile>
        {
            new AiSiteProfile
            {
                Id = "GENSPARK",
                Name = "Genspark (AI Chat)",
                Url = "https://www.genspark.ai/agents?type=ai_chat",
                SupportsAuto = true
            },
            new AiSiteProfile
            {
                Id = "PERPLEXITY",
                Name = "Perplexity",
                Url = "https://www.perplexity.ai/",
                SupportsAuto = true
            },
            new AiSiteProfile
            {
                Id = "GOOGLE_AI",
                Name = "Google AI (Landing)",
                Url = "https://google.com/ai",
                // ランディングページでUIが一定ではないので自動は基本非推奨
                SupportsAuto = false
            }
        };

        public static AiSiteProfile GetByIdOrDefault(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return Presets[0];
            return Presets.FirstOrDefault(x => x.Id == id) ?? Presets[0];
        }
    }
}
