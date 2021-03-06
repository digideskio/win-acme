﻿using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Linq;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class ManualOptionsFactory : TargetPluginOptionsFactory<Manual, ManualOptions>
    {
        public ManualOptionsFactory(ILogService log) : base(log) { }

        public override ManualOptions Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            var input = inputService.RequestString("Enter comma-separated list of host names, starting with the common name");
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }
            else
            {
                return Create(input);
            }
        }

        public override ManualOptions Default(IOptionsService optionsService)
        {
            var args = optionsService.GetArguments<ManualArguments>();
            var input = optionsService.TryGetRequiredOption(nameof(args.Host), args.Host);
            var ret = Create(input);
            var commonName = args.CommonName;
            if (!string.IsNullOrWhiteSpace(commonName))
            {
                commonName = commonName.ToLower().Trim().ConvertPunycode();
                ret.CommonName = commonName;
                if (!ret.AlternativeNames.Contains(commonName))
                {
                    ret.AlternativeNames.Insert(0, commonName);
                }
            }
            return ret;
        }

        private ManualOptions Create(string input)
        {
            var sanList = input.ParseCsv().Select(x => x.ConvertPunycode());
            if (sanList != null)
            {
                return new ManualOptions()
                {
                    FriendlyNameSuggestion = sanList.First(),
                    CommonName = sanList.First(),
                    AlternativeNames = sanList.ToList()
                };
            }
            else
            {
                return null;
            }
        }
    }
}
