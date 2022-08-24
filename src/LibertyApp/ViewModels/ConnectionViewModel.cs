using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotRas;
using LibertyApp.Language;
using LibertyApp.Models;
using LibertyApp.Properties;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace LibertyApp.ViewModels;

public class ConnectionViewModel : ObservableObject
{
	#region Fields

	private static readonly RasDialer _dialer = new()
	{
		EntryName = Resources.ConnectionName,
		PhoneBookPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Network\Connections\Pbk\rasphone.pbk"),
	};
	private static readonly RasConnectionWatcher _watcher = new();

	/// <summary>
	/// Connection timer
	/// </summary>
	private readonly DispatcherTimer _dispatcherTimer;

	/// <summary>
	/// Connection start value
	/// </summary>
	private DateTime _startTime;

	/// <summary>
	/// Connection control background images of states
	/// </summary>
	private static readonly ImageBrush[] BackgroundImageStates =
	{
		new(new BitmapImage(new Uri("pack://application:,,,/Resources/background.jpg"))),
		new(new BitmapImage(new Uri("pack://application:,,,/Resources/background-connecting.jpg"))),
		new(new BitmapImage(new Uri("pack://application:,,,/Resources/background-connected.jpg")))
	};

	#endregion

	#region Public properties

	public ConnectionSpeed ConnectionSpeed { get; }

	public Brush BackgroundImage
	{
		get => _backgroundImage;
		private set => SetProperty(ref _backgroundImage, value);
	}

	private Brush _backgroundImage;

	/// <summary>
	/// Connection button state text and tooltip
	/// </summary>
	public string ConnectButtonText
	{
		get => _connectButtonText;
		private set => SetProperty(ref _connectButtonText, value);
	}

	private string _connectButtonText = Strings.ConnectText;

	/// <summary>
	/// Connection state text
	/// </summary>
	public string ConnectionState
	{
		get => _connectionState;
		private set => SetProperty(ref _connectionState, value);
	}

	private string _connectionState = Strings.StatusDisconnected;

	/// <summary>
	/// Connection timer value
	/// </summary>
	public TimeSpan Timer
	{
		get => _timer;
		private set => SetProperty(ref _timer, value);
	}

	private TimeSpan _timer;

	/// <summary>
	/// Connection state value
	/// </summary>
	public bool IsConnected
	{
		get => _isConnected;
		private set => SetProperty(ref _isConnected, value);
	}

	private bool _isConnected = false;

	private RasConnection Connection
	{
		get => _connection;
		set => SetProperty(ref _connection, value);
	}
	private RasConnection _connection;

	#endregion

	#region Constructors

	public ConnectionViewModel()
	{
		_watcher.Disconnected += (_, _) =>
		{
			_dispatcherTimer.Stop();

			Timer = TimeSpan.Zero;

			IsConnected = false;

			ShowDefaultAssets();

			ConnectButtonText = Strings.ConnectText;
			ConnectionState = Strings.StatusDisconnected;

			App.Current.NotifyIcon.ShowBalloonTip(100, Strings.AppName, Strings.StatusDisconnected, ToolTipIcon.Info);
		};

		BackgroundImage = BackgroundImageStates[0];

		ConnectionSpeed = new ConnectionSpeed();

		_dispatcherTimer = new DispatcherTimer
		{
			Interval = new TimeSpan(0, 0, 1),
		};

		_dispatcherTimer.Tick += (_, _) =>
		{
			Timer = DateTime.Now - _startTime;
			CommandManager.InvalidateRequerySuggested();
			ConnectionSpeed.CalculateDownloadSpeed();
			ConnectionSpeed.CalculateUploadSpeed();
		};

		ConnectCommandAsync = new AsyncRelayCommand(ConnectAsync);
	}

	#endregion

	#region Commands

	/// <summary>
	/// Connection relay command
	/// </summary>
	public IAsyncRelayCommand ConnectCommandAsync { get; }


	/// <summary>
	/// Connection relay command handler
	/// </summary>
	private async Task ConnectAsync()
	{
		try
		{
			// if connected already
			if (IsConnected)
			{
				// change interface properties
				ShowConnectingAssets();
				ConnectButtonText = ConnectionState = Strings.DisconnectingText;

				await Connection.DisconnectAsync(System.Threading.CancellationToken.None);
				//_watcher.Stop();

				_dispatcherTimer.Stop();

				Timer = TimeSpan.Zero;

				IsConnected = false;

				ShowDefaultAssets();

				ConnectButtonText = Strings.ConnectText;
				ConnectionState = Strings.StatusDisconnected;

				App.Current.NotifyIcon.ShowBalloonTip(100, Strings.AppName, Strings.StatusDisconnected, ToolTipIcon.Info);
			}
			// if not connected already
			else
			{
				// change interface properties
				ShowConnectingAssets();
				ConnectButtonText = Strings.ConnectingText;
				ConnectionState = Strings.StatusConnecting;

				Connection = await _dialer.ConnectAsync();
				_watcher.Connection = Connection;

				_startTime = DateTime.Now;
				_dispatcherTimer.Start();
				IsConnected = true;
				ShowConnectedAssets();
				ConnectButtonText = Strings.DisconnectText;
				ConnectionState = Strings.StatusConnected;

				App.Current.NotifyIcon.ShowBalloonTip(100, Strings.AppName, Strings.StatusConnected, ToolTipIcon.Info);

				_watcher.Start();
			}
		}
		catch (Exception e)
		{
			App.Current.NotifyIcon.ShowBalloonTip(3000, Strings.ConnectionErrorCaption, e.Message, ToolTipIcon.Error);

			// change interface properties
			IsConnected = false;
			ShowDefaultAssets();
			ConnectButtonText = Strings.ConnectText;
			ConnectionState = Strings.StatusDisconnected;
		}
	}

	#endregion

	#region Private methods

	/// <summary>
	/// Set control default background state
	/// </summary>
	private void ShowDefaultAssets() => BackgroundImage = BackgroundImageStates[0];

	/// <summary>
	/// Set control background state to connecting
	/// </summary>
	private void ShowConnectingAssets() => BackgroundImage = BackgroundImageStates[1];

	/// <summary>
	/// Set control background state to connected
	/// </summary>
	private void ShowConnectedAssets() => BackgroundImage = BackgroundImageStates[2];

	#endregion
}