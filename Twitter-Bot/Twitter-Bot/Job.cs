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

    public static async Task AddSubAsync(string subList, long chatId)
    {
        var subs = subList.Split(' ');
        List<Task> addJobs = new();

        foreach (var sub in subs)
        {
            var id = await _tw!.GetUserId(sub);
            if (id == 0)
            {
                addJobs.Add(_bot!.SendTextAsync($"User {sub} does not exist", chatId));
                continue;
            }

            addJobs.Add(_sql!.AddSubAsync(id, chatId));
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
                addJobs.Add(_bot!.SendTextAsync($"User {sub} does not exist", chatId));
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
            foreach (var chat in user.ChatId)
            {
                var tweetText = @$"
*{subTweet.Name}* ([@{subTweet.ScreenName}](https://twitter.com/{subTweet.ScreenName})) at {subTweet.CreatedAt:MM/dd/yyy H:mm:ss}:
{Utils.ReplaceByRegex(subTweet.Text)}
-- [Link to this Tweet](https://twitter.com/{subTweet.ScreenName}/status/{subTweet.TwId})
";
                if (subTweet.MediaList.Count == 0)
                {
                    var t = _bot!.SendTextAsync(tweetText, chat);
                    sendJobs.Add(t);
                }
                else
                {
                    switch (subTweet.Type)
                    {
                        case Tweet.TweetList.MediaType.None:
                        case Tweet.TweetList.MediaType.Photo:
                        {
                            var t = _bot!.SendPhotoGroupAsync(subTweet.MediaList, tweetText, chat);
                            sendJobs.Add(t);
                            break;
                        }
                        case Tweet.TweetList.MediaType.Video:
                        {
                            var t = _bot!.SendVideoGroupAsync(subTweet.MediaList, tweetText, chat);
                            sendJobs.Add(t);
                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            _sql.UpdateLastTweet(user.Id, tweetList[0].TwId);
        }

        foreach (var sendJob in sendJobs) await sendJob;
    }

    public static async Task<string> GetSubListAsync(long chatId)
    {
        var subListText = "sub list:";
        var subList = await _sql!.GetSubListAsync();

        foreach (var user in subList.Where(user => user.ChatId.Contains(chatId)))
        {
            subListText += "\n";
            var userList = await _tw!.GetUserListAsync(user.Id);
            subListText += $"*{userList.Name}* ([@{userList.ScreenName}](https://twitter.com/{userList.ScreenName}))";
        }

        return subListText;
    }
}