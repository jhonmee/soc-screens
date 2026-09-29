// MuroSOC - ConfigStore
using System;
using System.IO;
using System.Windows.Forms;

namespace MuroSoc
{
    internal sealed class ConfigStore
    {
        private readonly string path;
        private readonly Timer watchTimer;
        private DateTime lastKnownWrite;

        public ConfigStore(string path)
        {
            this.path = path;
            watchTimer = new Timer();
            watchTimer.Interval = 2000;
            watchTimer.Tick += delegate { CheckForExternalChange(); };
        }

        public event EventHandler<ConfigReloadedEventArgs> Reloaded;

        public AppConfig Current { get; private set; }

        public void Load()
        {
            AppConfig loaded = null;
            if (File.Exists(path))
            {
                try
                {
                    loaded = JsonFile.Read<AppConfig>(path);
                }
                catch (Exception ex)
                {
                    Log.Error("config.json no se pudo leer; se guarda copia y se usan valores por defecto", ex);
                    BackupBrokenFile();
                }
            }
            if (loaded == null)
            {
                loaded = new AppConfig();
                loaded.Normalize();
                Current = loaded;
                Save();
            }
            else
            {
                loaded.Normalize();
                Current = loaded;
            }
            lastKnownWrite = GetWriteTime();
            watchTimer.Start();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                JsonFile.Write(path, Current);
                lastKnownWrite = GetWriteTime();
            }
            catch (Exception ex)
            {
                Log.Error("No se pudo guardar config.json", ex);
            }
        }

        private void CheckForExternalChange()
        {
            DateTime write = GetWriteTime();
            if (write == lastKnownWrite)
            {
                return;
            }
            lastKnownWrite = write;
            if (write == DateTime.MinValue)
            {
                return;
            }
            try
            {
                AppConfig loaded = JsonFile.Read<AppConfig>(path);
                loaded.Normalize();
                Current = loaded;
                Log.Info("config.json recargado");
                OnReloaded(new ConfigReloadedEventArgs(null));
            }
            catch (Exception ex)
            {
                Log.Error("config.json cambió pero no es válido; se mantiene la configuración anterior", ex);
                OnReloaded(new ConfigReloadedEventArgs(ex.Message));
            }
        }

        private void OnReloaded(ConfigReloadedEventArgs args)
        {
            EventHandler<ConfigReloadedEventArgs> handler = Reloaded;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        private DateTime GetWriteTime()
        {
            try
            {
                return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
            }
            catch (Exception)
            {
                return DateTime.MinValue;
            }
        }

        private void BackupBrokenFile()
        {
            try
            {
                string backup = path + ".error-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json";
                File.Copy(path, backup, true);
            }
            catch (Exception)
            {
            }
        }
    }

    internal sealed class ConfigReloadedEventArgs : EventArgs
    {
        public ConfigReloadedEventArgs(string error)
        {
            Error = error;
        }

        public string Error { get; private set; }
    }
}
