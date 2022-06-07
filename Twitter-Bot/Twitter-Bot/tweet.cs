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
            foreach (var timelineMedia in timelineTweets[0].Media)
                if (timelineMedia.MediaType == "photo")
                    mediaList.Add(timelineMedia.MediaURLHttps);
                else
                    mediaList.Add(timelineMedia.MediaURLHttps);

            var tst = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
            var thisTime = TimeZoneInfo.ConvertTime(timelineTweets[0].CreatedAt.DateTime, TimeZoneInfo.Utc, tst);
            twlist.Add(new TweetList
            {
                Name = name,
                ScreenName = screenName,
                CreatedAt = thisTime,
                Text = timelineTweets[0].FullText,
                TwId = timelineTweets[0].Id,
                MediaList = mediaList,
                Type = timelineTweets[0].Media.Count == 0 ? TweetList.MediaType.None :
                    timelineTweets[0].Media[0].MediaType == "photo" ? TweetList.MediaType.Photo :
                    TweetList.MediaType.Video
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
                    foreach (var timelineMedia in timelineTweet.Media)
                        if (timelineMedia.MediaType == "photo")
                        {
                            mediaList.Add(timelineMedia.MediaURLHttps);
                        }
                        else
                        {
                            var videoUrl = "";
                            var max = 0;
                            foreach (var v in timelineMedia.VideoDetails.Variants)
                                if (v.Bitrate > max)
                                {
                                    max = v.Bitrate;
                                    videoUrl = v.URL;
                                }

                            mediaList.Add(videoUrl);
                        }

                    var tst = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
                    var thisTime = TimeZoneInfo.ConvertTime(timelineTweet.CreatedAt.DateTime, TimeZoneInfo.Utc, tst);
                    twlist.Add(new TweetList
                    {
                        Name = name,
                        ScreenName = screenName,
                        CreatedAt = thisTime,
                        Text = timelineTweet.FullText,
                        TwId = timelineTweet.Id,
                        MediaList = mediaList,
                        Type = timelineTweet.Media.Count == 0 ? TweetList.MediaType.None :
                            timelineTweet.Media[0].MediaType == "photo" ? TweetList.MediaType.Photo :
                            TweetList.MediaType.Video
                    });
                }
            }
        }

        return twlist;
    }

    public struct TweetList
    {
        public enum MediaType
        {
            None,
            Photo,
            Video
        }

        public string? Name = null;
        public string? ScreenName = null;
        public DateTimeOffset CreatedAt = default;
        public string? Text = null;
        public long? TwId = 0;
        public List<string> MediaList = new();
        public MediaType Type = MediaType.Photo;

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