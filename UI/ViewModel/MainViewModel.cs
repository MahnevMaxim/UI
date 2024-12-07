using SetUp.UI.Base;
using SetUp.UI.Model;
using System.Windows;

namespace SetUp.UI.ViewModel
{
    internal class MainViewModel : BaseViewModel
    {
        private Main modelMain;

        public override void InitializeViewModel()
        {
            modelMain = (Main)Model;
        }

        public string IikoPluginsFolderPath
        {
            get => modelMain.IikoPluginsFolderPath;
            set => modelMain.IikoPluginsFolderPath = value;
        }

        public string UpdateServer => modelMain.UpdateServer;

        public string InfoBlock => modelMain.InfoBlock;

        public string Release => modelMain.Release;

        public string ReleaseType => modelMain.ReleaseType;

        public RelayCommand InstallCommand => new RelayCommand(obj => modelMain.Install(), obj => modelMain.CanInstall);

        public RelayCommand CloseCommand => new RelayCommand(obj => Application.Current.Shutdown());
    }
}
