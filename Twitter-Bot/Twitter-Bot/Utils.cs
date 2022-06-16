using System.Text.RegularExpressions;

namespace Twitter_Bot;

public static class Utils
{
    public static string? ReplaceByRegex(string? s)
    {
        var mc = Regex.Matches(s!, @"([\*_`\[])");
        for (var i = 0; i < mc.Count; i++)
            s = s!.Insert(mc[i].Index + i, @"\");

        return s;
    }
}