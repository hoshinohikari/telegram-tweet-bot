using Tweetinvi;
using Tweetinvi.Core.Extensions;
using Tweetinvi.Exceptions;
using Tweetinvi.Models;
using Tweetinvi.Models.Entities;
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

        if (sinceid == 0)
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
            }
            catch (Exception e)
            {
                Log.ErrorLog(e.ToString());
                await Task.Delay(1000);
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
                }
                catch (Exception ex)
                {
                    Log.ErrorLog(ex.ToString());
                }
            }
        else
            try
            {
                timelineTweets.Clear();
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
            }
            catch (Exception e)
            {
                Log.ErrorLog(e.ToString());
                await Task.Delay(1000);
                try
                {
                    timelineTweets.Clear();
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
                }
                catch (Exception ex)
                {
                    Log.ErrorLog(ex.ToString());
                }
            }

        if (timelineTweets.Count <= 0) return twlist;

        var user = await _app.Users.GetUserAsync(id);
        var name = user.Name;
        var screenName = user.ScreenName;

        foreach (var timelineTweet in timelineTweets)
        {
            List<Media> mediaList = new();

            if (timelineTweet.IsRetweet)
            {
                if (timelineTweet.RetweetedTweet.QuotedTweet != null)
                    foreach (var media in timelineTweet.RetweetedTweet.QuotedTweet.Media.Select(TweetMedia2BotMedia)
                                 .Where(media => !mediaList.Contains(media) && !media.Url.IsEmpty()))
                        mediaList.Add(media);

                foreach (var media in timelineTweet.RetweetedTweet.Media.Select(TweetMedia2BotMedia)
                             .Where(media => !mediaList.Contains(media) && !media.Url.IsEmpty())) mediaList.Add(media);
            }

            if (timelineTweet.QuotedTweet != null)
                foreach (var media in timelineTweet.QuotedTweet.Media.Select(TweetMedia2BotMedia)
                             .Where(media => !mediaList.Contains(media) && !media.Url.IsEmpty()))
                    mediaList.Add(media);

            foreach (var media in timelineTweet.Media.Select(TweetMedia2BotMedia)
                         .Where(media => !mediaList.Contains(media) && !media.Url.IsEmpty())) mediaList.Add(media);

            var tst = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
            var thisTime = TimeZoneInfo.ConvertTime(timelineTweet.CreatedAt.DateTime, TimeZoneInfo.Utc, tst);

            twlist.Add(new TweetList
            {
                Name = name,
                ScreenName = screenName,
                CreatedAt = thisTime,
                Text = timelineTweet.FullText,
                TwId = timelineTweet.Id,
                MediaList = mediaList
            });
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

    private static Media TweetMedia2BotMedia(IMediaEntity tweetMedia)
    {
        var media = new Media();
        switch (tweetMedia.MediaType)
        {
            case "photo":
            {
                media.Url = tweetMedia.MediaURLHttps;
                media.Type = Media.MediaType.Photo;
                break;
            }
            case "animated_gif":
            case "video":
            {
                var max = 0;
                foreach (var v in tweetMedia.VideoDetails.Variants)
                    if (v.Bitrate >= max)
                    {
                        max = v.Bitrate;
                        media.Url = v.URL;
                    }

                media.Type = Media.MediaType.Video;
                break;
            }
            default:
            {
                Log.WarnLog($"unknown media type {tweetMedia.MediaType}, url is {tweetMedia.MediaURLHttps}");
                break;
            }
        }

        return media;
    }

    public struct Media
    {
        public enum MediaType
        {
            Photo,
            Video
        }

        public string Url;
        public MediaType Type;
    }

    public struct TweetList
    {
        public string? Name;
        public string? ScreenName;
        public DateTime CreatedAt;
        public string? Text;
        public long? TwId;
        public List<Media> MediaList;
    }

    public struct UserList
    {
        public string Name;
        public string ScreenName;
    }
}