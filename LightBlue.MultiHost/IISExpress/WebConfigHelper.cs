using System;
using System.IO;
using LightBlue.Infrastructure;
using LightBlue.MultiHost.Configuration;

namespace LightBlue.MultiHost.IISExpress
{
    static class WebConfigHelper
    {
        public static WebHostArgs Create(RoleConfiguration config)
        {
            var configurationPath = config.ConfigurationPath;
            var configurationFilePath = ConfigurationLocator.LocateConfigurationFile(configurationPath);
            var serviceDefinitionPath = ConfigurationLocator.LocateServiceDefinition(configurationPath);
            var assembly = config.Assembly;
            var args = new WebHostArgs
            {
                Assembly = assembly,
                Port = int.Parse(config.Port),
                RoleName = config.RoleName,
                Title = config.Title,
                ConfigurationPath = configurationFilePath,
                ServiceDefinitionPath = serviceDefinitionPath,
                UseSsl = bool.Parse(config.UseSsl),
                Hostname = config.Hostname,
                UseHostedStorage = false,
                Use64Bit = false,
            };
            return args;
        }

        public static void PatchWebConfig(WebHostArgs args)
        {
            var webConfigFilePath = DetermineWebConfigPath(args.Assembly);
            if (!File.Exists(webConfigFilePath))
            {
                throw new ArgumentException("No web.config could be located for the site");
            }

            ConfigurationManipulation.RemoveAzureTraceListenerFromConfiguration(webConfigFilePath);
        }

        public static string DetermineWebConfigPath(string assemblyPath)
        {
            var siteDirectory = DetermineSiteDirectory(assemblyPath);
            return Path.Combine(siteDirectory, "web.config");
        }

        public static string DetermineSiteDirectory(string assemblyPath)
        {
            return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(assemblyPath) ?? "", ".."));
        }

        public static string SiteBinDirectory(string assemblyPath)
        {
            return Path.GetDirectoryName(assemblyPath) ?? "";
        }
    }
}