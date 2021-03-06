﻿using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class SelfHostingOptionsFactory : ValidationPluginOptionsFactory<SelfHosting, SelfHostingOptions>
    {
        public SelfHostingOptionsFactory(ILogService log) : base(log) { }

        public override SelfHostingOptions Aquire(Target target, IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            return Default(target, optionsService);
        }

        public override SelfHostingOptions Default(Target target, IOptionsService optionsService)
        {
            var args = optionsService.GetArguments<SelfHostingArguments>();
            return new SelfHostingOptions()
            {
                Port = args.ValidationPort
            };
        }
    }
}