// See https://aka.ms/new-console-template for more information

using SharpYaml.Serialization;
using Tweetinvi.Core.Extensions;
using Twitter_Bot;

Dictionary<object, object>? yamlTree;
string? telegramBotToken, twitterConsumerKey, twitterConsumerSecret, twitterBearerToken;

if (File.Exists("config.yml"))
{
    var config = await File.ReadAllTextAsync("config.yml");
    var serializer = new Serializer();
    yamlTree = (Dictionary<object, object>)serializer.Deserialize(new StringReader(config));
}
else
{
    Log.ErrorLog("Config file does not exist");
    return;
}

try
{
    telegramBotToken = yamlTree["TELEGRAM_BOT_TOKEN"].ToString();
    twitterConsumerKey = yamlTree["TWITTER_CONSUMER_KEY"].ToString();
    twitterConsumerSecret = yamlTree["TWITTER_CONSUMER_SECRET"].ToString();
    twitterBearerToken = yamlTree["TWITTER_BEARER_TOKEN"].ToString();

    if (telegramBotToken.IsEmpty() || twitterConsumerKey.IsEmpty() || twitterConsumerSecret.IsEmpty() ||
        twitterBearerToken.IsEmpty())
    {
        Log.ErrorLog("Token Is Empty");
        return;
    }
}
catch (Exception ex)
{
    Log.ErrorLog("Config file error");
    Log.ErrorLog(ex.ToString());
    return;
}

Job.Start(telegramBotToken, twitterConsumerKey, twitterConsumerSecret, twitterBearerToken);

var running = true;

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true; //true: 不导致退出。false: 会导致退出
    running = false;
    Log.InfoLog("You have Press Ctrl+C");
};

await Job.GetTweetAsync();

while (running)
{
    await Task.Delay(60000);

    await Job.GetTweetAsync();
}

Log.InfoLog("Quit");