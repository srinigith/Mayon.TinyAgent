// See https://aka.ms/new-console-template for more information
using Mayon.TinyAgent;

Console.WriteLine("Welcom!");
string modelPath = @"F:\Huggingface_Models\gemma-2-2b-it-GGUF\gemma-2-2b-it-Q4_K_M.gguf";
//modelPath = @"F:\Huggingface_Models\sweep-next-edit-1.5B\sweep-next-edit-1.5b.q8_0.v2.gguf";
AgentCore tinyAgent = new AgentCore("Supriya", "Receptionist");
string systemText =
    """
        You are a helpful chatbot agent to response user's queries.        
    """;

var rag = """
        Company Information:
            Name: Mayon technologies
            Address: 12345, Kinston st.
            Vision: To be provide a valuable services to the customers.
            Mission: Our core values are to generate a standart tools to the market.
        """;

var tools =
    """
        Tools:    
            [{
                "Name":"GetWeatherForDate",
                "Description":"To get weather for given date or day",
                "Paramters":{"name":"input_date", "type":Date, "required":true}
            },{
                "Name":"GetWeather",
                "Description":"To get current weather",
                "Paramters":null
            },{
                "Name":"GetWeather",
                "Description":"To get today weather",
                "Paramters":null
            },{
                "Name":"GetAnnualReport",
                "Description":"To get annual report",
                "Paramters":null
            },{
                "Name":"GetListOfHolidays",
                "Description":"To get list of holidays for the calander year",
                "Paramters":null
            }]
        """;

var tasks =
    """        
        1. Evaluate the user input text and identify, it's general query or a RAG retrival information or the tool calls.
        2. If the user text is a tool call but any of the required paramter values are missing in the user text, then request them with a validation message.
        3. If the you got the tool_call_response, answer to the user with tool results.
        4. make sure thread-shold for the RAG first then tool calls and then your general responses.        
    """;

var resultTemplate = """
        {"Response_Type":"General | RAG | Tool_Validation | Tool_Call | Tool_Response", ResponseText:"", Tool_Name=""}
    """;

await tinyAgent.AddSysMessage(systemText);
await tinyAgent.AddContextData(rag);
await tinyAgent.AddToolJson(tools);
await tinyAgent.AddAgentRolesTask(tasks);
await tinyAgent.ExpectedOutput("JSON", resultTemplate);
await tinyAgent.Setup(modelPath: modelPath, supressAIComments: true);
//Chat begin
Console.Write("Please wait for the moment. I'm getting ready now...\n");
bool initAgent = false;
var input = "Introduce yourself.";
while (true)
{
    if (initAgent == true)
    {
        input = Console.ReadLine();
    }
    initAgent = true;
    if (string.IsNullOrWhiteSpace(input)) break;
    bool isYeild = false;

    if (!isYeild)
    {
        var message = await tinyAgent.Chat(input);
        Console.WriteLine(message);
    }
    else
    {
        await foreach (var word in tinyAgent.ChatStream(input))
        {
            Console.Write(word);
        }
    }

    Console.Write("\nUser: ");
}