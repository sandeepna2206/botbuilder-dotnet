﻿// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Recognizers
{
    /// <summary>
    /// IRecognizer implementation which uses regex expressions to identify intents
    /// </summary>
    public class RegexRecognizer : IRecognizer
    {
        /// <summary>
        /// Dictionary of patterns -> Intent names
        /// </summary>
        public Dictionary<string, string> Intents = new Dictionary<string, string>();
        
        public async Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // Process only messages
            if (turnContext.Activity.Type != ActivityTypes.Message)
            {
                return new RecognizerResult() { Text = turnContext.Activity.Text };
            }

            // Identify matched intents
            var utterance = turnContext.Activity.Text ?? string.Empty;

            var result = new RecognizerResult()
            {
                Text = utterance,
                Intents = new Dictionary<string, IntentScore>(),
            };

            foreach (var kv in Intents)
            {
                var intent = kv.Key;
                var regex = new Regex(kv.Value);

                var match = regex.Match(utterance);

                if (match.Success)
                {
                    // TODO length weighted match and multiple intents
                    result.Intents.Add(intent, new IntentScore() { Score = 1.0 });

                    // Check for named capture groups
                    var entities = new Dictionary<string, string>();
                    foreach (var name in regex.GetGroupNames())
                    {
                        if (!string.IsNullOrEmpty(match.Groups[name].Value))
                        {
                            entities.Add(name, match.Groups[name].Value);
                        }
                    }
                    result.Entities = JObject.FromObject(entities);
                }
            }

            return result;
        }

        public Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken) where T : IRecognizerConvert, new()
        {
            throw new NotImplementedException();
        }
    }
}