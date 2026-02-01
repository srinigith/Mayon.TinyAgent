
---

## API Summary

- `AgentCore(string agentName = "", string agentRole = "")`  
  Create agent (no model loading).

- `Task AddSysMessage(string systemMsg)`  
  Add a system message to be injected during `Setup`.

- `Task AddToolJson(string toolsJson)`  
  Add tool metadata (JSON or text).

- `Task AddContextData(string ragOrContextData)`  
  Add RAG/context data.

- `Task AddAgentRolesTask(string tasks)`  
  Add agent responsibilities/task description.

- `Task ExpectedOutput(string format = "json", string template = "")`  
  Specify expected output format and optional template/schema.

- `Task Setup(string modelPath, int contextSize = 1024, int gpuLayers = 0, int maxTokens = 256, bool supressAIComments = true)`  
  Load model, configure params, and create the chat session. Must be called before `Chat` or `ChatStream`.

- `Task<string> Chat(string input)`  
  Get a single concatenated response. Stops on anti-prompt tokens.

- `IAsyncEnumerable<string> ChatStream(string input, CancellationToken cancellationToken = default)`  
  Stream tokens as produced. Cancel via `CancellationToken`.

---

## Configuration & Tuning

- `ModelParams.ContextSize` — controls context window. Larger values increase memory usage.
- `ModelParams.GpuLayerCount` — number of layers run on GPU; set >0 for GPUs with appropriate backend.
- `InferenceParams.MaxTokens` — generated token limit.
- `InferenceParams.AntiPrompts` — tokens that stop generation (default includes `"User:"`, `"Assistant"`, `"<|end_of_turn|>"`).
- `supressAIComments` — when `true`, injects a system message to return only expected output (useful for structured responses).

---

## Gotchas & Best Practices

- `AgentCore` stores `session` and `inferenceParams` in static fields — multiple `AgentCore` instances share the same session. Reinitialize carefully if concurrency or multiple models are needed.
- Always call `Setup(...)` before `Chat(...)` or `ChatStream(...)`.
- For production or long-running apps, handle model resource usage and dispose patterns appropriately (the current wrapper does not expose explicit model disposal).
- If you see extra commentary in outputs, verify `_expectedOutput` is set and `supressAIComments` is enabled.
- If model loading fails, check `modelPath`, file permissions, and compatibility with `LLamaSharp` backend.

---

## Performance & Resource Notes

- CPU-only mode is slower and memory-intensive. For larger models prefer GPU backend and configure `GpuLayerCount`.
- Reduce `ContextSize` and `MaxTokens` to lower memory and runtime cost.
- Use streaming (`ChatStream`) to present partial results to UI without buffering full responses.

---

## Troubleshooting

- Model file not found — ensure correct path and supported model format.
- OutOfMemory — reduce `contextSize`, `maxTokens`, or use a smaller model / GPU.
- Slow performance — use GPU backend or smaller model; measure via profiling tools.

---

## Examples & Tests

See `DemoConsole/Program.cs` for a working example that demonstrates:
- Preparing system/context/tasks/tools messages.
- Calling `Setup`.
- Using `Chat` and `ChatStream`.

---

## Contributing

- PRs welcome. Follow repository guidelines and run unit tests (if present).
- Repository: `https://github.com/srinigith/Mayon.TinyAgent`

---

## License

See the repository for license information.
