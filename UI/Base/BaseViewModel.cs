using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SetUp.UI.Base
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public BaseModel Model { get; set; }

        public void SetModel(BaseModel model)
        {
            Model = model;
            Model.ViewModel = this;
            InitializeViewModel();
        }

        /// <summary>
        /// Если что-то дополнительно надо инициализировать,
        /// то делать это здесь, в смысле в дочернем классе.
        /// </summary>
        public virtual void InitializeViewModel() { }


        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
