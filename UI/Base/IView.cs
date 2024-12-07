namespace SetUp.UI.Base
{
    public interface IView
    {
        void Show();

        bool? ShowDialog();

        void ShowMessage(string body, string title);
    }
}
