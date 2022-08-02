using System;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Security.Authentication.ExtendedProtection;
using System.Text.Json;
using Eleventh_Project.Logic;
using Microsoft.Extensions.Configuration;
using JsonException = Newtonsoft.Json.JsonException;
using Microsoft.Extensions.DependencyInjection;
using Nito.AsyncEx;

namespace Eleventh_Project
{
    public class Project
    {
        private static IConfiguration Configuration;
        private static string[] _currencies;
        public static async Task Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json")
                .Build();

            serviceCollection.AddSingleton<IConfiguration>(Configuration);

            var botClient = new TelegramBotClient(Configuration.GetSection("TelegramToken").Value);
            var section = Configuration.GetSection("Currencies");
            _currencies = section.Get<string[]>();
            var me = await botClient.GetMeAsync();
            using var cts = new CancellationTokenSource();
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };
            botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
                );
            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();

            cts.Cancel();
        }

        private async static Task SendStartingMessage(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            Message start = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text:
                $"Welcome to Currency Exchange Bot. Instruction: You have to write like that: 'DD.MM.YYYY USD' without ''. List of currencies: {string.Join(" ", _currencies)}",
            cancellationToken: cancellationToken
            );
        }

        async static Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message)
            {
                return;
            }

            if (message.Text is not { } messageText)
            {
                return;
            }

            var chatId = message.Chat.Id;

            Console.WriteLine(
                $"Received a '{messageText}' message in chat {chatId} from user: {update.Message.From.FirstName} {update.Message.From.Username}.");

            if (message.Text == "/start")
            {
                await SendStartingMessage(botClient, chatId, cancellationToken);
                return;
            }
            TelegramBotLogic telegramBotLogic = new TelegramBotLogic(message, chatId, cancellationToken, botClient, _currencies);
            await telegramBotLogic.MainLogicAsync();
        }

        static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
            CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
    }
}

