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
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Xml.Linq;

namespace DeployWeb
{
    public class Program
    {
        private static ConnectionHelper connections;
        static void Main(string[] args)
        {
            try
            {
                if (args.Length < 2) { throw new Exception("Missing args for the console app"); }
                var type = args[0];

                var basePath = args[1].Replace("\\", "/");
                var filters2 = args.Skip(2).ToList();

                var settings = GetSettings(basePath);
                var filters1 = settings.defFilters;
                var sourcePath = settings.srcFolder.StartsWith(".") ? System.IO.Path.Combine(basePath + settings.srcFolder) : settings.srcFolder;

                connections = new ConnectionHelper(basePath, type);

                if (type == "deploy") { Deploy.DeployWebRes(connections, sourcePath, filters1, filters2); }
                if (type == "create") { connections.CreateConnection(); }
                if (type == "update") { UpdateConnection(); }
                if (type == "delete") { DeleteConnection(); }
                if (type == "download") { Download.DownloadWebRes(connections, sourcePath, filters1, filters2); }
            }
            catch (Exception e) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
                Console.ResetColor();
                throw e;
            }
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

        static Settings GetSettings(string basePath)
        {
            var settingsPath = basePath + "/settings.json";

            if (File.Exists(settingsPath))
            {
                var sr = new StreamReader(settingsPath);
                var json = sr.ReadToEnd();
                sr.Close();

                return JsonConvert.DeserializeObject<Settings>(json);
            }

            else
            {
                var settings = new Settings();
                Console.WriteLine("Creating settings json:");

                Console.WriteLine("Path (absolute or relative) to your Webresources folder (example: \\\\..\\\\Example.CRM.Webresources\\\\src\\\\Webresources)");
                settings.srcFolder = Console.ReadLine();

                Console.WriteLine("Your default filter, only web resources containg this word will be updated (example: ex1_,ex2)");
                settings.defFilters = Console.ReadLine().Split(',').ToList();

                var content = JsonConvert.SerializeObject(settings);
                File.WriteAllText(basePath + "/settings.json", content);

                return settings;
            }
        }
    }
}
