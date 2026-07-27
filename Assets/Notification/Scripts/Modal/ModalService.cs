using System;

namespace Sandbox.Notification
{
    /// <summary>Separate from NotificationService because modal interactions return a result.</summary>
    public sealed class ModalService
    {
        private readonly ModalPresenter presenter;

        public ModalService(ModalPresenter presenter)
        {
            this.presenter = presenter;
        }

        public void Show(ConfirmModalRequest request, Action<ConfirmResult> completed)
        {
            presenter.Show(request, completed);
        }
    }
}
