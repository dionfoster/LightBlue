﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using LightBlue.MultiHost.Configuration;
using LightBlue.MultiHost.ViewModel;

namespace LightBlue.MultiHost
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow :  INotifyPropertyChanged
    {
        private Role _selectedItem;
        private string _searchText;
        public CollectionViewSource CollectionViewSource { get; set; }
        public ObservableCollection<Role> Services { get; set; }
        public ImageSource MainIcon { get { return ImageUtil.ColourImage(@"Resources\debug.ico", CustomBrushes.Main); } }


        public Role SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;
                OnPropertyChanged("SelectedItem");
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                string msg = null;
                try
                {
                    msg = e.ExceptionObject.ToString();
                }
                catch (Exception ex)
                {
                    msg = "Couldn't get exception details: " + ex.Message;
                }

                try
                {
                    MessageBox.Show(msg);
                }
                catch
                {
                    Debugger.Launch();
                    Debugger.Break();
                }
            };
            App.Current.DispatcherUnhandledException += (s, e) =>
            {
                string msg = null;
                try
                {
                    msg = e.Exception.ToString();
                }
                catch (Exception ex)
                {
                    msg = "Couldn't get exception details: " + ex.Message;
                }

                try
                {
                    MessageBox.Show(msg);
                }
                catch
                {
                    Debugger.Launch();
                    Debugger.Break();
                }
            };

            CollectionViewSource = new CollectionViewSource();

            Services = new ObservableCollection<Role>();

            foreach (var h in App.Configuration.Roles)
            {
                var r = new Role(h);
                Services.Add(r);
                r.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == "Status") CollectionViewSource.View.Refresh();
                };
            }

            CollectionViewSource.Filter += CollectionViewSource_Filter;

            CollectionViewSource.Source = Services;

            DataContext = this;

            var autos = Services.Where(x => x.Status == RoleStatus.Sequenced).ToArray();
            BeginAutoStart(autos);
        }

        public string Version
        {
            get
            {
                return Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    .InformationalVersion;
            }
        }

        async void BeginAutoStart(Role[] roles)
        {
            await Task.Delay(TimeSpan.FromSeconds(0.2));
            foreach (var r in roles) r.StartAutomatically();
        }

        void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_searchText)) e.Accepted = true;
            else
            {
                var r = (Role)e.Item;
                e.Accepted = r.Title.ToLowerInvariant().Contains(_searchText.ToLowerInvariant())
                             || r.Status.ToString().ToLowerInvariant().Contains(_searchText.ToLowerInvariant())
                             || r.RoleName.ToLowerInvariant().Contains(_searchText.ToLowerInvariant());
            }
        }

        private void Start_OnClick(object sender, RoutedEventArgs e)
        {
            if (VerifiySelection())
            {
                var copy = listView.SelectedItems.Cast<Role>().ToArray();
                foreach (var r in copy)
                {
                    r.Start();
                }
            }
        }

        private bool VerifiySelection()
        {
            if (listView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Can't perform action: you don't have any services selected.");
                return false;
            }
            return true;
        }

        private void Stop_OnClick(object sender, RoutedEventArgs e)
        {
            if (VerifiySelection())
            {
                var copy = listView.SelectedItems.Cast<Role>().ToArray();
                foreach (var r in copy)
                {
                    r.Stop();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var d = PropertyChanged;
            if (d != null) d(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Debug_OnClick(object sender, RoutedEventArgs e)
        {
            Debugger.Launch();
        }

        private void DebugIis_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null && SelectedItem.IsIisExpress)
            {
                SelectedItem.DebugIisExpress();
            }
        }

        private void FilterTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = FilterTextBox.Text;
            CollectionViewSource.View.Refresh();
        }

        private void EditConfiguration_OnClick(object sender, RoutedEventArgs e)
        {
            if (VerifiySingleSelection())
            {
                var role = (Role)listView.SelectedItem;
                if (role.Status == RoleStatus.Stopped)
                {
                    var service = new RoleConfigurationService();
                    var changed = service.Edit(role.Title, App.MultiHostConfigurationFilePath);
                    if (changed != null)
                    {
                        changed.EnabledOnStartup = false;

                        var configDir = Path.GetDirectoryName(App.MultiHostConfigurationFilePath);
                        changed.ConfigurationPath = Path.GetFullPath(Path.Combine(configDir, changed.ConfigurationPath));
                        changed.Assembly = Path.GetFullPath(Path.Combine(configDir, changed.Assembly));

                        var newRole = new Role(changed);
                        newRole.Logs = role.Logs;
                        
                        var index = listView.SelectedIndex;
                        Services.RemoveAt(index);
                        Services.Insert(index, newRole);

                        listView.SelectedIndex = index;
                    }
                }
                else
                {
                    MessageBox.Show(role.Title + " must be stopped if you want to edit its configuration.");
                }
            }
        }

        private bool VerifiySingleSelection()
        {
            if (listView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Can't perform action: you don't have any services selected.");
                return false;
            }
            if (listView.SelectedItems.Count > 1)
            {
                MessageBox.Show("Can't perform action: you have more than one service selected.");
                return false;
            }
            return true;
        }
    }
}

