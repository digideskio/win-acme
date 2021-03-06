﻿using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class Script : ScriptClient, IInstallationPlugin
    {
        private Renewal _renewal;
        private ScriptOptions _options;

        public Script(Renewal renewal, ScriptOptions options, ILogService logService) : base(logService)
        {
            _options = options;
            _renewal = renewal;
        }

        void IInstallationPlugin.Install(CertificateInfo newCertificate, CertificateInfo oldCertificate)
        {
            RunScript(
                  _options.Script,
                  _options.ScriptParameters,
                  newCertificate.SubjectName,
                  _renewal.PfxPassword,
                  newCertificate.PfxFile.FullName,
                  newCertificate.Store?.Name ?? "[None]",
                  newCertificate.Certificate.FriendlyName,
                  newCertificate.Certificate.Thumbprint,
                  newCertificate.PfxFile.Directory.FullName);
        }
    }
}