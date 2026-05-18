using Slugify;

namespace Application;

public static class SlugConfigurator
{
    public static SlugHelper GetRussianSlugHelper()
    {
        var config = new SlugHelperConfiguration();
        
        var replacements = new Dictionary<string, string>
        {
            {"а", "a"}, {"б", "b"}, {"в", "v"}, {"г", "g"}, {"д", "d"}, {"е", "e"}, {"ё", "yo"},
            {"ж", "zh"}, {"з", "z"}, {"и", "i"}, {"й", "j"}, {"к", "k"}, {"л", "l"}, {"м", "m"},
            {"н", "n"}, {"о", "o"}, {"п", "p"}, {"р", "r"}, {"с", "s"}, {"т", "t"}, {"у", "u"},
            {"ф", "f"}, {"х", "h"}, {"ц", "c"}, {"ч", "ch"}, {"ш", "sh"}, {"щ", "sch"}, {"ъ", ""},
            {"ы", "y"}, {"ь", ""}, {"э", "e"}, {"ю", "yu"}, {"я", "ya"}
        };

        foreach (var replacement in replacements)
        {
            config.StringReplacements.Add(replacement.Key, replacement.Value);
        }

        return new SlugHelper(config);
    }
}