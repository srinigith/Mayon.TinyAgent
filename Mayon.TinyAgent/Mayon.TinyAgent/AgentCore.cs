using LLama;
using LLama.Common;
using System.Runtime.CompilerServices;

namespace Mayon.TinyAgent
{
    /// <summary>
    /// Core wrapper around a local LLama model that exposes a simple chat-style API.
    /// </summary>
    /// <remarks>
    /// Responsibilities:
    /// - Configure and load a local LLama model.
    /// - Build and maintain a chat session with an initial system prompt and history.
    /// - Provide synchronous and streaming chat entry points for user messages.
    /// 
    /// Notes:
    /// - This class holds static session and inference parameters so multiple instances will
    ///   share the same underlying model session.
    /// - The class is intentionally minimal and expects callers to call <see cref="Setup(string, bool)"/>
    ///   before use.
    /// </remarks>
    public class AgentCore
    {
        /// <summary>
        /// Default agent name used in system messages when no name is provided.
        /// </summary>
        private readonly string _agentName = "ChatBot";

        /// <summary>
        /// Default agent role used in system messages when no role is provided.
        /// </summary>
        private readonly string _agentRole = "Messenger";

        /// <summary>
        /// Optional role/task description exposed as a public static so it can be set externally.
        /// This text is added as a system message during session setup when present.
        /// </summary>
        private protected string? _agentTasks;

        /// <summary>
        /// Local filesystem path to the LLama model files. Populated via <see cref="Setup(string, bool)"/>.
        /// This value is used to construct <see cref="ModelParams"/> when loading model weights.
        /// </summary>
        private protected string? _modelPath;

        /// <summary>
        /// Expected output format (for example "text" or "json"). Added to system messages on setup.
        /// This is used to instruct the model about the format of the response.
        /// </summary>
        private protected string? _expectedOutput;

        /// <summary>
        /// Optional output template or schema string that supplements <see cref="_expectedOutput"/>.
        /// When provided it is appended to the system prompt to better constrain the model output.
        /// </summary>
        private protected string? _expectedOutputTempl;

        /// <summary>
        /// Optional initial system message that is injected into the chat history on setup.
        /// Use <see cref="AddSysMessage(string)"/> to populate this value.
        /// </summary>
        private protected string? _kichStartSystemMsg;

        /// <summary>
        /// Optional tools description (JSON or text) that is injected into the chat history on setup.
        /// Set via <see cref="AddToolJson(string)"/> to inform the model about available tool metadata.
        /// </summary>
        private protected string? _toolsJson;

        /// <summary>
        /// Optional context or retrieval-augmented generation (RAG) data that is injected into the chat
        /// history during setup. Use <see cref="AddContextData(string)"/> to populate this field.
        /// </summary>
        private protected string? _ragOrContextData;

        /// <summary>
        /// Inference options used for generation (max tokens, anti-prompts, etc.).
        /// Configured during <see cref="Setup(string, bool)"/>.
        /// </summary>
        private protected InferenceParams? inferenceParams;

        /// <summary>
        /// Active chat session. Created and initialized in <see cref="Setup(string, bool)"/>.
        /// The session is reused across instances of <see cref="AgentCore"/> because it is static.
        /// </summary>
        private protected ChatSession? session;

        /// <summary>
        /// Create a new <see cref="AgentCore"/> instance.
        /// </summary>
        /// <param name="agentName">Optional agent name. If empty the default name is used.</param>
        /// <param name="agentRole">Optional agent role. If empty the default role is used.</param>
        public AgentCore(string agentName = "", string agentRole = "")
        {
            if (!string.IsNullOrWhiteSpace(agentName))
            {
                _agentName = agentName;
            }
            if (!string.IsNullOrWhiteSpace(agentRole))
            {
                _agentRole = agentRole;
            }
        }

        /// <summary>
        /// Adds an initial system message that will be included in the chat history when <see cref="Setup"/> is called.
        /// </summary>
        /// <param name="SystemMsg">The system message text to add. If null or whitespace, the call is ignored.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task AddSysMessage(string SystemMsg)
        {
            if (!string.IsNullOrWhiteSpace(SystemMsg))
            {
                _kichStartSystemMsg = SystemMsg;
            }
        }

        /// <summary>
        /// Adds tool metadata (for example a JSON description of available tools) that will be included
        /// in the chat history on <see cref="Setup"/>.
        /// </summary>
        /// <param name="toolsJson">Tool description in JSON or text form. Ignored if null or whitespace.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task AddToolJson(string toolsJson)
        {
            if (!string.IsNullOrWhiteSpace(toolsJson))
            {
                _toolsJson = $"""
                    Tools Data:
                        {toolsJson}
                    """;
            }
        }

