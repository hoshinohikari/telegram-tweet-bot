using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Twitter_Bot;

public class TelegramBot
{
    private readonly TelegramBotClient? _bot;
    private readonly CancellationTokenSource _cts = new();

    public TelegramBot(string? token)
    {
        _bot = new TelegramBotClient(token!);

        // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
        };

        _bot.StartReceiving(HandleUpdateAsync, HandlePollingErrorAsync, receiverOptions, _cts.Token);
    }

    private static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Log.ErrorLog(errorMessage);
        return Task.CompletedTask;
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        // Only process Message updates: https://core.telegram.org/bots/api#message
        if (update.Type != UpdateType.Message)
            return;
        // Only process text messages
        if (update.Message!.Type != MessageType.Text)
            return;

        Log.InfoLog($"Received a '{update.Message.Text}' message in chat {update.Message.Chat.Id}.");

        if (update.Message.Text!.Length <= 1 || update.Message.Text[0] != '/')
            return;

        if (!update.Message.Text.Contains(' '))
        {
            switch (update.Message.Text[1..])
            {
                case "sub":
                case "mediasub":
                case "unsub":
                    await _bot!.SendTextMessageAsync(update.Message.Chat.Id, "No subscribed object!!!",
                        cancellationToken: cancellationToken);
                    break;
                case "help":
                    await _bot!.SendTextMessageAsync(update.Message.Chat.Id, @"- /sub - subscribes to updates from users
- /mediasub - subscribes to updates from users (media only)
- /unsub - unsubscribes from users
- /sublist - get a list of subscribed users
- /help - view help text
", ParseMode.Markdown, cancellationToken: cancellationToken);
                    break;
                case "sublist":
                    var user = await Job.GetSubListAsync(update.Message.Chat.Id);
                    await _bot!.SendTextMessageAsync(update.Message.Chat.Id, user, ParseMode.Markdown,
                        cancellationToken: cancellationToken);
                    break;
            }

            return;
        }

        switch (update.Message.Text[1..update.Message.Text.IndexOf(' ')])
        {
            case "sub":
                await Job.AddSubAsync(update.Message.Text[5..], update.Message.Chat.Id, 0);
                break;
            case "mediasub":
                await Job.AddSubAsync(update.Message.Text[10..], update.Message.Chat.Id, 1);
                break;
            case "unsub":
                await Job.DelSubAsync(update.Message.Text[7..], update.Message.Chat.Id);
                break;
        }
    }

    public async Task SendTextAsync(string text, long id)
    {
        try
        {
            await _bot!.SendTextMessageAsync(id, text, ParseMode.Markdown, cancellationToken: _cts.Token);
        }
        catch (Exception e)
        {
            Log.ErrorLog(e.ToString());
            Log.ErrorLog(text);
            await Task.Delay(1000);
            try
            {
                await _bot!.SendTextMessageAsync(id, text, ParseMode.Markdown, cancellationToken: _cts.Token);
            }
            catch (Exception ex)
            {
                Log.ErrorLog(ex.ToString());
                Log.ErrorLog(text);
            }
        }
    }

    public async Task SendNoModeTextAsync(string text, long id)
    {
        try
        {
            await _bot!.SendTextMessageAsync(id, text, cancellationToken: _cts.Token);
        }
        catch (Exception e)
        {
            Log.ErrorLog(e.ToString());
            Log.ErrorLog(text);
            await Task.Delay(1000);
            try
            {
                await _bot!.SendTextMessageAsync(id, text, cancellationToken: _cts.Token);
            }
            catch (Exception ex)
            {
                Log.ErrorLog(ex.ToString());
                Log.ErrorLog(text);
            }
        }
    }

    public async Task SendPhotoGroupAsync(List<string> mediaList, string caption, long id)
    {
        var inputMedia = new List<IAlbumInputMedia>();
        try
        {
            inputMedia.AddRange(mediaList.Select((t, i) => i == 0
                ? new InputMediaPhoto(t) { Caption = caption, ParseMode = ParseMode.Markdown }
                : new InputMediaPhoto(t)));

            await _bot!.SendMediaGroupAsync(id, inputMedia, cancellationToken: _cts.Token);
        }
        catch (Exception e)
        {
            Log.ErrorLog(e.ToString());
            mediaList.ForEach(i => Log.ErrorLog($"mediaList is {i}"));
            await Task.Delay(1000);
            try
            {
                await _bot!.SendMediaGroupAsync(id, inputMedia, cancellationToken: _cts.Token);
            }
            catch (Exception ex)
            {
                Log.ErrorLog(ex.ToString());
                mediaList.ForEach(i => Log.ErrorLog($"mediaList is {i}"));
            }
        }
    }

    public async Task SendVideoGroupAsync(List<string> mediaList, string caption, long id)
    {
        var inputMedia = new List<IAlbumInputMedia>();
        try
        {
            inputMedia.AddRange(mediaList.Select((t, i) => i == 0
                ? new InputMediaVideo(t) { Caption = caption, ParseMode = ParseMode.Markdown }
                : new InputMediaVideo(t)));

            await _bot!.SendMediaGroupAsync(id, inputMedia, cancellationToken: _cts.Token);
        }
        catch (Exception e)
        {
            Log.ErrorLog(e.ToString());
            mediaList.ForEach(i => Log.ErrorLog($"mediaList is {i}"));
            await Task.Delay(1000);
            try
            {
                await _bot!.SendMediaGroupAsync(id, inputMedia, cancellationToken: _cts.Token);
            }
            catch (Exception ex)
            {
                Log.ErrorLog(ex.ToString());
                mediaList.ForEach(i => Log.ErrorLog($"mediaList is {i}"));
            }
        }
    }

    public async Task SendMediaGroupAsync(List<Tweet.Media> mediaList, string caption, long id)
    {
        var inputMedia = new List<IAlbumInputMedia>();

        try
        {
            for (var i = 0; i < mediaList.Count; i++)
                switch (mediaList[i].Type)
                {
                    case Tweet.Media.MediaType.Photo:
                    {
                        inputMedia.Add(i == 0
                            ? new InputMediaPhoto(mediaList[i].Url)
                                { Caption = caption, ParseMode = ParseMode.Markdown }
                            : new InputMediaPhoto(mediaList[i].Url));
                        break;
                    }
                    case Tweet.Media.MediaType.Video:
                    {
                        inputMedia.Add(i == 0
                            ? new InputMediaVideo(mediaList[i].Url)
                                { Caption = caption, ParseMode = ParseMode.Markdown }
                            : new InputMediaVideo(mediaList[i].Url));
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            await _bot!.SendMediaGroupAsync(id, inputMedia, cancellationToken: _cts.Token);
        }
        catch (Exception e)
        {
            Log.ErrorLog(e.ToString());
            mediaList.ForEach(i => Log.ErrorLog($"mediaList is {i}"));
            await Task.Delay(1000);
            try
            {
                await _bot!.SendMediaGroupAsync(id, inputMedia, cancellationToken: _cts.Token);
            }
            catch (Exception ex)
            {
                Log.ErrorLog(ex.ToString());
                mediaList.ForEach(i => Log.ErrorLog($"mediaList is {i}"));
            }
        }
    }
}