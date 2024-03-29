﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Maple;
using Newtonsoft.Json;
using PlantMonitorApp.Azure;
using Xamarin.Forms;

namespace PlantMonitorApp
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public static int HIGH = 1;
        public static int MEDIUM = 2;
        public static int LOW = 3;

        RestClient plantClient;

        bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); }
        }

        bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set { _isRefreshing = value; OnPropertyChanged(nameof(IsRefreshing)); }
        }

        bool _isEmpty;
        public bool IsEmpty
        {
            get => _isEmpty;
            set { _isEmpty = value; OnPropertyChanged(nameof(IsEmpty)); }
        }

        ServerItem _selectedServer;
        public ServerItem SelectedServer
        {
            get => _selectedServer;
            set { _selectedServer = value; OnPropertyChanged(nameof(SelectedServer)); }
        }

        public ObservableCollection<ServerItem> ServerList { get; set; }
        public ObservableCollection<HumidityModel> LevelList { get; set; }

        public Command GetHumidityCommand { private set; get; }
        public Command RefreshServersCommand { private set; get; }

        public MainViewModel()
        {
            plantClient = new RestClient();

            LevelList = new ObservableCollection<HumidityModel>();
            ServerList = new ObservableCollection<ServerItem>();

            GetHumidityCommand = new Command(async (s) => await LoadAsync());
            RefreshServersCommand = new Command(async () => await GetServersAsync());

            LoadAsync();
        }

        async Task LoadAsync()
        {
            if (IsBusy)
                return;
            IsBusy = true;

            await BlobManager.DownloadLogFileAsync();
            string json = File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + App.LOG_FILE_NAME);
            var list = JsonConvert.DeserializeObject<List<HumidityModel>>(json);

            LevelList.Clear();
            foreach (var item in list)
                LevelList.Add(item);

            IsBusy = false;
            IsRefreshing = false;
        }

        async Task GetServersAsync()
        {
            if (IsBusy)
                return;
            IsBusy = true;

            ServerList.Clear();

            var servers = await plantClient.FindMapleServersAsync();
            foreach (var server in servers)
            {
                if (server.Name.Contains("Plant"))
                {
                    ServerList.Add(server);
                }
            }

            if (servers.Count > 0)
            {
                SelectedServer = ServerList[0];
                await GetHumidityCommandExecute();
            }

            IsEmpty = servers.Count == 0;

            IsBusy = false;
        }

        async Task GetHumidityCommandExecute()
        {
            if (SelectedServer == null)
                return;

            LevelList.Clear();

            var humitidyLogs = await plantClient.GetHumidityAsync(SelectedServer);
            foreach (var log in humitidyLogs)
            {
                int humidity = (int) (log.Humidity * 100);

                LevelList.Insert(0, new HumidityModel()
                {
                    Humidity = humidity,
                    Level = (humidity >= 75) ? HIGH : (humidity >= 50) ? MEDIUM : LOW,
                    Date = DateTime.Now.ToString("hh:mm tt dd/MMM/yyyy")
                });
            }

            IsRefreshing = false;
        }

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }
}