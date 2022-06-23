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

            try
            {
                timelineTweets.Clear();
                var userTimeline = _app.Timelines.GetUserTimelineIterator(new GetUserTimelineParameters(id)
                {
                    IncludeEntities = true,
                    IncludeRetweets = true,
                    PageSize = 1
                    //SinceId = 
                });
                var page = await userTimeline.NextPageAsync();
                timelineTweets.AddRange(page);
                if (timelineTweets.Count <= 0) return twlist;
            }
            catch (Exception e)
            {
                Log.ErrorLog(e.ToString());
                try
                {
                    timelineTweets.Clear();
                    var userTimeline = _app.Timelines.GetUserTimelineIterator(new GetUserTimelineParameters(id)
                    {
                        IncludeEntities = true,
                        IncludeRetweets = true,
                        PageSize = 1
                        //SinceId = 
                    });
                    var page = await userTimeline.NextPageAsync();
                    timelineTweets.AddRange(page);
                    if (timelineTweets.Count <= 0) return twlist;
                }
                catch (Exception ex)
                {
                    Log.ErrorLog(ex.ToString());
                }
            }

            List<string> mediaList = new();
            if (!timelineTweets[0].IsRetweet)
            {
                if (timelineTweets[0].QuotedTweet != null)
                {
                    if (timelineTweets[0].Media.Count != 0 && timelineTweets[0].Media[0].MediaType != "photo")
                    {
                    }
                    else
                    {
                        mediaList.AddRange(from quotedTweetMedia in timelineTweets[0].QuotedTweet.Media
                            where quotedTweetMedia.MediaType == "photo"
                            select quotedTweetMedia.MediaURLHttps);
                    }
                }

                foreach (var timelineMedia in timelineTweets[0].Media)
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
            }
            else
            {
                mediaList.AddRange(from retweetedTweetMedia in timelineTweets[0].RetweetedTweet.Media
                    where retweetedTweetMedia.MediaType == "photo"
                    select retweetedTweetMedia.MediaURLHttps);
                foreach (var timelineMedia in timelineTweets[0].Media)
                    if (timelineMedia.MediaType == "photo")
                    {
                        if (!mediaList.Contains(timelineMedia.MediaURLHttps))
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
            }

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

            if (timelineTweets.Count <= 0) return twlist;
            var user = await _app.Users.GetUserAsync(id);
            name = user.Name;
            screenName = user.ScreenName;
            foreach (var timelineTweet in timelineTweets)
            {
                List<string> mediaList = new();
                if (!timelineTweet.IsRetweet)
                {
                    if (timelineTweet.QuotedTweet != null)
                    {
                        if (timelineTweet.Media.Count != 0 && timelineTweet.Media[0].MediaType != "photo")
                        {
                        }
                        else
                        {
                            mediaList.AddRange(from quotedTweetMedia in timelineTweet.QuotedTweet.Media
                                where quotedTweetMedia.MediaType == "photo"
                                select quotedTweetMedia.MediaURLHttps);
                        }
                    }

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
                }
                else
                {
                    mediaList.AddRange(from retweetedTweetMedia in timelineTweet.RetweetedTweet.Media
                        where retweetedTweetMedia.MediaType == "photo"
                        select retweetedTweetMedia.MediaURLHttps);
                    foreach (var timelineMedia in timelineTweet.Media)
                        if (timelineMedia.MediaType == "photo")
                        {
                            if (!mediaList.Contains(timelineMedia.MediaURLHttps))
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

        return twlist;
    }

    public async Task<UserList> GetUserListAsync(long id)
    {
        UserList userList;

        var user = await _app.Users.GetUserAsync(id);
        userList.Name = user.Name;
        userList.ScreenName = user.ScreenName;

        return userList;
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
        public DateTime CreatedAt = default;
        public string? Text = null;
        public long? TwId = 0;
        public List<string> MediaList = new();
        public MediaType Type = MediaType.Photo;

        public TweetList()
        {
        }
    }

    public struct UserList
    {
        public string Name;
        public string ScreenName;
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