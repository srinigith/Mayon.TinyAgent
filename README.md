
Notes:
- `Chat` concatenates tokens produced by the session until an anti-prompt is encountered.
- `ChatStream` yields tokens incrementally. Use a `CancellationToken` to stop early.

## APIs (summary)

- `AgentCore(string agentName = "", string agentRole = "")` — create instance.
- `Task AddSysMessage(string SystemMsg)` — add a system message for next `Setup`.
- `Task AddToolJson(string toolsJson)` — add tool metadata (formatted before being added).
- `Task AddContextData(string RagOrContextData)` — add RAG/context block.
- `Task AddAgentRolesTask(string tasks)` — set role/task description.
- `Task ExpectedOutput(string format = "json", string template = "")` — instruct expected response format.
- `Task Setup(string modelPath, int contextSize = 1024, int gpuLayers = 0, int maxTokens = 256, bool supressAIComments = true)` — load model and create session.
- `Task<string> Chat(string input)` — get a single concatenated response.
- `IAsyncEnumerable<string> ChatStream(string input, CancellationToken cancellationToken = default)` — stream tokens.

## Troubleshooting

- If `Setup` is called with an empty `modelPath` the method returns early and no session will be created. Verify the path.
- If `Chat` returns:  
  `"The model is not configured correctly. Check your model path and settings to ensure correct operation."`  
  it indicates `Setup` did not complete successfully or the session was not created.
- If your model requires GPU layers, set `gpuLayers` > 0 during `Setup`.
- For long synchronous outputs, prefer `ChatStream` to avoid large string concatenation overhead.

## Testing

- Run tests from the command line:
  - `dotnet test`
- In Visual Studio:
  - Use __Test Explorer__ to run / debug tests.

Unit tests included in `Mayon.TinyAgent.Tests` exercise configuration helpers (e.g., `AddSysMessage`, `AddToolJson`, `ExpectedOutput`) and behavior when no session exists.

## Debugging in Visual Studio

- Set breakpoints in `AgentCore.cs` and use __Debug > Start Debugging__ or __Debug > Step Into__.
- Use the __Output__ and __Diagnostics Tools__ windows to inspect runtime information.
- If you need to inspect static or private fields during tests, use the Immediate window or write test helpers to expose state.

## Extension points

- `AddToolOpenApiUris`, `AddRagDocument`, and `AddRagURL` are placeholders you can implement to:
  - Register remote tool endpoints or OpenAPI metadata.
  - Ingest text files or URLs for RAG data.
- You can customize `InferenceParams` or `ModelParams` prior to session creation to tune generation quality and resources.

## Security & resource guidance

- Model weights can be large; ensure adequate disk space.
- CPU-only models require sufficient memory. For GPU acceleration ensure compatible drivers and CUDA/cuDNN are installed.
- Never check large model files into source control.

## Contributing

- Follow repository coding conventions and add tests for new behavior.
- Run `dotnet test` before submitting changes.
- Use PR descriptions to explain changes to model handling or session lifetimes.

If you want, I can also:
- Add a short `Program.cs` example that compiles out-of-the-box (with stubs) for quick local testing.
- Create unit tests that cover additional behaviors (e.g., verifying that `Setup` adds the expected system messages to the `ChatHistory` if internals are made accessible).
