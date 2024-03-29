﻿using CarRemote.Client;
using Maple;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace CarRemote
{
    public class MainViewModel : INotifyPropertyChanged
    {
        CarClient carClient;

        #region Property/Fields
        bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set { _isConnected = value; OnPropertyChanged(nameof(IsConnected)); }
        }

        bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); }
        }

        bool isButtonUpPressed;
        public bool IsButtonUpPressed
        {
            get => isButtonUpPressed;
            set { isButtonUpPressed = value; OnPropertyChanged(nameof(IsButtonUpPressed)); }
        }

        bool isButtonDownPressed;
        public bool IsButtonDownPressed
        {
            get => isButtonDownPressed;
            set { isButtonDownPressed = value; OnPropertyChanged(nameof(IsButtonDownPressed)); }
        }

        bool isButtonLeftPressed;
        public bool IsButtonLeftPressed
        {
            get => isButtonLeftPressed;
            set { isButtonLeftPressed = value; OnPropertyChanged(nameof(IsButtonLeftPressed)); }
        }

        bool isButtonRightPressed;
        public bool IsButtonRightPressed
        {
            get => isButtonRightPressed;
            set { isButtonRightPressed = value; OnPropertyChanged(nameof(IsButtonRightPressed)); }
        }

        bool _isServerListEmpty;
        public bool IsServerListEmpty
        {
            get => _isServerListEmpty;
            set { _isServerListEmpty = value; OnPropertyChanged(nameof(IsServerListEmpty)); }
        }

        string status;
        public string Status
        {
            get => status;
            set { status = value; OnPropertyChanged(nameof(Status)); }
        }

        ServerItem _selectedServer;
        public ServerItem SelectedServer
        {
            get => _selectedServer;
            set { _selectedServer = value; OnPropertyChanged(nameof(SelectedServer)); }
        }

        public ObservableCollection<ServerItem> ServerList { get; set; }

        public Command RefreshServersCommand { private set; get; }
        #endregion

        public MainViewModel()
        {
            ServerList = new ObservableCollection<ServerItem>();

            RefreshServersCommand = new Command(async ()=> { await GetServersAsync(); });

            carClient = new CarClient();

            GetServersAsync();
        }

        async Task GetServersAsync()
        {
            if (IsBusy)
                return;
            IsBusy = true;

            try
            {
                Status = "Searching...";
                ServerList.Clear();

                var servers = await carClient.FindMapleServersAsync();
                foreach (var server in servers)
                    ServerList.Add(server);

                if (servers.Count > 0)
                {
                    SelectedServer = ServerList[0];
                    Status = "Connected";
                    IsConnected = true;
                }
                else
                {
                    Status = "No cars found";
                    IsConnected = false;
                }

                IsServerListEmpty = servers.Count == 0;
            }
            catch(Exception ex)
            {
                Debug.Print(ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task SendCommandAsync(string command)
        {
            bool isSuccessful = false;
            Status = "Sending Command...";

            switch (command)
            {
                case CommandConstants.STOP:
                    if (isSuccessful = await carClient.StopAsync(SelectedServer))
                        IsButtonUpPressed = IsButtonDownPressed = IsButtonLeftPressed = IsButtonRightPressed = false;
                    break;

                case CommandConstants.TURN_LEFT:
                    if (isSuccessful = await carClient.TurnLeftAsync(SelectedServer))
                        IsButtonLeftPressed = true;
                    break;

                case CommandConstants.TURN_RIGHT:
                    if (isSuccessful = await carClient.TurnRightAsync(SelectedServer))
                        IsButtonRightPressed = true;
                    break;

                case CommandConstants.MOVE_FORWARD:
                    if (isSuccessful = await carClient.MoveForwardAsync(SelectedServer))
                        IsButtonUpPressed = true;
                    break;

                case CommandConstants.MOVE_BACKWARD:
                    if (isSuccessful = await carClient.MoveBackwardAsync(SelectedServer))
                        IsButtonDownPressed = true;
                    break;
            }

            if (isSuccessful)
            {
                Status = "Connected";
            }
            else
            {   
                IsConnected = IsButtonUpPressed = IsButtonDownPressed = IsButtonLeftPressed = IsButtonRightPressed = false;
                await GetServersAsync();
            }
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