using System;

namespace LogCleaner.Configuration
{
  internal class ConfigException : Exception
  {
    public ConfigException()
    {
    }

    public ConfigException(string message)
      : base(message)
    {
    }

    public ConfigException(string message, Exception inner)
      : base(message, inner)
    {
    }
  }
}
