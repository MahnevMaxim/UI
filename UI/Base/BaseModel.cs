using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace SetUp.UI.Base
{
    public abstract class BaseModel : INotifyPropertyChanged
    {
        [JsonIgnore]
        public BaseViewModel ViewModel { get; set; }

        public delegate void MessageHandler(string body, string title);
        public event MessageHandler OnSendMessage;

        // События в дочернем классе будут работать только через обёртку
        protected virtual void SendMessage(string body, string title)
        {
            OnSendMessage?.Invoke(body, title);
        }

        // Стартовый метод
        internal virtual void Run() { }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
            // для автоматического обновления одноимённых свойст модели и вьюмодели
            ViewModel?.OnPropertyChanged(prop);
        }
    }
}
