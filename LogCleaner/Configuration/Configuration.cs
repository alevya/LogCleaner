using System;
using System.Configuration;

namespace LogCleaner.Configuration
{
  public class ConfigurationApp
  {
      private  System.Configuration.Configuration _cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
      private string _savePath;
      private bool _removeFromBlocks;
      private bool _zipped;

      public MachinesCollection MachineItems
      {
          get
          {
              var machines = _cfg.GetSection("StartupMachines") as MachinesConfigSection;
              return machines?.MachineItems;
          }
      }

      public string SavePath
      {
        get
        {
            var cSavePath = _cfg.AppSettings.Settings["SavePath"];
            return cSavePath != null ? cSavePath.Value : _savePath;
        }
        set
        {
            var cSavePath = _cfg.AppSettings.Settings["SavePath"];
            if (cSavePath == null)
            {
                _savePath = value;
                return;
            }

            cSavePath.Value = value;
            _cfg.Save();
        }
      }

      public  bool RemoveFromBlocks
      {

        get
        {
            var cRemoveFromBlocks = _cfg.AppSettings.Settings["RemoveFromBlocks"];
            if (cRemoveFromBlocks == null) return _removeFromBlocks;
            bool.TryParse(cRemoveFromBlocks.Value, out _removeFromBlocks);
            return _removeFromBlocks;
        }
        set
        {
            var cRemoveFromBlocks = _cfg.AppSettings.Settings["RemoveFromBlocks"];
            if (cRemoveFromBlocks == null)
            {
                _removeFromBlocks = value;
                return;
            }
            cRemoveFromBlocks.Value = Convert.ToString(value);
            _cfg.Save();
        }
      }

      public bool Zipped
      {
        get
        {
            var cZipped = _cfg.AppSettings.Settings["Zipped"];
            if (cZipped == null) return _zipped;
            bool.TryParse(cZipped.Value, out _zipped);
            return _zipped;
        }
        set
        {
            var cZipped = _cfg.AppSettings.Settings["Zipped"];
            if (cZipped == null)
            {
                _zipped = value;
                return;
            }

            cZipped.Value = Convert.ToString(value);
            _cfg.Save();
        }
      }

      public  int? RequestTimeout
      {
        get
        {
            var cRequestTimeout = _cfg.AppSettings.Settings["RequestTimeout"];
            if (cRequestTimeout == null) return null;
            int.TryParse(cRequestTimeout.Value, out int requestTimeout);
            return requestTimeout;
        }
      }

      public int? ReadWriteTimeout
      {
        get
        {
            var cReadWriteTimeout = _cfg.AppSettings.Settings["ReadWriteTimeout"];
            if (cReadWriteTimeout == null) return null;
            int.TryParse(cReadWriteTimeout.Value, out int readWriteTimeout);
            return readWriteTimeout;
        }
      }

      public void Load(string path)
      {
        var confMap = new ExeConfigurationFileMap {ExeConfigFilename = path};
        _cfg = ConfigurationManager.OpenMappedExeConfiguration(confMap, ConfigurationUserLevel.None);
      }

    }

    public class MachinesConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("Machines")]
        public MachinesCollection MachineItems
        {
            get { return (MachinesCollection) base["Machines"]; }
        }
    }

    public class MachineElement : ConfigurationElement
    {
        [ConfigurationProperty("number", DefaultValue = "", IsKey = true, IsRequired = true)]
        public string MachineNumber
        {
            get { return (string)base["number"]; }
            set { base["number"] = value; }
        }

    [ConfigurationProperty("ip", DefaultValue = "", IsKey = false, IsRequired = false)]
    public string MachineIp
    {
        get
        {
          return (string)base["ip"];
        }
        set { base["ip"] = value; }
    }
  }

    [ConfigurationCollection(typeof(MachineElement), AddItemName = "Machine")]
    public class MachinesCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new MachineElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((MachineElement) element).MachineNumber;
        }

        public MachineElement this[int index]
        {
            get { return (MachineElement) BaseGet(index); }
        }
    }
}
