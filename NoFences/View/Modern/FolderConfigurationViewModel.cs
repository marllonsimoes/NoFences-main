using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using NoFences.View.Service;
using NoFencesService.Repository;

namespace NoFences.View.Modern
{
    public sealed class FolderConfigurationViewModel : ObservableRecipient
    {
        public FolderConfigurationViewModel(IFoldersConfigurationService foldersConfigurationService) {
            _foldersConfigurationService = foldersConfigurationService;
        }

        private IFoldersConfigurationService _foldersConfigurationService;

        private FolderConfiguration _folderConfiguration;

        public FolderConfiguration SelectedFolderConfiguration { 
            get { return _folderConfiguration; }
            set
            {
                SetProperty(ref _folderConfiguration, value);
            }
        }

        #region IoC property listener
        protected override void OnActivated()
        {
            Messenger.Register<FolderConfigurationViewModel, PropertyChangedMessage<FolderConfiguration>>(this, (r, m) => r.Receive(m));
        }

        private void Receive(PropertyChangedMessage<FolderConfiguration> message)
        {
            if (message.Sender.GetType() == typeof(MonitoredPathViewModel)
                && message.PropertyName.Equals(nameof(SelectedFolderConfiguration))) 
            {
                SelectedFolderConfiguration = message.NewValue;
            }
        }
        #endregion

        public void Save()
        {
            OnPropertyChanged(nameof(SelectedFolderConfiguration));
            Messenger.Send(new PropertyChangedMessage<FolderConfiguration>(this, nameof(SelectedFolderConfiguration), SelectedFolderConfiguration, SelectedFolderConfiguration));
        }
    }
}
