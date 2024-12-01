using LLama.Common;
using LLama;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLama.Transformers;
using static LLama.LLamaTransforms;

namespace LLamaServer.Core
{
    public class LLamaService : IDisposable
    {
        private readonly LLamaWeights _model;
        private readonly LLamaContext _context;

        public LLamaService(string modelPath, int contextSize = 1024, int gpuLayerCount = 10)
        {
            // Set up model parameters and load the model
            var parameters = new ModelParams(modelPath)
            {
                ContextSize = (uint?)contextSize,
                GpuLayerCount = gpuLayerCount
            };

            _model = LLamaWeights.LoadFromFile(parameters);
            _context = _model.CreateContext(parameters);
        }

        public LLamaWeights Model { get { return _model; } }

        // Method to create and return a new InteractiveExecutor instance
        public InteractiveExecutor CreateExecutor()
        {
            return new InteractiveExecutor(_context);
        }

        // Dispose of resources
        public void Dispose()
        {
            _context?.Dispose();
            _model?.Dispose();
        }

        public async Task doit()
        {
            string modelPath = @"C:\Users\dmitr\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\OllamaGGUF\Meta-Llama-3.1-8B-Instruct-Q6_K.gguf";

            var parameters = new ModelParams(modelPath)
            {
                ContextSize = 1024, // The longest length of chat as memory.
                GpuLayerCount = 20 // How many layers to offload to GPU. Please adjust it according to your GPU memory.
            };
            using var model = LLamaWeights.LoadFromFile(parameters);
            using var context = model.CreateContext(parameters);
            var executor = new InteractiveExecutor(context);

            var chatHistory = new ChatHistory();
            chatHistory.AddMessage(AuthorRole.System, "Respond directly and concisely to user questions without " +
                "repeating previous dialogue or adding unnecessary context.");
            chatHistory.AddMessage(AuthorRole.User, "Hello, Assistant.");
            chatHistory.AddMessage(AuthorRole.Assistant, "Hello. How may I help you today?");

            ChatSession session = new(executor, chatHistory);

            InferenceParams inferenceParams = new InferenceParams()
            {
                TokensKeep = 56,
                MaxTokens = 1024, // No more than 256 tokens should appear in answer. Remove it if antiprompt is enough for control.
                AntiPrompts = new List<string> { "" } // Stop generation once antiprompts appear.
            };

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("The chat session has started.\nUser: ");

            Console.ForegroundColor = ConsoleColor.Green;

            string userInput = Console.ReadLine() ?? "";

            while (userInput != "exit")
            {
                await foreach ( // Generate the response streamingly.
                    var text
                    in session.ChatAsync(
                        new ChatHistory.Message(AuthorRole.User, userInput),
                        inferenceParams))
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(text);
                }
                Console.ForegroundColor = ConsoleColor.Green;
                userInput = Console.ReadLine() ?? "";
            }
        }
    }
}
