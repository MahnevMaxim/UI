using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using Core.App;
using Core.App.Helpers;
using SetUp.UI.Base;
using AppContext = Core.App.AppContext;

namespace SetUp.UI.Model
{
    public class Main : BaseModel
    {
        private const string SuccessCaption = "Успех";
        private const string FailedCaption = "Ошибка";

        private const string PluginUpdaterFolderName = "PluginUpdater";
        private const string PluginUpdaterExeName = "PluginUpdaterService.exe";
        private const string PluginUpdaterProcessName = "PluginUpdaterService";
        private const string PluginUpdaterVersionRegistryKey = "PluginUpdaterServiceVersion";

        private const string UpdaterExeName = "UpdaterService.exe";
        private const string UpdaterProcessName = "UpdaterService";
        private const string UpdaterVersionRegistryKey = "UpdaterServiceVersion";
        private const string UpdaterPackagesZipName = "UpdaterServicePackages.zip";
        private const string StartUpdaterCmdCommand = "Sc start AtolUpdaterService";

        private const string IikoIsRuningMessage = "Для установки необходимо закрыть iikoFront";
        private const string InstallSuccessMessage = "Плагин установлен";
        private const string InstallFailedMessage = "Плагин не был установлен";

        private const string PluginVersionRegistryKey = "PluginVersion";

        private string _infoBlock;
        private BackgroundWorker _installCommandBackgroundWorker;
        private string _iikoPluginsFolderPath;
        private string _updateServer;

        public string Release => Consts.ReleaseVersion;
        public string ReleaseType => Consts.ReleaseType;

        public bool CanUpdate => _installCommandBackgroundWorker?.IsBusy != true;

        public bool CanInstall => !string.IsNullOrWhiteSpace(IikoPluginsFolderPath) &&
                !string.IsNullOrWhiteSpace(UpdateServer) &&
                _installCommandBackgroundWorker?.IsBusy != true;