        /// <summary>
        /// Adds contextual or RAG (retrieval-augmented generation) data that will be included
        /// as a system message on session setup.
        /// </summary>
        /// <param name="RagOrContextData">Contextual data text. If null or whitespace, the call is ignored.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task AddContextData(string RagOrContextData)
        {
            if (!string.IsNullOrWhiteSpace(RagOrContextData))
            {
                _ragOrContextData = $"""
                    Context Data:
                        {RagOrContextData}
                    """;
            }
        }

        public async Task AddRagDocument(FileInfo file)
        {
            if(file.Extension == "txt")
            {

            }
        }

        public async Task AddRagURL(Uri path)
        {
            if (path.AbsoluteUri != "")
            {
                //Web Scrapping
            }
        }

        /// <summary>
        /// Placeholder to add tool endpoints (e.g., OpenAPI host + path). Not implemented.
        /// </summary>
        /// <param name="openApiHost">Tool host base URL.</param>
        /// <param name="toolPath">Tool endpoint path or identifier.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method currently has no implementation and can be extended to register remote tool URIs.
        /// Implementers may store the provided URIs and expose them to the model via system messages.
        /// </remarks>
        public async Task AddToolOpenApiUris(string openApiHost, string toolPath)
        {

        }

        /// <summary>
        /// Sets the agent's role/task description text which will be added to the initial system messages.
        /// </summary>
        /// <param name="tasks">A textual description of the agent's tasks.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task AddAgentRolesTask(string tasks)
        {
            _agentTasks =
                $"""
                    Your primary role's tasks are as follows:
                    {tasks}
                """;
        }

