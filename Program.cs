using DeployWeb.Helpers;
using DeployWeb.Models;
using DeployWeb.Properties;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Sharprompt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;

namespace DeployWeb
{
    public class Program
    {
        private static ConnectionHelper connections;
        static void Main(string[] args)
        {
            if(args.Length < 1) { throw new Exception("Missing args for the console app"); }
            var type = args[0];
            if (args.Length < 2) { throw new Exception("Missing args for the console app"); }

            var basePath = args[1].Replace("\\", "/");
            var settingsPath = basePath + "/settings.json";
            var filters = args.Skip(2).ToList();

            var sr = new StreamReader(settingsPath);
            var json = sr.ReadToEnd();
            sr.Close();

            connections = new ConnectionHelper(basePath, type);

            var settings = JsonConvert.DeserializeObject<Settings>(json);
            var sourcePath = settings.srcFolder;
            filters.AddRange(settings.defFilters);

            if (type == "deploy") { Deploy.DeployWebRes(sourcePath, filters, connections); }
            if (type == "create") { connections.CreateConnection(); }
            if (type == "update") { UpdateConnection(); }
            if (type == "delete") { DeleteConnection(); }
            if (type == "download") { Download.DownloadWebRes(sourcePath, filters, connections); }

            return;
        }

        static void UpdateConnection() 
        {
            var listConnections = connections.GetConnections();

            var options = new SelectOptions<Connection>();
            options.Items = listConnections;
            options.Message = "Select Connection";
            options.DefaultValue = listConnections.FirstOrDefault();

            var connection = Prompt.Select(options);
            connections.UpdateConnection(connection);
        }

        static void DeleteConnection()
        {
            var listConnections = connections.GetConnections();

            var options = new SelectOptions<Connection>();
            options.Items = listConnections;
            options.Message = "Select Connection";
            options.DefaultValue = listConnections.FirstOrDefault();

            var connection = Prompt.Select(options);

            Console.WriteLine($"Are you sure you want to delete {connection.name} connection? [y/N]: ");
            var res = Console.ReadLine();
            if(res.ToLower() != "y") { return; }

            connections.DeleteConnection(connection);
        }
    }
}
