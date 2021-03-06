namespace Twitter_Bot;

public static class Job
{
    private static Sqlite? _sql;
    private static TelegramBot? _bot;
    private static Tweet? _tw;

    public static void Start(string? token, string? consumerKey, string? consumerSecret, string? bearerToken)
    {
        _sql = new Sqlite("tweet.db");
        _bot = new TelegramBot(token);
        _tw = new Tweet(consumerKey, consumerSecret, bearerToken);
    }

    public static async Task AddSubAsync(string subList, long chatId, long kind)
    {
        var subs = subList.Split(' ');
        List<Task> addJobs = new();

        foreach (var sub in subs)
        {
            var id = await _tw!.GetUserId(sub);
            if (id == 0)
            {
                addJobs.Add(_bot!.SendNoModeTextAsync($"User {sub} does not exist", chatId));
                continue;
            }

            addJobs.Add(_sql!.AddSubAsync(id, chatId, kind));
        }

        foreach (var addJob in addJobs) await addJob;
    }

    public static async Task DelSubAsync(string subList, long chatId)
    {
        var subs = subList.Split(' ');
        List<Task> addJobs = new();

        foreach (var sub in subs)
        {
            var id = await _tw!.GetUserId(sub);
            if (id == 0)
            {
                addJobs.Add(_bot!.SendNoModeTextAsync($"User {sub} does not exist", chatId));
                continue;
            }

            addJobs.Add(_sql!.DelSubAsync(id, chatId));
        }

        foreach (var addJob in addJobs) await addJob;
    }

    public static async Task GetTweetAsync()
    {
        List<Task> sendJobs = new();

        var subList = await _sql!.GetSubListAsync();

        foreach (var user in subList)
        {
            var tweetList = await _tw!.GetTweetAsync(user.Id, user.Sinceid);
            if (tweetList.Count <= 0) continue;
            foreach (var subTweet in tweetList)
                for (var i = 0; i < user.ChatId.Count; i++)
                {
                    var chat = user.ChatId[i];
                    var kind = user.SubKind[i];
                    var tweetText = @$"
*{subTweet.Name}* ([@{subTweet.ScreenName}](https://twitter.com/{subTweet.ScreenName})) at {subTweet.CreatedAt:MM/dd/yyy H:mm:ss}:
{Utils.ReplaceByRegex(subTweet.Text)}
-- [Link to this Tweet](https://twitter.com/{subTweet.ScreenName}/status/{subTweet.TwId})
";
                    if (subTweet.MediaList.Count == 0)
                    {
                        if (kind != 0) continue;
                        var t = _bot!.SendTextAsync(tweetText, chat);
                        sendJobs.Add(t);
                    }
                    else
                    {
                        var t = _bot!.SendMediaGroupAsync(subTweet.MediaList, tweetText, chat);
                        sendJobs.Add(t);
                    }
                }

            _sql.UpdateLastTweet(user.Id, tweetList[0].TwId);
        }

        foreach (var sendJob in sendJobs) await sendJob;
    }

    public static async Task<string> GetSubListAsync(long chatId)
    {
        var subListText = "sub list:";
        var mediasubListText = "mediasub list:";
        var subList = await _sql!.GetSubListAsync();

        for (var i = 0; i < subList.Count; i++)
        {
            if (!subList[i].ChatId.Contains(chatId)) continue;
            if (subList[i].SubKind[subList[i].ChatId.FindIndex(l => l == chatId)] == 0)
            {
                subListText += "\n";
                var userList = await _tw!.GetUserListAsync(subList[i].Id);
                subListText +=
                    $"*{userList.Name}* ([@{userList.ScreenName}](https://twitter.com/{userList.ScreenName}))";
            }
            else
            {
                mediasubListText += "\n";
                var userList = await _tw!.GetUserListAsync(subList[i].Id);
                mediasubListText +=
                    $"*{userList.Name}* ([@{userList.ScreenName}](https://twitter.com/{userList.ScreenName}))";
            }
        }

        subListText += "\n";
        subListText += "\n";
        subListText += mediasubListText;

        return subListText;
    }

    public static async Task<int> GetSubNum()
    {
        var subList = await _sql!.GetSubListAsync();
        return subList.Count;
    }
}