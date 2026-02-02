using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mayon.TinyAgent;
using Xunit;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Mayon.TinyAgent.Tests
{
    [TestClass]
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
                var f = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
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

        [TestMethod]
        public async Task AddSysMessage_SetsPrivateField()
        {
            //ResetStaticFields();
            var core = new AgentCore("tester", "tester");
            await core.AddSysMessage("initial-system-msg");

            var f = typeof(AgentCore).GetField("_kichStartSystemMsg", BindingFlags.Instance | BindingFlags.NonPublic);
            var val = f?.GetValue(core) as string;
            Xunit.Assert.Equal("initial-system-msg", val);
        }

        [TestMethod]
        public async Task AddToolJson_SetsToolsJsonField_WithPrefix()
        {
            //ResetStaticFields();
            var core = new AgentCore();
            var json = "{\"tool\":\"x\"}";
            await core.AddToolJson(json);

            var f = typeof(AgentCore).GetField("_toolsJson", BindingFlags.Instance | BindingFlags.NonPublic);
            var val = f.GetValue(core) as string;
            Assert.IsNotNull(val);
            Assert.Contains("Tools Data:", val);
            Assert.Contains(json, val);
        }

        [TestMethod]
        public async Task AddContextData_SetsRagOrContextDataField_WithPrefix()
        {
            //ResetStaticFields();
            var core = new AgentCore();
            var ctx = "some context data";
            await core.AddContextData(ctx);

            var f = typeof(AgentCore).GetField("_ragOrContextData", BindingFlags.Instance | BindingFlags.NonPublic);
            var val = f.GetValue(core) as string;
            Assert.IsNotNull(val);
            Assert.Contains("Context Data:", val);
            Assert.Contains(ctx, val);
        }

        [TestMethod]
        public async Task AddAgentRolesTask_SetsAgentTasksField()
        {
            //ResetStaticFields();
            var core = new AgentCore();
            var tasks = "do important work";
            await core.AddAgentRolesTask(tasks);

            var f = typeof(AgentCore).GetField("_agentTasks", BindingFlags.Instance | BindingFlags.NonPublic);
            var val = f.GetValue(core) as string;
            Assert.IsNotNull(val);
            Assert.Contains("Your primary role's tasks are as follows:", val);
            Assert.Contains(tasks, val);
        }

        [TestMethod]
        public async Task ExpectedOutput_SetsExpectedOutputAndTemplate()
        {
            //ResetStaticFields();
            var core = new AgentCore();
            await core.ExpectedOutput("text", "{schema}");

            var fFormat = typeof(AgentCore).GetField("_expectedOutput", BindingFlags.Instance | BindingFlags.NonPublic);
            var fTempl = typeof(AgentCore).GetField("_expectedOutputTempl", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.AreEqual("text", fFormat.GetValue(core) as string);
            Assert.AreEqual("{schema}", fTempl.GetValue(core) as string);
        }

        [TestMethod]
        public async Task Setup_WithEmptyModelPath_DoesNotCreateSession()
        {
            //ResetStaticFields();
            var core = new AgentCore();

            // call Setup with empty path -> method should return early and not create a session
            await core.Setup("", contextSize: 512, gpuLayers: 0, maxTokens: 32, supressAIComments: true);

            var sessionField = typeof(AgentCore).GetField("session", BindingFlags.Instance | BindingFlags.NonPublic);
            var sessionVal = sessionField.GetValue(core);
            Assert.IsNull(sessionVal);
        }

        [TestMethod]
        public async Task Chat_WhenSessionIsNull_ReturnsModelNotConfiguredMessage()
        {
            //ResetStaticFields();
            var core = new AgentCore();
            var result = await core.Chat("hello");
            Assert.AreEqual("The model is not configured correctly. Check your model path and settings to ensure correct operation.", result);
        }

        [TestMethod]
        public async Task ChatStream_WhenSessionIsNull_YieldsNoItems()
        {
            //ResetStaticFields();
            var core = new AgentCore();

            var items = new List<string>();
            await foreach (var t in core.ChatStream("hello"))
            {
                items.Add(t);
            }

            Assert.IsEmpty(items);
        }
    }
}