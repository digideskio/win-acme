﻿using Newtonsoft.Json;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.StorePlugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PKISharp.WACS.Services.Renewal
{
    internal abstract class BaseRenewalService : IRenewalService
    {
        internal ILogService _log;
        internal PluginService _plugin;
        internal int _renewalDays;
        internal List<ScheduledRenewal> _renewalsCache;
        internal string _configPath = null;

        public BaseRenewalService(
            ISettingsService settings,
            IOptionsService options,
            ILogService log,
            PluginService plugin)
        {
            _log = log;
            _plugin = plugin;
            _configPath = settings.ConfigPath;
            _renewalDays = settings.RenewalDays;
            _log.Debug("Renewal period: {RenewalDays} days", _renewalDays);
        }

        public ScheduledRenewal Find(Target target)
        {
            return Renewals.Where(r => string.Equals(r.Target.Host, target.Host)).FirstOrDefault();
        }

        public void Save(ScheduledRenewal renewal, RenewResult result)
        {
            var renewals = Renewals.ToList();
            if (renewal.New)
            {
                renewal.History = new List<RenewResult>();
                renewals.Add(renewal);
                renewal.New = false;
                _log.Information(true, "Adding renewal for {target}", renewal.Target.Host);

            }
            else if (result.Success)
            {
                _log.Information(true, "Renewal for {host} succeeded", renewal.Target.Host);
            }
            else
            {
                _log.Error("Renewal for {host} failed, will retry on next run", renewal.Target.Host);
            }

            // Set next date
            if (result.Success)
            {
                renewal.Date = DateTime.UtcNow.AddDays(_renewalDays);
                _log.Information(true, "Next renewal scheduled at {date}", renewal.Date.ToUserString());
            }
            renewal.Updated = true;
            renewal.History.Add(result);
            Renewals = renewals;
        }

        public void Import(ScheduledRenewal renewal)
        {
            var renewals = Renewals.ToList();
            renewals.Add(renewal);
            _log.Information(true, "Importing renewal for {target}", renewal.Target.Host);
            Renewals = renewals;
        }

        public IEnumerable<ScheduledRenewal> Renewals
        {
            get => ReadRenewals();
            private set => WriteRenewals(value);
        }

        public void Cancel(ScheduledRenewal renewal)
        {
            Renewals = Renewals.Except(new[] { renewal });
            _log.Warning("Renewal {target} cancelled", renewal);
        }

        public void Clear()
        {
            Renewals = new List<ScheduledRenewal>();
        }



        /// <summary>
        /// To be implemented by inherited classes (e.g. registry/filesystem/database)
        /// </summary>
        /// <param name="BaseUri"></param>
        /// <returns></returns>
        internal abstract string[] RenewalsRaw { get; set; }
        
        /// <summary>
        /// Parse renewals from store
        /// </summary>
        public IEnumerable<ScheduledRenewal> ReadRenewals()
        {
            if (_renewalsCache == null)
            {
                var read = RenewalsRaw;
                var list = new List<ScheduledRenewal>();
                if (read != null)
                {
                    list.AddRange(read.Select(x => Load(x, _configPath)).Where(x => x != null));
                }
                _renewalsCache = list;
            }
            return _renewalsCache;
        }

        /// <summary>
        /// Serialize renewal information to store
        /// </summary>
        /// <param name="BaseUri"></param>
        /// <param name="Renewals"></param>
        public void WriteRenewals(IEnumerable<ScheduledRenewal> Renewals)
        {
            var list = Renewals.ToList();
            list.ForEach(r =>
            {
                if (r.Updated)
                {
                    var history = HistoryFile(r, _configPath);
                    if (history != null) {
                        File.WriteAllText(history.FullName, JsonConvert.SerializeObject(r.History));
                    }
                    r.Updated = false;
                }
            });
            RenewalsRaw = list.Select(x => JsonConvert.SerializeObject(x,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                })).ToArray();
            _renewalsCache = list;
        }

        /// <summary>
        /// Parse from string
        /// </summary>
        /// <param name="renewal"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private ScheduledRenewal Load(string renewal, string path)
        {
            ScheduledRenewal result;
            try
            {
                result = JsonConvert.DeserializeObject<ScheduledRenewal>(renewal,
                    new PluginOptionsConverter<StorePluginOptions>(_plugin.StorePluginOptionTypes()));
                if (result?.Target == null)
                {
                    throw new Exception();
                }
            }
            catch
            {
                _log.Error("Unable to deserialize renewal: {renewal}", renewal);
                return null;
            }

            if (result.History == null)
            {
                result.History = new List<RenewResult>();
                var historyFile = HistoryFile(result, path);
                if (historyFile != null && historyFile.Exists)
                {
                    try
                    {
                        var history = JsonConvert.DeserializeObject<List<RenewResult>>(File.ReadAllText(historyFile.FullName));
                        if (history != null)
                        {
                            result.History = history;
                        }
                    }
                    catch
                    {
                        _log.Warning("Unable to read history file {path}", historyFile.Name);
                    }
                }
            }

            if (result.Target.AlternativeNames == null)
            {
                result.Target.AlternativeNames = new List<string>();
            }
            return result;
        }

        /// <summary>
        /// Determine location and name of the history file
        /// </summary>
        /// <param name="renewal"></param>
        /// <param name="configPath"></param>
        /// <returns></returns>
        private FileInfo HistoryFile(ScheduledRenewal renewal, string configPath)
        {
            FileInfo fi = configPath.LongFile("", renewal.Target.Host, ".history.json", _log);
            if (fi == null) {
                _log.Warning("Unable access history for {renewal]", renewal);
            }
            return fi;
        }
    }
}