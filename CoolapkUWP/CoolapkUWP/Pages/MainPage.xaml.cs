﻿using CoolapkUWP.BackgroundTasks;
using CoolapkUWP.Common;
using CoolapkUWP.Controls;
using CoolapkUWP.Helpers;
using CoolapkUWP.Models;
using CoolapkUWP.Pages.BrowserPages;
using CoolapkUWP.Pages.FeedPages;
using CoolapkUWP.Pages.SettingsPages;
using CoolapkUWP.ViewModels.BrowserPages;
using CoolapkUWP.ViewModels.FeedPages;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Metadata;
using Windows.Phone.UI.Input;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using muxc = Microsoft.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace CoolapkUWP.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private bool isLoaded;

        private string _userName;
        public string UserName
        {
            get => _userName;
            set
            {
                if (_userName != value)
                {
                    _userName = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private ImageSource _userAvatar;
        public ImageSource UserAvatar
        {
            get => _userAvatar;
            set
            {
                if (_userAvatar != value)
                {
                    _userAvatar = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private NotificationsModel _notificationsModel;
        public NotificationsModel NotificationsModel
        {
            get => _notificationsModel;
            set
            {
                if (_notificationsModel != value)
                {
                    _notificationsModel = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private readonly List<(string Tag, Type Page)> _pages = new List<(string Tag, Type Page)>
        {
            ("Find", typeof(FindPage)),
            ("Home", typeof(IndexPage)),
            ("Circle", typeof(CirclePage)),
            ("Settings", typeof(SettingsPage)),
            ("Notifications", typeof(NotificationsPage))
        };

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChangedEvent([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            if (name != null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }
        }

        public MainPage()
        {
            InitializeComponent();
            UIHelper.MainPage = this;
            LiveTileTask.Instance?.UpdateTile();
            UIHelper.ShellDispatcher = Dispatcher;
            NotificationsModel.Instance?.Update();
            NotificationsModel = NotificationsModel.Instance;
            SearchBoxHolder.RegisterPropertyChangedCallback(Slot.IsStretchProperty, new DependencyPropertyChangedCallback(OnIsStretchProperty));
            NavigationView.RegisterPropertyChangedCallback(muxc.NavigationView.IsBackButtonVisibleProperty, new DependencyPropertyChangedCallback(OnIsBackButtonVisibleChanged));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            OnLoginChanged(string.Empty, true);
            SettingsHelper.LoginChanged += OnLoginChanged;
            if (ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons"))
            { HardwareButtons.BackPressed += System_BackPressed; }
            AppTitleText.Text = ResourceLoader.GetForViewIndependentUse().GetString("AppName") ?? "酷安";
            if (!isLoaded)
            {
                if (e.Parameter is IActivatedEventArgs ActivatedEventArgs)
                {
                    OpenActivatedEventArgs(ActivatedEventArgs);
                }
                else
                {
                    NavigationView_Navigate("Home", new EntranceNavigationTransitionInfo());
                }
                isLoaded = true;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (this.IsAppWindow())
            {
                this.GetWindowForElement().Changed -= AppWindow_Changed;
            }
            else
            {
                Window.Current.SetTitleBar(null);
                SystemNavigationManager.GetForCurrentView().BackRequested -= System_BackRequested;
                CoreApplication.GetCurrentView().TitleBar.LayoutMetricsChanged -= TitleBar_LayoutMetricsChanged;
            }
            SettingsHelper.LoginChanged -= OnLoginChanged;
            if (ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons"))
            { HardwareButtons.BackPressed -= System_BackPressed; }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.IsAppWindow())
            {
                this.GetWindowForElement().Changed += AppWindow_Changed;
            }
            else
            {
                Window.Current?.SetTitleBar(DragRegion);
                SystemNavigationManager.GetForCurrentView().BackRequested += System_BackRequested;
                CoreApplication.GetCurrentView().TitleBar.LayoutMetricsChanged += TitleBar_LayoutMetricsChanged;
                if (ApiInformation.IsMethodPresent("Windows.UI.Composition.Compositor", "TryCreateBlurredWallpaperBackdropBrush"))
                { BackdropMaterial.SetApplyToRootOrPageBackground(this, true); }
            }
        }

        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            RightPaddingColumn.Width = new GridLength(sender.TitleBar.IsVisible ? 188 : 0);
        }

        private void TitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args) => UpdateAppTitle(sender);

        public string GetAppTitleFromSystem => Package.Current.DisplayName;

        private async void OpenActivatedEventArgs(IActivatedEventArgs args)
        {
            if (!await NavigationViewFrame.OpenActivatedEventArgs(args))
            {
                NavigationView_Navigate("Home", new EntranceNavigationTransitionInfo());
            }
        }

        private void OnIsStretchProperty(DependencyObject sender, DependencyProperty dp)
        {
            if (sender is Slot)
            {
                UpdateAppTitleIcon();
            }
        }

        private void OnIsBackButtonVisibleChanged(DependencyObject sender, DependencyProperty dp)
        {
            UpdateLeftPaddingColumn();
            UpdateAppTitleIcon();
        }

        private void NavigationView_Navigate(string NavItemTag, NavigationTransitionInfo TransitionInfo, object vs = null)
        {
            Type _page = null;

            (string Tag, Type Page) item = _pages.FirstOrDefault(p => p.Tag.Equals(NavItemTag, StringComparison.Ordinal));
            _page = item.Page;
            // Get the page type before navigation so you can prevent duplicate
            // entries in the back stack.
            Type PreNavPageType = NavigationViewFrame.CurrentSourcePageType;

            // Only navigate if the selected page isn't currently loaded.
            if (_page != null && !Equals(PreNavPageType, _page))
            {
                _ = NavigationViewFrame.Navigate(_page, vs, TransitionInfo);
            }
        }

        private void NavigationView_BackRequested(muxc.NavigationView sender, muxc.NavigationViewBackRequestedEventArgs args) => _ = TryGoBack();

        private void NavigationView_ItemInvoked(muxc.NavigationView sender, muxc.NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer != null)
            {
                string NavItemTag = args.InvokedItemContainer.Tag.ToString();
                NavigationView_Navigate(NavItemTag, args.RecommendedNavigationTransitionInfo);
            }
        }

        private bool TryGoBack()
        {
            if (!Dispatcher.HasThreadAccess)
            { return false; }

            if (!NavigationViewFrame.CanGoBack)
            { return false; }

            // Don't go back if the nav pane is overlayed.
            if (NavigationView.IsPaneOpen &&
                (NavigationView.DisplayMode == muxc.NavigationViewDisplayMode.Compact ||
                 NavigationView.DisplayMode == muxc.NavigationViewDisplayMode.Minimal))
            { return false; }

            NavigationViewFrame.GoBack();
            return true;
        }

        private void On_Navigated(object _, NavigationEventArgs e)
        {
            NavigationView.IsBackEnabled = NavigationViewFrame.CanGoBack;
            NavigationView.IsBackButtonVisible = NavigationViewFrame.CanGoBack
                ? muxc.NavigationViewBackButtonVisible.Visible
                : muxc.NavigationViewBackButtonVisible.Collapsed;
            if (NavigationViewFrame.SourcePageType != null)
            {
                (string Tag, Type Page) item = _pages.FirstOrDefault(p => p.Page == e.SourcePageType);
                if (item.Tag != null)
                {
                    muxc.NavigationViewItem SelectedItem = NavigationView.MenuItems
                        .OfType<muxc.NavigationViewItem>()
                        .FirstOrDefault(n => n.Tag.Equals(item.Tag))
                            ?? NavigationView.FooterMenuItems
                                .OfType<muxc.NavigationViewItem>()
                                .FirstOrDefault(n => n.Tag.Equals(item.Tag));
                    NavigationView.SelectedItem = SelectedItem;
                }
            }
            UIHelper.HideProgressBar();
        }

        private void NavigationViewControl_PaneClosing(muxc.NavigationView sender, muxc.NavigationViewPaneClosingEventArgs args)
        {
            UpdateLeftPaddingColumn();
        }

        private void NavigationViewControl_PaneOpening(muxc.NavigationView sender, object args)
        {
            UpdateLeftPaddingColumn();
        }

        private void UpdateLeftPaddingColumn()
        {
            LeftPaddingColumn.Width = NavigationView.PaneDisplayMode == muxc.NavigationViewPaneDisplayMode.Top
                ? NavigationView.IsBackButtonVisible != muxc.NavigationViewBackButtonVisible.Collapsed
                    ? new GridLength(48) : new GridLength(0)
                    : NavigationView.DisplayMode == muxc.NavigationViewDisplayMode.Minimal
                        ? NavigationView.IsPaneOpen ? new GridLength(72)
                        : NavigationView.IsPaneToggleButtonVisible
                            ? NavigationView.IsBackButtonVisible != muxc.NavigationViewBackButtonVisible.Collapsed
                            ? new GridLength(88) : new GridLength(48)
                                : NavigationView.IsBackButtonVisible != muxc.NavigationViewBackButtonVisible.Collapsed
                                ? new GridLength(48) : new GridLength(0)
                                    : NavigationView.IsBackButtonVisible != muxc.NavigationViewBackButtonVisible.Collapsed
                                    ? new GridLength(48) : new GridLength(0);
        }

        private void NavigationViewControl_DisplayModeChanged(muxc.NavigationView sender, muxc.NavigationViewDisplayModeChangedEventArgs args)
        {
            UpdateLeftPaddingColumn();
            UpdateAppTitleIcon();
        }

        private void UpdateAppTitleIcon()
        {
            AppTitleIcon.Margin = SearchBoxHolder.IsStretch
                && NavigationView.PaneDisplayMode != muxc.NavigationViewPaneDisplayMode.Top
                && NavigationView.DisplayMode != muxc.NavigationViewDisplayMode.Minimal
                    ? NavigationView.IsBackButtonVisible == muxc.NavigationViewBackButtonVisible.Visible
                        ? new Thickness(0, 0, 16, 0)
                        : new Thickness(28.5, 0, 28, 0)
                    : NavigationView.IsBackButtonVisible == muxc.NavigationViewBackButtonVisible.Visible
                        || (NavigationView.DisplayMode == muxc.NavigationViewDisplayMode.Minimal
                            && NavigationView.PaneDisplayMode != muxc.NavigationViewPaneDisplayMode.Top
                            && NavigationView.IsPaneToggleButtonVisible)
                        ? new Thickness(0, 0, 16, 0)
                        : new Thickness(16, 0, 16, 0);
        }

        private void OnLoginChanged(string sender, bool args) => _ = Dispatcher.AwaitableRunAsync(() => SetUserAvatar(args));

        private async void SetUserAvatar(bool isLogin)
        {
            if (isLogin && await SettingsHelper.CheckLoginAsync())
            {
                string UID = SettingsHelper.Get<string>(SettingsHelper.Uid);
                if (!string.IsNullOrEmpty(UID))
                {
                    (string UID, string UserName, string UserAvatar) results = await NetworkHelper.GetUserInfoByNameAsync(UID);
                    if (results.UID != UID) { return; }
                    UserName = results.UserName;
                    UserAvatar = new BitmapImage(new Uri(results.UserAvatar));
                }
            }
            else
            {
                UserName = null;
                UserAvatar = null;
            }
        }

        private void UpdateAppTitle(CoreApplicationViewTitleBar coreTitleBar)
        {
            //ensure the custom title bar does not overlap window caption controls
            RightPaddingColumn.Width = new GridLength(coreTitleBar.SystemOverlayRightInset);
        }

        private void System_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = TryGoBack();
            }
        }

        private void System_BackPressed(object sender, BackPressedEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = TryGoBack();
            }
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as FrameworkElement).Tag.ToString())
            {
                case "Login":
                    NavigationViewFrame.Navigate(typeof(BrowserPage), new BrowserViewModel(UriHelper.LoginUri));
                    break;
                case "Logout":
                    SettingsHelper.Logout();
                    break;
                case "Settings":
                    NavigationView.SelectedItem = NavigationView.FooterMenuItems.LastOrDefault();
                    break;
                case "CreateFeed":
                    new CreateFeedControl
                    {
                        FeedType = CreateFeedType.Feed,
                        PopupTransitions = new TransitionCollection
                        {
                            new EdgeUIThemeTransition
                            {
                                Edge = EdgeTransitionLocation.Bottom
                            }
                        }
                    }.Show(this);
                    break;
            }
        }

        #region 搜索框

        private async void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                ObservableCollection<object> observableCollection = new ObservableCollection<object>();
                sender.ItemsSource = observableCollection;
                string keyWord = sender.Text;
                await ThreadSwitcher.ResumeBackgroundAsync();
                (bool isSucceed, JToken result) = await RequestHelper.GetDataAsync(UriHelper.GetUri(UriType.SearchWords, keyWord), true);
                if (isSucceed && result != null && result is JArray array && array.Count > 0)
                {
                    foreach (JToken token in array)
                    {
                        switch (token.Value<string>("entityType"))
                        {
                            case "apk":
                                await Dispatcher.AwaitableRunAsync(() => observableCollection.Add(new SearchWord(token as JObject)));
                                break;
                            case "searchWord":
                            default:
                                await Dispatcher.AwaitableRunAsync(() => observableCollection.Add(new SearchWord(token as JObject)));
                                break;
                        }
                    }
                }
            }
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            //if (args.ChosenSuggestion is AppModel app)
            //{
            //    UIHelper.NavigateInSplitPane(typeof(AppPages.AppPage), "https://www.coolapk.com" + app.Url);
            //}
            //else
            if (args.ChosenSuggestion is SearchWord word)
            {
                NavigationViewFrame.Navigate(typeof(SearchingPage), new SearchingViewModel(word.ToString(), word.Glyph == "\uE77B" ? 1 : -1));
            }
            else if (args.ChosenSuggestion is null && !string.IsNullOrEmpty(sender.Text))
            {
                NavigationViewFrame.Navigate(typeof(SearchingPage), new SearchingViewModel(sender.Text));
            }
        }

        #endregion

        #region 状态栏

        public async void ShowProgressBar()
        {
            if (!Dispatcher.HasThreadAccess)
            {
                await Dispatcher.ResumeForegroundAsync();
            }
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = true;
            ProgressBar.ShowError = false;
            ProgressBar.ShowPaused = false;
        }

        public async void ShowProgressBar(double value)
        {
            if (!Dispatcher.HasThreadAccess)
            {
                await Dispatcher.ResumeForegroundAsync();
            }
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = false;
            ProgressBar.ShowError = false;
            ProgressBar.ShowPaused = false;
            ProgressBar.Value = value;
        }

        public async void PausedProgressBar()
        {
            if (!Dispatcher.HasThreadAccess)
            {
                await Dispatcher.ResumeForegroundAsync();
            }
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = true;
            ProgressBar.ShowError = false;
            ProgressBar.ShowPaused = true;
        }

        public async void ErrorProgressBar()
        {
            if (!Dispatcher.HasThreadAccess)
            {
                await Dispatcher.ResumeForegroundAsync();
            }
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = true;
            ProgressBar.ShowPaused = false;
            ProgressBar.ShowError = true;
        }

        public async void HideProgressBar()
        {
            if (!Dispatcher.HasThreadAccess)
            {
                await Dispatcher.ResumeForegroundAsync();
            }
            ProgressBar.Visibility = Visibility.Collapsed;
            ProgressBar.IsIndeterminate = false;
            ProgressBar.ShowError = false;
            ProgressBar.ShowPaused = false;
            ProgressBar.Value = 0;
        }

        public async void ShowMessage(string message = null)
        {
            if (!Dispatcher.HasThreadAccess)
            {
                await Dispatcher.ResumeForegroundAsync();
            }

            AppTitleText.Text = message ?? ResourceLoader.GetForViewIndependentUse().GetString("AppName") ?? "酷安";

            if (this.IsAppWindow())
            {
                this.GetWindowForElement().Title = message ?? string.Empty;
            }
            else
            {
                ApplicationView.GetForCurrentView().Title = message ?? string.Empty;
            }
        }

        #endregion
    }
}
