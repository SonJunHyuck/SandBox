using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sandbox.Notification
{
    /// <summary>One retained modal view. Requests wait for the preceding interaction result.</summary>
    public sealed class ModalPresenter
    {
        private readonly ConfirmModalView view;
        private readonly Queue<ModalEntry> queue = new Queue<ModalEntry>();
        private bool showing;
        private bool closing;
        private ModalEntry current;

        public ModalPresenter(RectTransform area, ConfirmModalView prefab)
        {
            view = UnityEngine.Object.Instantiate(prefab, area);
            view.HideImmediately();
        }

        public void Show(ConfirmModalRequest request, Action<ConfirmResult> completed)
        {
            queue.Enqueue(new ModalEntry(request, completed));
            ShowNext();
        }

        private void ShowNext()
        {
            if (showing || queue.Count == 0)
                return;

            current = queue.Dequeue();
            showing = true;
            view.Bind(current.Request,
                () => Close(ConfirmResult.Confirmed),
                () => Close(ConfirmResult.Cancelled));
            view.Show();
        }

        private void Close(ConfirmResult result)
        {
            if (!showing || closing)
                return;

            closing = true;
            view.Hide(() =>
            {
                current.Completed?.Invoke(result);
                showing = false;
                closing = false;
                ShowNext();
            });
        }

        private sealed class ModalEntry
        {
            public readonly ConfirmModalRequest Request;
            public readonly Action<ConfirmResult> Completed;

            public ModalEntry(ConfirmModalRequest request, Action<ConfirmResult> completed)
            {
                Request = request;
                Completed = completed;
            }
        }
    }
}
