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
            Console.WriteLine(id);
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
            if (tweetList.Count > 0)
            {
                foreach (var subTweet in tweetList)
                foreach (var chat in user.ChatId)
                {
                    var tweetText = @$"
*{subTweet.Name}* ([@{subTweet.ScreenName}](https://twitter.com/{subTweet.ScreenName})) at {subTweet.CreatedAt}:
{subTweet.Text}
-- [Link to this Tweet](https://twitter.com/{subTweet.ScreenName}/status/{subTweet.TwId})
";
                    if (subTweet.MediaList.Count == 0)
                    {
                        var t = _bot!.SendTextAsync(tweetText, chat);
                        sendJobs.Add(t);
                    }
                    else
                    {
                        if (subTweet.Type == Tweet.TweetList.MediaType.Photo)
                        {
                            var t = _bot!.SendPhotoGroupAsync(subTweet.MediaList, tweetText, chat);
                            sendJobs.Add(t);
                        }
                        else if (subTweet.Type == Tweet.TweetList.MediaType.Video)
                        {
                            var t = _bot!.SendVideoGroupAsync(subTweet.MediaList, tweetText, chat);
                            sendJobs.Add(t);
                        }
                    }
                }

                _sql.UpdateLastTweet(user.Id, tweetList[0].TwId);
            }
        }

        foreach (var sendJob in sendJobs) await sendJob;
    }
}