using System.Windows;

namespace SetUp.UI.Base
{
    internal class MvvmFactory
    {
        // метод создания окон и установления отношений между ними
        // при необходимости запустить логику в модели при загрузке окна надо переопределить метод Run() в модели
        public static void CreateWindow(BaseModel model, BaseViewModel viewModel, IView view, ViewMode viewMode)
        {
            viewModel.SetModel(model);
            (view as Window).DataContext = viewModel;
            model.OnSendMessage += (string body, string title) => view.ShowMessage(body, title);
            if (viewMode == ViewMode.Show)
            {
                view.Show();
            }

            if (viewMode == ViewMode.ShowDialog)
            {
                _ = view.ShowDialog();
            }

            model.Run();
        }
    }
}
