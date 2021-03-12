using TCAdmin.SDK.Objects;
using Server = TCAdmin.GameHosting.SDK.Objects.Server;
using Service = TCAdmin.GameHosting.SDK.Objects.Service;

namespace TCAdminBackupManager
{
    public static class Extensions
    {
        public static string ReplaceVariables(this string query, Service service = null, User user = null, Server server = null, Datacenter datacenter = null)
        {
            var input = new TCAdmin.SDK.Scripting.InputParser(query);
            service?.ReplacePropertyValues(input);
            service?.ReplaceCustomVariables(new TCAdmin.GameHosting.SDK.Objects.Game(service.GameId).CustomVariables, service.Variables, input);
            user?.ReplacePropertyValues(input);
            server?.ReplacePropertyValues(input);
            datacenter?.ReplacePropertyValues(input);

            var output = input.GetOutput();
            return output;
        }
    }
}