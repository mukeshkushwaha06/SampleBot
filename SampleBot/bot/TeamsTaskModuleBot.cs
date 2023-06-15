using AdaptiveCards.Templating;
using bot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace bot
{
    public class TeamsTaskModuleBot : TeamsActivityHandler
    {     
        public TeamsTaskModuleBot()
        {
            
        }
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var card = await CreateInitialCardAttachment(turnContext);          
            await turnContext.SendActivityAsync(MessageFactory.Attachment(card), cancellationToken);
        }

        protected override async Task<InvokeResponse> OnInvokeActivityAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            var activityValue = turnContext.Activity.Value.ToString();

            var data = JsonConvert.DeserializeObject<InitialSequentialCard>(activityValue);

            string verb = data.action.verb;

            AdaptiveCardInvokeResponse response =new AdaptiveCardInvokeResponse();
            switch (verb)
            {
                case "initialRefresh":
                     response = await GetInvokeResponseHome(turnContext,data);
                    break;
                case "success":
                    response =await GetInvokeResponseSuccess(turnContext, data);
                    break;
            }

            return CreateInvokeResponse(response);
        }

        #region Adaptive card data creation
        private async Task<Attachment> CreateInitialCardAttachment<T>(ITurnContext<T> turnContext) where T : IActivity
        {
            // combine path for cross platform support
            string[] paths = { ".", "Resources", "initial.json" };
            var adaptiveCardJson = File.ReadAllText(Path.Combine(paths));

            AdaptiveCardTemplate template = new AdaptiveCardTemplate(adaptiveCardJson);

            var member = await TeamsInfo.GetMemberAsync(turnContext, turnContext.Activity.From.Id);

            var payloadData = new
            {
                createdByID = member.Id,
                createdBy=member?.Name
            };

            var cardJsonstring = template.Expand(payloadData, _ => string.Empty);

            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(cardJsonstring),
            };
            return adaptiveCardAttachment;
        }


        private async Task<Attachment> CreateHomeCardAttachment<T>(ITurnContext<T> turnContext, InitialSequentialCard data) where T : IActivity
        {
            // combine path for cross platform support
            string[] paths = { ".", "Resources", "homeCard.json" };

            var member = await TeamsInfo.GetMemberAsync(turnContext, turnContext.Activity.From.Id);
            var adaptiveCardJson = File.ReadAllText(Path.Combine(paths));

            AdaptiveCardTemplate template = new AdaptiveCardTemplate(adaptiveCardJson);
            var payloadData = new
            {
                Org="User Input:"
            };

            var cardJsonstring = template.Expand(payloadData, _ => string.Empty);

            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(cardJsonstring),
            };
            return adaptiveCardAttachment;
        }

        public async Task<AdaptiveCardInvokeResponse> GetInvokeResponseHome<T>(ITurnContext<T> turnContext, InitialSequentialCard data) where T : IActivity
        {
            var attachment = await CreateHomeCardAttachment(turnContext, data);
            return new AdaptiveCardInvokeResponse
            {
                StatusCode = 200,
                Value = attachment.Content,
                Type = "application/vnd.microsoft.card.adaptive",
            };
        }


        private async Task<Attachment> CreateSuccessCardAttachment<T>(ITurnContext<T> turnContext, InitialSequentialCard data) where T : IActivity
        {
            // combine path for cross platform support
            string[] paths = { ".", "Resources", "success.json" };

            var member = await TeamsInfo.GetMemberAsync(turnContext, turnContext.Activity.From.Id);
            var adaptiveCardJson = File.ReadAllText(Path.Combine(paths));

            var cardData = GetCardData(data.action.data);

            AdaptiveCardTemplate template = new AdaptiveCardTemplate(adaptiveCardJson);
            var payloadData = new
            {
                Name = cardData.Name
            };

            var cardJsonstring = template.Expand(payloadData, _ => string.Empty);

            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(cardJsonstring),
            };
            return adaptiveCardAttachment;
        }

        private CardData GetCardData(JObject jObj)
        {
            var card = new CardData();
            var isLatestDataAvailable = typeof(CardData).GetProperties().Any(_ => jObj.ContainsKey(_.Name));

            if(isLatestDataAvailable)
            {
                card.Name= jObj["Name"]?.ToString() ?? string.Empty;
            }

            return card;
        }

        private async Task<AdaptiveCardInvokeResponse> GetInvokeResponseSuccess<T>(ITurnContext<T> turnContext, InitialSequentialCard data) where T : IActivity
        {
            var attachment = await CreateSuccessCardAttachment(turnContext,data);
            return new AdaptiveCardInvokeResponse
            {
                StatusCode = 200,
                Value = attachment.Content,
                Type = "application/vnd.microsoft.card.adaptive",
            };
        }

        #endregion
    }
}
