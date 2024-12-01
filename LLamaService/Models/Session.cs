using LLama.Common;
using LLama;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLama.Abstractions;
using LLama.Transformers;
using LLama.Sampling;

namespace LLamaServer.Models
{
    public class Session
    {
        private readonly InteractiveExecutor _executor;
        private readonly ChatHistory _chatHistory;
        private readonly ChatSession _session;
        private readonly LLamaWeights _model;

        public Session(InteractiveExecutor executor, LLamaWeights model)
        {
            _model = model;

            _executor = executor;
            var chatHistoryJson = File.ReadAllText("Config/chat-start-history.json");

            _chatHistory = ChatHistory.FromJson(chatHistoryJson) ?? new ChatHistory();

            _session = new(_executor, _chatHistory);
        }

        // Simulates processing a message with the LLama model and returns a stream of response parts
        public async IAsyncEnumerable<string> ProcessMessageAsync(string message)
        {
            Console.WriteLine("DEBUG FROM LLAMA SERVER SESSION :" + message);

            // add the default templator.
            _session.WithHistoryTransform(new PromptTemplateTransformer(_model, withAssistant: true));

            // Add a transformer to eliminate printing the end of turn tokens, llama 3 specifically has an odd LF that gets printed sometimes
            _session.WithOutputTransform(new LLamaTransforms.KeywordTextOutputStreamTransform(
                [_model.Tokens.EndOfTurnToken ?? "User:", "�"],
                redundancyLength: 5));

            var inferenceParams = new InferenceParams
            {
                SamplingPipeline = new DefaultSamplingPipeline
                {
                    Temperature = 0.6f
                },

                MaxTokens = -1, // keep generating tokens until the anti prompt is encountered
                AntiPrompts = [_model.Tokens.EndOfTurnToken ?? "User:"] // model specific end of turn string (or default)
            };

            await foreach (var text in _session.ChatAsync(
                new ChatHistory.Message(AuthorRole.User, message),
                inferenceParams))
            {
                yield return text;
            }
        }
    }
}
