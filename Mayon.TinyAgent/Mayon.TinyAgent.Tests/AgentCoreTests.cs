using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mayon.TinyAgent;
using Xunit;

namespace Mayon.TinyAgent.Tests
{
    public class AgentCoreTests
    {
        private static void ResetStaticFields()
        {
            var type = typeof(AgentCore);
            // Fields to reset
            var staticFieldNames = new[]
            {
                "_agentTasks",
                "_modelPath",
                "_expectedOutput",
                "_expectedOutputTempl",
                "_kichStartSystemMsg",
                "_toolsJson",
                "_ragOrContextData",
                "inferenceParams",
                "session"
            };

            foreach (var name in staticFieldNames)
            {
                var f = type.GetField(name, BindingFlags.Static | BindingFlags.NonPublic);
                if (f != null)
                {
                    // set reference types to null; leave value types default
                    if (f.FieldType.IsValueType)
                    {
                        var defaultValue = Activator.CreateInstance(f.FieldType);
                        f.SetValue(null, defaultValue);
                    }
                    else
                    {
                        f.SetValue(null, null);
                    }
                }
            }
        }

        [Fact]
        public async Task AddSysMessage_SetsPrivateField()
        {
            ResetStaticFields();
            var core = new AgentCore();
            await core.AddSysMessage("initial-system-msg");

            var f = typeof(AgentCore).GetField("_kichStartSystemMsg", BindingFlags.Static | BindingFlags.NonPublic);
            var val = f.GetValue(null) as string;
            Assert.Equal("initial-system-msg", val);
        }

        [Fact]
        public async Task AddToolJson_SetsToolsJsonField_WithPrefix()
        {
            ResetStaticFields();
            var core = new AgentCore();
            var json = "{\"tool\":\"x\"}";
            await core.AddToolJson(json);

            var f = typeof(AgentCore).GetField("_toolsJson", BindingFlags.Static | BindingFlags.NonPublic);
            var val = f.GetValue(null) as string;
            Assert.NotNull(val);
            Assert.Contains("Tools Data:", val);
            Assert.Contains(json, val);
        }

        [Fact]
        public async Task AddContextData_SetsRagOrContextDataField_WithPrefix()
        {
            ResetStaticFields();
            var core = new AgentCore();
            var ctx = "some context data";
            await core.AddContextData(ctx);

            var f = typeof(AgentCore).GetField("_ragOrContextData", BindingFlags.Static | BindingFlags.NonPublic);
            var val = f.GetValue(null) as string;
            Assert.NotNull(val);
            Assert.Contains("Context Data:", val);
            Assert.Contains(ctx, val);
        }

        [Fact]
        public async Task AddAgentRolesTask_SetsAgentTasksField()
        {
            ResetStaticFields();
            var core = new AgentCore();
            var tasks = "do important work";
            await core.AddAgentRolesTask(tasks);

            var f = typeof(AgentCore).GetField("_agentTasks", BindingFlags.Static | BindingFlags.NonPublic);
            var val = f.GetValue(null) as string;
            Assert.NotNull(val);
            Assert.Contains("Your primary role's tasks are as follows:", val);
            Assert.Contains(tasks, val);
        }

        [Fact]
        public async Task ExpectedOutput_SetsExpectedOutputAndTemplate()
        {
            ResetStaticFields();
            var core = new AgentCore();
            await core.ExpectedOutput("text", "{schema}");

            var fFormat = typeof(AgentCore).GetField("_expectedOutput", BindingFlags.Static | BindingFlags.NonPublic);
            var fTempl = typeof(AgentCore).GetField("_expectedOutputTempl", BindingFlags.Static | BindingFlags.NonPublic);

            Assert.Equal("text", fFormat.GetValue(null) as string);
            Assert.Equal("{schema}", fTempl.GetValue(null) as string);
        }

        [Fact]
        public async Task Setup_WithEmptyModelPath_DoesNotCreateSession()
        {
            ResetStaticFields();
            var core = new AgentCore();

            // call Setup with empty path -> method should return early and not create a session
            await core.Setup("", contextSize: 512, gpuLayers: 0, maxTokens: 32, supressAIComments: true);

            var sessionField = typeof(AgentCore).GetField("session", BindingFlags.Static | BindingFlags.NonPublic);
            var sessionVal = sessionField.GetValue(null);
            Assert.Null(sessionVal);
        }

        [Fact]
        public async Task Chat_WhenSessionIsNull_ReturnsModelNotConfiguredMessage()
        {
            ResetStaticFields();
            var core = new AgentCore();
            var result = await core.Chat("hello");
            Assert.Equal("The model is not configured correctly. Check your model path and settings to ensure correct operation.", result);
        }

        [Fact]
        public async Task ChatStream_WhenSessionIsNull_YieldsNoItems()
        {
            ResetStaticFields();
            var core = new AgentCore();

            var items = new List<string>();
            await foreach (var t in core.ChatStream("hello"))
            {
                items.Add(t);
            }

            Assert.Empty(items);
        }
    }
}