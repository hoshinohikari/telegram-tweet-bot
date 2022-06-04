using Tweetinvi;
using Tweetinvi.Exceptions;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace Twitter_Bot;

public class Tweet
{
    private readonly TwitterClient _app;

    public Tweet(string? consumerKey, string? consumerSecret, string? bearerToken)
    {
        var tw = new TwitterCredentials(consumerKey, consumerSecret, bearerToken);
        _app = new TwitterClient(tw);
    }

    public async Task<long> GetUserId(string name)
    {
        IUser? user;
        try
        {
            user = await _app.Users.GetUserAsync(name);
        }
        catch (TwitterException)
        {
            return 0;
            // ignored
        }

        return user.Id;
    }

    public async Task<List<TweetList>> GetTweetAsync(long id, long sinceid)
    {
        List<TweetList> twlist = new();
        var timelineTweets = new List<ITweet>();
        string name;
        string screenName;
        if (sinceid == 0)
        {
            var user = await _app.Users.GetUserAsync(id);
            name = user.Name;
            screenName = user.ScreenName;

            var userTimeline = _app.Timelines.GetUserTimelineIterator(new GetUserTimelineParameters(id)
            {
                IncludeEntities = true,
                IncludeRetweets = true,
                PageSize = 10
                //SinceId = 
            });
            var page = await userTimeline.NextPageAsync();
            timelineTweets.AddRange(page);
            List<string> mediaList = new();
            /*foreach (var timelineMedia in timelineTweets[0].Media)
            {
                if (timelineMedia.MediaType == "photo")
                    MediaList.Add(timelineMedia.MediaURLHttps);
                else
                    MediaList.Add(timelineMedia.MediaURLHttps);
            }*/
            twlist.Add(new TweetList
            {
                Name = name,
                ScreenName = screenName,
                CreatedAt = timelineTweets[0].CreatedAt,
                Text = timelineTweets[0].FullText,
                TwId = timelineTweets[0].Id,
                MediaList = mediaList
            });
        }
        else
        {
            var userTimeline = _app.Timelines.GetUserTimelineIterator(new GetUserTimelineParameters(id)
            {
                IncludeEntities = true,
                IncludeRetweets = true,
                SinceId = sinceid
            });
            while (!userTimeline.Completed)
            {
                var page = await userTimeline.NextPageAsync();
                timelineTweets.AddRange(page);
            }

            if (timelineTweets.Count > 0)
            {
                var user = await _app.Users.GetUserAsync(id);
                name = user.Name;
                screenName = user.ScreenName;
                foreach (var timelineTweet in timelineTweets)
                {
                    List<string> mediaList = new();
                    twlist.Add(new TweetList
                    {
                        Name = name,
                        ScreenName = screenName,
                        CreatedAt = timelineTweet.CreatedAt,
                        Text = timelineTweet.FullText,
                        TwId = timelineTweet.Id,
                        MediaList = mediaList
                    });
                }
            }
        }

        return twlist;
    }

    public struct TweetList
    {
        public string? Name = null;
        public string? ScreenName = null;
        public DateTimeOffset CreatedAt = default;
        public string? Text = null;
        public long? TwId = 0;
        public List<string> MediaList = new();

        public TweetList()
        {
        }
    }

    /*public async Task test()
    {
        // create a consumer only credentials
        var appCredentials = new TwitterCredentials(consumerKey,
            consumerSecret,
            bearerToken);

        var appClient = new TwitterClient(appCredentials);

        var timelineTweets = new List<ITweet>();

        //appClient.Timelines.GetUserTimelineAsync("hoshicolle_info");
        var userTimeline = appClient.Timelines.GetUserTimelineIterator(new GetUserTimelineParameters("hoshicolle_info")
        {
            //ContinueMinMaxCursor = 
            //CustomQueryParameters = {  },
            //ExcludeReplies = true,
            //IncludeContributorDetails = true,
            IncludeEntities = true,
            IncludeRetweets = true,
            //MaxId = 1,
            //PageSize = 9
            SinceId = 1523679305133596672
            //TrimUser = 
            //TweetMode = 
            //User = 
        });

        while (!userTimeline.Completed)
        {
            var page = await userTimeline.NextPageAsync();
            timelineTweets.AddRange(page);
        }
    }*/
}