        /// <summary>
        /// Sets the expected output format and optional template used to instruct the model's response format.
        /// </summary>
        /// <param name="format">The expected output format (for example "text" or "json"). Defaults to "json".</param>
        /// <param name="template">An optional output template or schema that will be included in system messages.</param>
        /// <returns>A <see cref="Task"/> that completes after the values are stored.</returns>
        /// <remarks>
        /// The provided values are stored in the static fields <c>_expectedOutput</c> and
        /// <c>_expectedOutputTempl</c>. These are read during <see cref="Setup(string, bool)"/>
        /// to add guidance to the model's system prompt. This method performs no validation of
        /// the format or template; callers should ensure they provide valid strings suitable
        /// for inclusion in a system prompt.
        /// </remarks>
        public async Task ExpectedOutput(string format = "json", string template = "")
        {
            _expectedOutput = format;
            _expectedOutputTempl = template;
        }

                
        /// <summary>
        /// Configures and loads the LLama model from the given local path, and initializes the chat session.
        /// </summary>
        /// <param name="modelPath">Local filesystem path to the LLama model files. If null or whitespace this method returns without loading.</param>
        /// <param name="supressAIComments">When true, adds a system instruction to ask the model to return only the expected output and no extra commentary.</param>
        /// <returns>A task representing the asynchronous setup operation.</returns>
        /// <remarks>
        /// - This method prepares <see cref="ModelParams"/> and <see cref="InferenceParams"/>, loads model weights,
        ///   creates a context and an <see cref="InteractiveExecutor"/>, then constructs a <see cref="ChatSession"/>.
        /// - System messages assembled from fields like <c>_agentName</c>, <c>_agentRole</c>, <c>_agentTasks</c>,
        ///   and any added context/tools are injected into the initial chat history.
        /// - Callers should ensure the model files exist at <paramref name="modelPath"/> and that resources are sufficient
        ///   for the configured <see cref="ModelParams"/> (for example CPU-only or GPU).
        /// </remarks>
        public async Task Setup(string modelPath, int contextSize = 1024, int gpuLayers = 0, int maxTokens = 256, bool supressAIComments = true)
        {
            _modelPath = modelPath;
            try
            {
                if (string.IsNullOrWhiteSpace(_modelPath))
                {
                    return;
                }

                var parameters = new ModelParams(_modelPath) //Loading model from local path
                {
                    ContextSize = (uint)contextSize,
                    GpuLayerCount = gpuLayers //0 for CPU-only
                };

                inferenceParams = new InferenceParams()
                {
                    MaxTokens = maxTokens,
                    // Add "User:" so the model stops before trying to simulate a new user message
                    AntiPrompts = new List<string> { "User:", "Assistant", "<|end_of_turn|>" }
                };

                // Load the model weights
                var weights = await LLamaWeights.LoadFromFileAsync(parameters);

                // Create a context and executor
                var context = weights.CreateContext(parameters);

                // Create the executor as you already did
                var executor = new InteractiveExecutor(context);

                // Initialize a ChatHistory (this is what ChatAsync actually updates)
                var history = new ChatHistory();

                if (!string.IsNullOrWhiteSpace(_agentName))
                {
                    string agentNameMsg = $"As a bot agent, your name is {_agentName}.";
                    history.AddMessage(AuthorRole.System, agentNameMsg);
                }

                if (!string.IsNullOrWhiteSpace(_agentRole))
                {
                    string agentRoleMsg = $"You are a bot agent and your role is {_agentRole}.";
                    history.AddMessage(AuthorRole.System, agentRoleMsg);
                }

                // Adding system messages
                if (!string.IsNullOrWhiteSpace(_kichStartSystemMsg))
                {
                    history.AddMessage(AuthorRole.System, _kichStartSystemMsg);
                }

                if (!string.IsNullOrWhiteSpace(_ragOrContextData))
                {
                    history.AddMessage(AuthorRole.System, _ragOrContextData);
                }

                if (!string.IsNullOrWhiteSpace(_toolsJson))
                {
                    history.AddMessage(AuthorRole.System, _toolsJson);
                }

                if (!string.IsNullOrWhiteSpace(_agentTasks))
                {
                    history.AddMessage(AuthorRole.System, _agentTasks);
                }

                if (!string.IsNullOrWhiteSpace(_expectedOutput))
                {
                    string output = $"""
                            Expected output format: {_expectedOutput}
                        """;
                    if (!string.IsNullOrWhiteSpace(_expectedOutputTempl))
                    {
                        output += $"""
                            with the template: {_expectedOutputTempl}
                        """;
                    }

                    history.AddMessage(AuthorRole.System, output);
                }

                if (supressAIComments)
                {
                    // Clear, grammatically-correct instruction to the model to return only the expected output
                    history.AddMessage(AuthorRole.System, $"Return only the {_expectedOutput} output. Do not include any additional comments or notes.");
                }

                // Create the ChatSession (This is where ChatAsync lives!)
                session = new ChatSession(executor, history);
                session.WithOutputTransform(new LLamaTransforms.KeywordTextOutputStreamTransform(
                    new string[] { "User:", "Assistant", "<|end_of_turn|>" },
                    redundancyLength: 5
                ));
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Synchronously collects a single response from the chat session for the provided user input.
        /// </summary>
        /// <param name="input">The user message to send. If null or whitespace, an explanatory string is returned.</param>
        /// <returns>A task that resolves to the concatenated response string from the model.</returns>
        /// <remarks>
        /// - If the static <see cref="session"/> is null this returns an empty string.
        /// - The method reads tokens from <see cref="ChatSession.ChatAsync"/> until an anti-prompt is encountered.
        /// - The token stream is concatenated into a single string. For large outputs consider using <see cref="ChatStream(string, CancellationToken)"/>.
        /// </remarks>
        public async Task<string> Chat(string input)
        {
            try
            {
                string respons = "";
                if (session != null)
                {
                    if (string.IsNullOrWhiteSpace(input)) return "Unable to read the message.";
                    await foreach (var token in session.ChatAsync(new ChatHistory.Message(AuthorRole.User, input), inferenceParams))
                    {
                        if (token.Contains("User:")) break;
                        //Console.Write(token);
                        respons += token;
                    }
                }
                else
                {
                    respons = "The model is not configured correctly. Check your model path and settings to ensure correct operation.";
                }
                return respons;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Streams tokens from the chat session as they are produced by the model.
        /// </summary>
        /// <param name="input">The user message to send. If null or whitespace the returned sequence yields an explanatory string.</param>
        /// <param name="cancellationToken">Cancellation token to stop streaming early.</param>
        /// <returns>An async enumerable producing tokens (strings) emitted by the model.</returns>
        /// <remarks>
        /// - The stream yields tokens until an anti-prompt token (for example "User:") is seen or the session completes.
        /// - The caller should handle cancellation via the provided token.
        /// - Use this method when the consumer needs incremental output (for example UI streaming or progressive processing).
        /// </remarks>
        public async IAsyncEnumerable<string> ChatStream(string input, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            string respons = "";
            if (session != null)
            {
                if (string.IsNullOrWhiteSpace(input)) { yield return "Unable to read the message."; yield break; }
                await foreach (var token in session.ChatAsync(new ChatHistory.Message(AuthorRole.User, input), inferenceParams))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (token.Contains("User:")) break;
                    yield return token;
                }
            }
        }
    }
}
