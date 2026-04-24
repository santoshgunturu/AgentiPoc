using System.ComponentModel;
using AgenticPA.Services;
using AgenticPA.Services.Models;
using ModelContextProtocol.Server;

namespace AgenticPA.McpServer.Tools;

[McpServerToolType]
public static class ClientTools
{
    [McpServerTool(Name = "get_clients")]
    [Description("Return the full catalog of payers/clients (name, state, client id).")]
    public static async Task<IReadOnlyList<Client>> GetClients(IClientService clients)
        => await clients.GetAllAsync();
}