        /// <summary>
        /// Сервер хранения файлов обновления
        /// </summary>
        public string IikoPluginsFolderPath
        {
            get => _iikoPluginsFolderPath;
            set
            {
                _iikoPluginsFolderPath = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Сервер хранения файлов обновления
        /// </summary>
        public string UpdateServer
        {
            get => _updateServer;
            set
            {
                // может пригодиться, если поле во вьюшке будет редактируемым
                if (_installCommandBackgroundWorker?.IsBusy != true)
                {
                    _updateServer = value;
                }

                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Информация для отображения пользователю (статус бар)
        /// </summary>
        public string InfoBlock
        {
            get => _infoBlock;
            set
            {
                _infoBlock = value;
                OnPropertyChanged();
            }
        }

        public Main()
        {
            IikoPluginsFolderPath = PathHelper.SearchIikoPluginsFolder();
            UpdateServer = AppContext.UpdateServer;
#if DEBUG
            InfoBlock = "Состояние установки";
#endif
        }

        internal void Install()
        {
            try
            {
                // Проверка закрыта ли iikoFront
                if (IikoHelper.IsFrontRun)
                {
                    SendMessage(IikoIsRuningMessage, FailedCaption);
                    return;
                }

                _installCommandBackgroundWorker = new BackgroundWorker();
                _installCommandBackgroundWorker.DoWork += (sender, e) =>
                {
                    try
                    {
                        string newVersion = WebHelper.GetLastPluginVersion(UpdateServer);

                        InfoBlock = "Удаление устаревших компонентов";
                        DeleteAllComponents();

                        InfoBlock = "Загрузка новых компонентов";
                        LoadAllComponents();

                        InfoBlock = "Установка новых компонентов";
                        InstallAllComponents();

                        // ожидание установки плагина
                        InfoBlock = "Установка плагина";

                        for (int i = 0; i < 60; i++)
                        {
                            Thread.Sleep(TimeSpan.FromSeconds(1));

                            InfoBlock = $"Ожидание установки плагина {i + 1}/60";

                            string oldVersion = Registry.ReadRegistryValue(PluginVersionRegistryKey, string.Empty);

                            // Проверка установки службы
                            if (newVersion == oldVersion)
                            {
                                // Если служба установлена выдаем предупреждение об этом и выходим из функции
                                SendMessage(InstallSuccessMessage, SuccessCaption);

                                InfoBlock = string.Empty;

                                return;
                            }
                        }

                        // Если выход не был совершен заранее
                        // то плагин не был установлен
                        SendMessage(InstallFailedMessage, FailedCaption);

                        AppContext.Logger.Warning("Plugin was not installed during a version control system deployment");
                    }
                    catch (Exception ex)
                    {
                        AppContext.Logger.Error(ex, ex.Message);
                        SendMessage(ex.Message, FailedCaption);
                    }

                    InfoBlock = string.Empty;
                };
                _installCommandBackgroundWorker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                AppContext.Logger.Error(ex, ex.Message);
                SendMessage(ex.Message, FailedCaption);
            }
        }
        private void DeleteAllComponents()
        {
            ServiceHelper.UninstallService(Path.Combine(PathHelper.RootFolderPath, PluginUpdaterFolderName, PluginUpdaterExeName));
            Process.GetProcesses()
                .Where(i => i.ProcessName.Contains(PluginUpdaterProcessName)).ToList()
                .ForEach(i => i.Kill());

            ServiceHelper.UninstallService(Path.Combine(PathHelper.RootFolderPath, UpdaterExeName));
            Process.GetProcesses()
                .Where(i => i.ProcessName.Contains(UpdaterProcessName)).ToList()
                .ForEach(i => i.Kill());

            Registry.WriteRegistryValue(PluginVersionRegistryKey, string.Empty);
            Registry.WriteRegistryValue(UpdaterVersionRegistryKey, string.Empty);
            Registry.WriteRegistryValue(PluginUpdaterVersionRegistryKey, string.Empty);
        }

        private void LoadAllComponents()
        {
            // Создание директории для установки службы
            if (!Directory.Exists(PathHelper.RootFolderPath))
            {
                _ = Directory.CreateDirectory(PathHelper.RootFolderPath);
            }
            // Создание директории для временных файлов
            if (!Directory.Exists(PathHelper.TempFolderPath))
            {
                _ = Directory.CreateDirectory(PathHelper.TempFolderPath);
            }

            // Загрузка актуальной версии службы обновления
            WebHelper.DownloadWebFile(WebHelper.GetUpdaterServiceLoadUrl(UpdateServer),
                Path.Combine(PathHelper.RootFolderPath, UpdaterExeName));

            // Загрузка актуальных пакетов для службы обновления
            WebHelper.DownloadWebFile(WebHelper.GetPackagesUpdaterServiceLoadUrl(UpdateServer),
                Path.Combine(PathHelper.TempFolderPath, UpdaterPackagesZipName));
        }

        private void InstallAllComponents()
        {
            using (ZipArchive zipFile = ZipFile.OpenRead(Path.Combine(PathHelper.TempFolderPath, UpdaterPackagesZipName)))
            {
                zipFile
                    .Entries
                    .Select(i => new { oldFilePath = Path.Combine(PathHelper.RootFolderPath, i.Name) })
                    .Select(i => new
                    {
                        isExist = File.Exists(i.oldFilePath),
                        i.oldFilePath
                    })
                    .Where(i => i.isExist).ToList()
                    .ForEach(i => File.Delete(i.oldFilePath));
            }

            ZipFile.ExtractToDirectory(
                    Path.Combine(PathHelper.TempFolderPath, UpdaterPackagesZipName),
                    PathHelper.RootFolderPath);

            ServiceHelper.InstallService(Path.Combine(PathHelper.RootFolderPath, UpdaterExeName));
            CmdHelper.ExecuteCommand(StartUpdaterCmdCommand);
        }
    }
}
