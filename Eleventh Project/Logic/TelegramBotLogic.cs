using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Eleventh_Project.Logic
{
    public class TelegramBotLogic
    {
        private Message _message;
        private long _chatId;
        private CancellationToken _cancellationToken;
        private ITelegramBotClient _botClient;
        private HttpClient _client = new HttpClient();
        private string[] _currencies;
        public TelegramBotLogic(Message message, long chatId, CancellationToken cancellationToken, ITelegramBotClient botClient, string[] currencies)
        {
            _message = message;
            _chatId = chatId;
            _cancellationToken = cancellationToken;
            _botClient = botClient;
            _currencies = currencies;
        }

        public async Task MainLogicAsync()
        {
            var dateAndCurrency = _message.Text.Split(' ');
            DateTime convertedDate = ValidateDateTime(dateAndCurrency[0]);
            if (convertedDate == new DateTime())
            {
                await SendWrongDateFormatAsync();
                return;
            }

            var bottomBoundDate = new DateTime(2018, 8, 1);
            if (dateAndCurrency.Length != 2)
            {
                await SendWrongDateFormatAsync();
                return;
            }

            await CheckDateTimeAsync(bottomBoundDate, convertedDate);

            bool hasCurrancy = CheckHasCurrency(dateAndCurrency[1]);

            if (!hasCurrancy)
            {
                await SendIncorrectCurrencyAsync();
                return;
            }

            var newBank = await ProcessPrivatBankAsync(convertedDate);
            await SearchForCarrency(newBank, dateAndCurrency[1]);
        }

        private async Task SendIncorrectCurrencyAsync()
        {
            Message result = await _botClient.SendTextMessageAsync(
                chatId: _chatId,
                text:
                $"Incorrect currency! List of currencies: {string.Join(" ", _currencies)}",
                cancellationToken: _cancellationToken
            );
        }

        private async Task SearchForCarrency(PrivatBankModels bank, string currency)
        {
            string upperCurrency = currency.ToUpper();
            foreach (var val in bank.ExchangeRate)
            {
                if (val.Currency == upperCurrency)
                {
                    await SendResultAsync(val);
                }
            }
        }

        private async Task SendResultAsync(ExchangeRateModels val)
        {
            string resultText = ChooseExchangeRate(val);

            Console.WriteLine(resultText);
            Message result = await _botClient.SendTextMessageAsync(
                chatId: _chatId,
                text: resultText,
                cancellationToken: _cancellationToken
            );
            return;
        }

        private static string ChooseExchangeRate(ExchangeRateModels val)
        {
            string resultText = String.Empty;
            if (val.PurchaseRate != 0)
            {
                resultText =
                    $"Selected currency: {val.Currency}, Purchase rate: {val.PurchaseRate} UAH, Sale rate: {val.SaleRate} UAH";
            }

            if (val.PurchaseRate == 0)
            {
                resultText =
                    $"Selected currency: {val.Currency}, Purchase rate: {val.PurchaseRateNB} UAH, Sale rate: {val.SaleRateNB} UAH";
            }

            return resultText;
        }

        private bool CheckHasCurrency(string currencyToCheck)
        {
            bool hasCurrency = false;
            string upperCurrencyToCheck = currencyToCheck.ToUpper();
            foreach (var currency in _currencies)
            {
                if (currency == upperCurrencyToCheck)
                {
                    hasCurrency = true;
                }
            }
            return hasCurrency;
        }

        private async Task CheckDateTimeAsync(DateTime bottomBoundDate, DateTime convertedDate)
        {
            if (convertedDate > DateTime.Now.Date)
            {
                Message futureTime = await _botClient.SendTextMessageAsync(
                    chatId: _chatId,
                    text: "We live in the present. You should write correct date.",
                    cancellationToken: _cancellationToken
                );
                return;
            }
            if (convertedDate < bottomBoundDate)
            {
                Message bottomBoundEror = await _botClient.SendTextMessageAsync(
                    chatId: _chatId,
                    text: "We have statistic only for last 4 years.",
                    cancellationToken: _cancellationToken
                );
                return;
            }
        }

        private DateTime ValidateDateTime(string dateToValidate)
        {
            string formatPrivat = "dd.MM.yyyy";
            string formatSlesh = "dd/MM/yyyy";
            if (DateTime.TryParseExact(dateToValidate, formatPrivat, CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out DateTime dateTime))
            {
                return dateTime;
            }
            if (DateTime.TryParseExact(dateToValidate, formatSlesh, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
            {
                string[] splitted = dateToValidate.Split("/");
                dateTime = DateTime.Parse($"{splitted[0]}.{splitted[1]}.{splitted[2]}");
                return dateTime;
            }

            dateTime = new DateTime();
            return dateTime;
        }

        private async Task<PrivatBankModels> ProcessPrivatBankAsync(DateTime dateTime)
        {
            const string startUrl = "https://api.privatbank.ua/p24api/exchange_rates?";

            var bulider = new UriBuilder(startUrl);
            bulider.Query = $"json&date={dateTime}";
            var url = bulider.ToString();
            var res = await _client.GetAsync(url);

            return await res.Content.ReadFromJsonAsync<PrivatBankModels>();
        }

        private async Task SendWrongDateFormatAsync()
        {
            Message dateTimeValidationError = await _botClient.SendTextMessageAsync(
                chatId: _chatId,
                text: "Wrong date format! You have to write like that: 'DD.MM.YYYY USD' or like this 'DD/MM/YYYY USD' without ''",
                cancellationToken: _cancellationToken
            );
        }
    }
}
