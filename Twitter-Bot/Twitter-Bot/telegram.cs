﻿using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Twitter_Bot;

public class TelegramBot
{
    private readonly TelegramBotClient? _bot;

    public TelegramBot(string? token)
    {
        _bot = new TelegramBotClient(token!);

        using var cts = new CancellationTokenSource();

        // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
        };

        _bot.StartReceiving(HandleUpdateAsync, HandlePollingErrorAsync, receiverOptions, cts.Token);
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
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

        Console.WriteLine($"Received a '{update.Message.Text}' message in chat {update.Message.Chat.Id}.");

        if (update.Message.Text!.Length <= 1 || update.Message.Text[0] != '/')
            return;

        if (update.Message.Text.IndexOf(' ') == -1)
        {
            switch (update.Message.Text.Substring(1, update.Message.Text.Length - 1))
            {
                case "sub":
                    await _bot!.SendTextMessageAsync(update.Message.Chat.Id, "No subscribed object!!!",
                        cancellationToken: cancellationToken);
                    break;
                case "help":
                    await _bot!.SendTextMessageAsync(update.Message.Chat.Id, "It doesn't help now!!!",
                        cancellationToken: cancellationToken);
                    break;
            }

            return;
        }

        switch (update.Message.Text.Substring(1, update.Message.Text.IndexOf(' ') - 1))
        {
            case "sub":
                //Console.WriteLine(update.Message.Text.Substring(5, update.Message.Text.Length - 5));
                await Job.AddSubAsync(update.Message.Text.Substring(5, update.Message.Text.Length - 5),
                    update.Message.Chat.Id);
                break;
        }
    }

    public async Task SendTextAsync(string text, long id)
    {
        await _bot!.SendTextMessageAsync(id, text, ParseMode.Markdown);
    }

    /*public async Task test()
    {
        var botClient = new TelegramBotClient(token);

        using var cts = new CancellationTokenSource();

        // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
        };

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMeAsync();
        Console.WriteLine($"Hello, World! I am user {me.Id} and my name is {me.FirstName}.");

        / *bool running = true;

        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true; //true: 不导致退出。false: 会导致退出
            running = false;
            Console.WriteLine("You have Press Ctrl+C");
        };

        while (running)
        {
            await Task.Delay(10, cts.Token);
        }* /
    }*/
}