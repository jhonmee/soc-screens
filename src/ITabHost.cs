// MuroSOC - ITabHost
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MuroSoc
{
    internal interface ITabHost
    {
        Control ContentHost { get; }

        Task<BrowserTab> OpenScriptTabAsync(BrowserTab opener);

        void CloseScriptTab(BrowserTab tab);

        void OnTabStateChanged(BrowserTab tab);

        void ShowNotice(BrowserTab tab, NoticeEventArgs notice);

        void FocusHost();

        void OnTabFocused(BrowserTab tab);

        List<MenuEntry> BuildMenu(BrowserTab tab);
    }
}
