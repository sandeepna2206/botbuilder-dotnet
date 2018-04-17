﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Ai.QnA
{
    /// <summary>
    /// Middleware for handling QnAMaker activity
    /// </summary>
    public class QnAMakerMiddleware : IMiddleware
    {
        public const string QnAMakerMiddlewareName = "QnAMakerMiddleware";
        public const string QnAMakerResultKey = "QnAMakerResult";
        public const string QnAMakerTraceType = "https://www.qnamaker.ai/schemas/trace";
        public const string QnAMakerTraceLabel = "QnAMaker Trace";
        private readonly QnAMaker _qnaMaker;
        private readonly QnAMakerMiddlewareOptions _options;

        /// <summary>
        /// Constructor for middleware that handles QnAMaker activity
        /// </summary>
        /// <param name="options"></param>
        /// <param name="httpClient">HttpClient used to talk to QnAMaker. If null, QnAMaker will initialize one.</param>
        public QnAMakerMiddleware(QnAMakerMiddlewareOptions options, HttpClient httpClient = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _qnaMaker = new QnAMaker(options, httpClient);
        }

        /// <summary>
        /// Handle incoming activity
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task OnTurn(ITurnContext context, MiddlewareSet.NextDelegate next)
        {
            if (context.Activity.Type == ActivityTypes.Message)
            {
                var messageActivity = context.Activity.AsMessageActivity();
                if (!string.IsNullOrEmpty(messageActivity.Text))
                {
                    var results = await _qnaMaker.GetAnswers(messageActivity.Text.Trim()).ConfigureAwait(false);
                    if (results == null)
                    {
                        throw new Exception("Call to QnAMaker failed.");
                    }

                    var traceInfo = new QnAMakerTraceInfo
                    {
                        QueryResults = results,
                        KnowledgeBaseId = _options.KnowledgeBaseId,
                        // leave out _options.SubscriptionKey, it is not public
                        ScoreThreshold = _options.ScoreThreshold,
                        Top = _options.Top,
                        StrictFilters = _options.StrictFilters,
                        MetadataBoost = _options.MetadataBoost,
                    };
                    var traceActivity = Activity.CreateTraceActivity(QnAMakerMiddlewareName, QnAMakerTraceType, traceInfo, QnAMakerTraceLabel);
                    await context.SendActivity(traceActivity).ConfigureAwait(false);
                    
                    if (results.Any())
                    {
                        if (!string.IsNullOrEmpty(_options.DefaultAnswerPrefixMessage))
                            await context.SendActivity(_options.DefaultAnswerPrefixMessage);

                        await context.SendActivity(results.First().Answer);

                        if (_options.EndActivityRoutingOnAnswer)
                            //Question is answered, don't keep routing
                            return;
                    }
                }
            }

            await next().ConfigureAwait(false);
        }
    }
}