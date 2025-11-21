using Asahi.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Asahi.ViewModels;


namespace Asahi.Helpers
{
    public static class ModalHelper
    {
        public static void ShowDeferred(ModalStore modalStore, string title, string message)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                modalStore.ShowModal(new MessageModalViewModel(title, message, modalStore));
            });
        }
    }
}
