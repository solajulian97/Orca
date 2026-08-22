#region Using declarations
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
#endregion

namespace NinjaTrader.NinjaScript.AddOns
{
	public enum OrcaTradeRecorderState
	{
		Off,
		Starting,
		Armed,
		Recording,
		Tail,
		Finalizing,
		Error
	}

	public sealed class OrcaTradeRecorderAddOn : AddOnBase
	{
		private static readonly object RuntimeSync = new object();
		private static OrcaTradeRecorderRuntime runtime;
		private NTMenuItem menuItem;
		private NTMenuItem hostMenu;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults) {
				Description = "Automatically records active NinjaTrader positions through OBS Studio";
				Name = "Orca Trade Recorder";
			} else if (State == State.Terminated) {
				DisposeRuntime();
			}
		}

		protected override void OnWindowCreated(Window window)
		{
			ControlCenter controlCenter = window as ControlCenter;
			if (controlCenter == null || menuItem != null)
				return;

			OrcaTradeRecorderRuntime recorder = GetOrCreateRuntime(controlCenter.Dispatcher);
			hostMenu = controlCenter.FindFirst("ControlCenterMenuItemTools") as NTMenuItem
				?? controlCenter.FindFirst("toolsMenuItem") as NTMenuItem
				?? controlCenter.FindFirst("ControlCenterMenuItemNew") as NTMenuItem;
			if (hostMenu == null) {
				OrcaTradeRecorderDiagnostics.Write("Control Center Tools menu was not found.");
				return;
			}

			menuItem = new NTMenuItem {
				Header = "Orca Trade Recorder",
				Style = Application.Current == null ? null : Application.Current.TryFindResource("MainMenuItem") as Style
			};
			menuItem.Click += OnMenuItemClick;
			hostMenu.Items.Add(menuItem);
			recorder.NotifyControlCenterReady();
		}

		protected override void OnWindowDestroyed(Window window)
		{
			ControlCenter controlCenter = window as ControlCenter;
			if (controlCenter == null || menuItem == null || hostMenu == null)
				return;
			menuItem.Click -= OnMenuItemClick;
			hostMenu.Items.Remove(menuItem);
			menuItem = null;
			hostMenu = null;
		}

		private void OnMenuItemClick(object sender, RoutedEventArgs e)
		{
			Dispatcher dispatcher = Application.Current == null ? Dispatcher.CurrentDispatcher : Application.Current.Dispatcher;
			OrcaTradeRecorderRuntime recorder = GetOrCreateRuntime(dispatcher);
			dispatcher.InvokeAsync(() => OrcaTradeRecorderWindow.ShowOrActivate(recorder));
		}

		private static OrcaTradeRecorderRuntime GetOrCreateRuntime(Dispatcher dispatcher)
		{
			lock (RuntimeSync) {
				if (runtime == null)
					runtime = new OrcaTradeRecorderRuntime(dispatcher);
				return runtime;
			}
		}

		private static void DisposeRuntime()
		{
			OrcaTradeRecorderRuntime recorder;
			lock (RuntimeSync) {
				recorder = runtime;
				runtime = null;
			}
			if (recorder != null)
				recorder.Dispose();
		}
	}

	[Serializable]
	public sealed class OrcaTradeRecorderSettings
	{
		public string ObsExecutablePath { get; set; }
		public string ObsHost { get; set; }
		public int ObsPort { get; set; }
		public string ProtectedObsPassword { get; set; }
		public string ObsProfile { get; set; }
		public string ObsSceneCollection { get; set; }
		public string ObsScene { get; set; }
		public string MicrophoneInput { get; set; }
		public string OutputDirectory { get; set; }
		public string FfmpegPath { get; set; }
		public int PreRollSeconds { get; set; }
		public int PostRollSeconds { get; set; }
		public double MinimumFreeSpaceGb { get; set; }
		public bool AutoLaunchObs { get; set; }

		[XmlIgnore]
		public string ObsPassword { get; set; }

		public OrcaTradeRecorderSettings()
		{
			ObsExecutablePath = OrcaTradeRecorderSettingsStore.FindDefaultObsPath();
			ObsHost = "127.0.0.1";
			ObsPort = 4455;
			ObsProfile = "Orca Trade Recorder";
			ObsSceneCollection = "Orca Trade Recorder";
			ObsScene = "Trading Monitor";
			MicrophoneInput = "Mic/Aux";
			OutputDirectory = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
				"NinjaTrader 8", "OrcaTradeRecordings");
			FfmpegPath = OrcaTradeRecorderSettingsStore.FindOnPath("ffmpeg.exe");
			PreRollSeconds = 15;
			PostRollSeconds = 15;
			MinimumFreeSpaceGb = 5;
			AutoLaunchObs = true;
		}

		public OrcaTradeRecorderSettings Clone()
		{
			return new OrcaTradeRecorderSettings {
				ObsExecutablePath = ObsExecutablePath,
				ObsHost = ObsHost,
				ObsPort = ObsPort,
				ProtectedObsPassword = ProtectedObsPassword,
				ObsPassword = ObsPassword,
				ObsProfile = ObsProfile,
				ObsSceneCollection = ObsSceneCollection,
				ObsScene = ObsScene,
				MicrophoneInput = MicrophoneInput,
				OutputDirectory = OutputDirectory,
				FfmpegPath = FfmpegPath,
				PreRollSeconds = PreRollSeconds,
				PostRollSeconds = PostRollSeconds,
				MinimumFreeSpaceGb = MinimumFreeSpaceGb,
				AutoLaunchObs = AutoLaunchObs
			};
		}
	}

	public static class OrcaTradeRecorderSettingsStore
	{
		private static readonly object Sync = new object();

		public static string SettingsPath
		{
			get {
				return Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
					"NinjaTrader 8", "OrcaTradeRecorder.xml");
			}
		}

		public static OrcaTradeRecorderSettings Load()
		{
			lock (Sync) {
				OrcaTradeRecorderSettings settings = null;
				try {
					if (File.Exists(SettingsPath)) {
						using (FileStream stream = File.OpenRead(SettingsPath))
							settings = (OrcaTradeRecorderSettings)new XmlSerializer(typeof(OrcaTradeRecorderSettings)).Deserialize(stream);
					}
				} catch (Exception ex) {
					OrcaTradeRecorderDiagnostics.Write("Settings load failed: " + ex.Message);
				}
				settings = Normalize(settings ?? new OrcaTradeRecorderSettings());
				settings.ObsPassword = Unprotect(settings.ProtectedObsPassword);
				return settings;
			}
		}

		public static OrcaTradeRecorderSettings Save(OrcaTradeRecorderSettings input)
		{
			lock (Sync) {
				OrcaTradeRecorderSettings settings = Normalize(input == null ? new OrcaTradeRecorderSettings() : input.Clone());
				settings.ProtectedObsPassword = Protect(settings.ObsPassword);
				string directory = Path.GetDirectoryName(SettingsPath);
				Directory.CreateDirectory(directory);
				string temporary = SettingsPath + ".tmp";
				using (FileStream stream = File.Create(temporary))
					new XmlSerializer(typeof(OrcaTradeRecorderSettings)).Serialize(stream, settings);
				if (File.Exists(SettingsPath)) {
					string backup = SettingsPath + ".bak";
					try { File.Replace(temporary, SettingsPath, backup, true); }
					catch {
						File.Copy(temporary, SettingsPath, true);
						File.Delete(temporary);
					}
				} else {
					File.Move(temporary, SettingsPath);
				}
				return settings;
			}
		}

		private static OrcaTradeRecorderSettings Normalize(OrcaTradeRecorderSettings settings)
		{
			if (settings == null)
				settings = new OrcaTradeRecorderSettings();
			settings.ObsExecutablePath = (settings.ObsExecutablePath ?? string.Empty).Trim();
			settings.ObsHost = string.IsNullOrWhiteSpace(settings.ObsHost) ? "127.0.0.1" : settings.ObsHost.Trim();
			settings.ObsPort = Math.Max(1, Math.Min(65535, settings.ObsPort <= 0 ? 4455 : settings.ObsPort));
			settings.ObsProfile = string.IsNullOrWhiteSpace(settings.ObsProfile) ? "Orca Trade Recorder" : settings.ObsProfile.Trim();
			settings.ObsSceneCollection = string.IsNullOrWhiteSpace(settings.ObsSceneCollection) ? "Orca Trade Recorder" : settings.ObsSceneCollection.Trim();
			settings.ObsScene = string.IsNullOrWhiteSpace(settings.ObsScene) ? "Trading Monitor" : settings.ObsScene.Trim();
			settings.MicrophoneInput = string.IsNullOrWhiteSpace(settings.MicrophoneInput) ? "Mic/Aux" : settings.MicrophoneInput.Trim();
			if (string.IsNullOrWhiteSpace(settings.OutputDirectory))
				settings.OutputDirectory = new OrcaTradeRecorderSettings().OutputDirectory;
			settings.OutputDirectory = Environment.ExpandEnvironmentVariables(settings.OutputDirectory.Trim());
			settings.FfmpegPath = string.IsNullOrWhiteSpace(settings.FfmpegPath) ? FindOnPath("ffmpeg.exe") : Environment.ExpandEnvironmentVariables(settings.FfmpegPath.Trim());
			settings.PreRollSeconds = Math.Max(0, Math.Min(120, settings.PreRollSeconds));
			settings.PostRollSeconds = Math.Max(0, Math.Min(120, settings.PostRollSeconds));
			settings.MinimumFreeSpaceGb = Math.Max(0.25, Math.Min(1000, settings.MinimumFreeSpaceGb));
			return settings;
		}

		private static string Protect(string value)
		{
			if (string.IsNullOrEmpty(value))
				return string.Empty;
			byte[] raw = null;
			byte[] protectedBytes = null;
			try {
				raw = Encoding.UTF8.GetBytes(value);
				protectedBytes = NativeDpapi.ProtectForCurrentUser(raw);
				return Convert.ToBase64String(protectedBytes);
			} catch (Exception ex) {
				throw new InvalidOperationException("OBS password could not be protected for this Windows user.", ex);
			} finally {
				if (raw != null)
					Array.Clear(raw, 0, raw.Length);
				if (protectedBytes != null)
					Array.Clear(protectedBytes, 0, protectedBytes.Length);
			}
		}

		private static string Unprotect(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return string.Empty;
			byte[] protectedBytes = null;
			byte[] raw = null;
			try {
				protectedBytes = Convert.FromBase64String(value);
				raw = NativeDpapi.UnprotectForCurrentUser(protectedBytes);
				return Encoding.UTF8.GetString(raw);
			} catch (Exception ex) {
				OrcaTradeRecorderDiagnostics.Write("Stored OBS password could not be decrypted: " + ex.Message);
				return string.Empty;
			} finally {
				if (protectedBytes != null)
					Array.Clear(protectedBytes, 0, protectedBytes.Length);
				if (raw != null)
					Array.Clear(raw, 0, raw.Length);
			}
		}

		private static class NativeDpapi
		{
			private const int CryptProtectUiForbidden = 0x1;

			[StructLayout(LayoutKind.Sequential)]
			private struct DataBlob
			{
				public int Length;
				public IntPtr Data;
			}

			[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			private static extern bool CryptProtectData(
				ref DataBlob dataIn,
				string dataDescription,
				IntPtr optionalEntropy,
				IntPtr reserved,
				IntPtr promptStruct,
				int flags,
				out DataBlob dataOut);

			[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			private static extern bool CryptUnprotectData(
				ref DataBlob dataIn,
				out IntPtr dataDescription,
				IntPtr optionalEntropy,
				IntPtr reserved,
				IntPtr promptStruct,
				int flags,
				out DataBlob dataOut);

			[DllImport("kernel32.dll", SetLastError = true)]
			private static extern IntPtr LocalFree(IntPtr memory);

			public static byte[] ProtectForCurrentUser(byte[] value)
			{
				DataBlob input = CreateInput(value);
				DataBlob output = new DataBlob();
				try {
					if (!CryptProtectData(ref input, "Orca Trade Recorder OBS password", IntPtr.Zero,
						IntPtr.Zero, IntPtr.Zero, CryptProtectUiForbidden, out output))
						throw new Win32Exception(Marshal.GetLastWin32Error());
					return CopyOutput(output);
				} finally {
					FreeInput(ref input);
					FreeOutput(ref output);
				}
			}

			public static byte[] UnprotectForCurrentUser(byte[] value)
			{
				DataBlob input = CreateInput(value);
				DataBlob output = new DataBlob();
				IntPtr description = IntPtr.Zero;
				try {
					if (!CryptUnprotectData(ref input, out description, IntPtr.Zero, IntPtr.Zero,
						IntPtr.Zero, CryptProtectUiForbidden, out output))
						throw new Win32Exception(Marshal.GetLastWin32Error());
					return CopyOutput(output);
				} finally {
					FreeInput(ref input);
					FreeOutput(ref output);
					if (description != IntPtr.Zero)
						LocalFree(description);
				}
			}

			private static DataBlob CreateInput(byte[] value)
			{
				if (value == null || value.Length == 0)
					throw new ArgumentException("DPAPI input cannot be empty.", "value");
				DataBlob blob = new DataBlob {
					Length = value.Length,
					Data = Marshal.AllocHGlobal(value.Length)
				};
				Marshal.Copy(value, 0, blob.Data, value.Length);
				return blob;
			}

			private static byte[] CopyOutput(DataBlob blob)
			{
				if (blob.Data == IntPtr.Zero || blob.Length <= 0)
					throw new InvalidOperationException("Windows DPAPI returned no data.");
				byte[] result = new byte[blob.Length];
				Marshal.Copy(blob.Data, result, 0, blob.Length);
				return result;
			}

			private static void FreeInput(ref DataBlob blob)
			{
				if (blob.Data != IntPtr.Zero) {
					ZeroMemory(blob.Data, blob.Length);
					Marshal.FreeHGlobal(blob.Data);
				}
				blob.Data = IntPtr.Zero;
				blob.Length = 0;
			}

			private static void FreeOutput(ref DataBlob blob)
			{
				if (blob.Data != IntPtr.Zero) {
					ZeroMemory(blob.Data, blob.Length);
					LocalFree(blob.Data);
				}
				blob.Data = IntPtr.Zero;
				blob.Length = 0;
			}

			private static void ZeroMemory(IntPtr memory, int length)
			{
				for (int index = 0; index < length; index++)
					Marshal.WriteByte(memory, index, 0);
			}
		}

		public static string FindDefaultObsPath()
		{
			string[] candidates = {
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "obs-studio", "bin", "64bit", "obs64.exe"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "obs-studio", "bin", "32bit", "obs32.exe")
			};
			return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
		}

		public static string FindOnPath(string fileName)
		{
			try {
				string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
				foreach (string part in path.Split(Path.PathSeparator)) {
					string directory = part.Trim().Trim('"');
					if (directory.Length == 0)
						continue;
					string candidate = Path.Combine(directory, fileName);
					if (File.Exists(candidate))
						return candidate;
				}
			} catch { }
			return string.Empty;
		}
	}

	public sealed class OrcaTradeRecorderSnapshot
	{
		public OrcaTradeRecorderState State { get; set; }
		public string Summary { get; set; }
		public string ObsHealth { get; set; }
		public string ReplayHealth { get; set; }
		public string MicrophoneHealth { get; set; }
		public string DiskHealth { get; set; }
		public string AccountHealth { get; set; }
		public int OpenPositionCount { get; set; }
		public string ActiveInstruments { get; set; }
		public TimeSpan RecordingElapsed { get; set; }
		public int PendingClipCount { get; set; }
		public bool IsArmed { get; set; }
	}

	internal sealed class OrcaRecorderPosition
	{
		public string AccountName;
		public string InstrumentName;
		public int Quantity;
		public string MarketPosition;
	}

	public sealed class OrcaRecorderManifestEvent
	{
		public DateTime Utc { get; set; }
		public string Type { get; set; }
		public string Detail { get; set; }
	}

	public sealed class OrcaRecorderCaptureManifest
	{
		public int SchemaVersion { get; set; }
		public string CaptureId { get; set; }
		public DateTime StartedUtc { get; set; }
		public DateTime StoppedUtc { get; set; }
		public int PreRollSeconds { get; set; }
		public int PostRollSeconds { get; set; }
		public bool StartedWithOpenPosition { get; set; }
		public List<string> Accounts { get; set; }
		public List<string> Instruments { get; set; }
		public int MaximumSimultaneousPositions { get; set; }
		public List<string> RawSegments { get; set; }
		public string FinalVideoPath { get; set; }
		public string FinalizationStatus { get; set; }
		public string FinalizationError { get; set; }
		public List<OrcaRecorderManifestEvent> Events { get; set; }
		[XmlIgnore]
		public string BundleDirectory { get; set; }

		public OrcaRecorderCaptureManifest()
		{
			Accounts = new List<string>();
			Instruments = new List<string>();
			RawSegments = new List<string>();
			Events = new List<OrcaRecorderManifestEvent>();
			FinalizationStatus = "Pending";
		}
	}

	public sealed class OrcaTradeRecorderRuntime : IDisposable
	{
		private readonly object sync = new object();
		private readonly Dispatcher dispatcher;
		private readonly DispatcherTimer timer;
		private readonly SemaphoreSlim operationGate = new SemaphoreSlim(1, 1);
		private readonly HashSet<Account> hookedAccounts = new HashSet<Account>(ReferenceEqualityComparer<Account>.Instance);
		private readonly List<OrcaRecorderCaptureManifest> pendingCaptures = new List<OrcaRecorderCaptureManifest>();
		private readonly OrcaObsWebSocketClient obsClient = new OrcaObsWebSocketClient();
		private OrcaTradeRecorderSettings settings;
		private OrcaTradeRecorderState state = OrcaTradeRecorderState.Off;
		private string summary = "Recorder is off";
		private string obsHealth = "Not connected";
		private string replayHealth = "Off";
		private string microphoneHealth = "Not checked";
		private string diskHealth = "Not checked";
		private string accountHealth = "Not monitoring";
		private List<OrcaRecorderPosition> openPositions = new List<OrcaRecorderPosition>();
		private OrcaRecorderCaptureManifest currentCapture;
		private string currentPreRollPath;
		private string currentMainPath;
		private DateTime recordingStartedUtc;
		private DateTime tailDeadlineUtc;
		private DateTime nextObsHealthCheckUtc;
		private bool armed;
		private bool ownsReplayBuffer;
		private bool currentNeedsRecoveryScan;
		private bool disposed;
		private int reconcileQueued;
		private int healthCheckQueued;

		public event EventHandler SnapshotChanged;

		public OrcaTradeRecorderRuntime(Dispatcher dispatcher)
		{
			this.dispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;
			settings = OrcaTradeRecorderSettingsStore.Load();
			try { pendingCaptures.AddRange(LoadPendingManifests(settings.OutputDirectory)); } catch { }
			timer = new DispatcherTimer(DispatcherPriority.Background, this.dispatcher) { Interval = TimeSpan.FromSeconds(1) };
			timer.Tick += OnTimerTick;
			timer.Start();
			OrcaTradeRecorderDiagnostics.Write("Runtime created; manual arm required.");
		}

		public OrcaTradeRecorderSettings GetSettings()
		{
			lock (sync)
				return settings.Clone();
		}

		public void SaveSettings(OrcaTradeRecorderSettings newSettings)
		{
			if (newSettings == null)
				return;
			lock (sync) {
				if (armed || state == OrcaTradeRecorderState.Starting || state == OrcaTradeRecorderState.Finalizing)
					throw new InvalidOperationException("Disarm the recorder before changing its settings.");
				settings = OrcaTradeRecorderSettingsStore.Save(newSettings);
			}
			obsClient.Disconnect();
			SetStatus(OrcaTradeRecorderState.Off, "Settings saved");
		}

		public OrcaTradeRecorderSnapshot GetSnapshot()
		{
			lock (sync) {
				return new OrcaTradeRecorderSnapshot {
					State = state,
					Summary = summary,
					ObsHealth = obsHealth,
					ReplayHealth = replayHealth,
					MicrophoneHealth = microphoneHealth,
					DiskHealth = diskHealth,
					AccountHealth = accountHealth,
					OpenPositionCount = openPositions.Count,
					ActiveInstruments = string.Join(", ", openPositions.Select(p => p.InstrumentName).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x)),
					RecordingElapsed = recordingStartedUtc == DateTime.MinValue ? TimeSpan.Zero : DateTime.UtcNow - recordingStartedUtc,
					PendingClipCount = pendingCaptures.Count,
					IsArmed = armed
				};
			}
		}

		public void NotifyControlCenterReady()
		{
			PublishSnapshot();
		}

		public void Arm()
		{
			lock (sync) {
				if (disposed || armed || state == OrcaTradeRecorderState.Starting || state == OrcaTradeRecorderState.Finalizing)
					return;
			}
			QueueOperation(ArmCoreAsync, true);
		}

		public void Disarm()
		{
			QueueOperation(DisarmCoreAsync, true);
		}

		public void TestConnection()
		{
			lock (sync) {
				if (armed || state == OrcaTradeRecorderState.Starting || state == OrcaTradeRecorderState.Finalizing)
					return;
			}
			QueueOperation(TestConnectionCoreAsync, true);
		}

		public void TestRecording()
		{
			lock (sync) {
				if (armed || state == OrcaTradeRecorderState.Starting || state == OrcaTradeRecorderState.Finalizing)
					return;
			}
			QueueOperation(TestRecordingCoreAsync, true);
		}

		public void OpenObs()
		{
			OrcaTradeRecorderSettings local = GetSettings();
			try {
				LaunchObs(local, false);
				SetSummary("OBS launch requested");
			} catch (Exception ex) {
				SetError("OBS could not be opened", ex);
			}
		}

		private async Task ArmCoreAsync()
		{
			SetStatus(OrcaTradeRecorderState.Starting, "Starting OBS and checking recorder readiness");
			OrcaTradeRecorderSettings local = GetSettings();
			try {
				string dailyOutput = PrepareOutputDirectory(local);
				await EnsureObsReadyAsync(local, dailyOutput, false).ConfigureAwait(false);
				if (local.PreRollSeconds > 0)
					await obsClient.CallAsync("StartReplayBuffer", null, TimeSpan.FromSeconds(10)).ConfigureAwait(false);
				lock (sync) {
					armed = true;
					ownsReplayBuffer = local.PreRollSeconds > 0;
					replayHealth = local.PreRollSeconds > 0 ? "Running" : "Disabled";
					accountHealth = "Monitoring all NinjaTrader accounts";
					nextObsHealthCheckUtc = DateTime.UtcNow.AddSeconds(5);
				}
				RunOnUi(() => {
					ReconcileAccountsAndPositions();
					bool hasOpen;
					lock (sync) hasOpen = openPositions.Count > 0;
					if (!hasOpen)
						SetStatus(OrcaTradeRecorderState.Armed, "Armed — waiting for a position");
					else {
						SetStatus(OrcaTradeRecorderState.Armed, "Armed with an existing open position; pre-arm footage is unavailable");
						StartCaptureFromCurrentPositions(true);
					}
				});
				OrcaTradeRecorderDiagnostics.Write("Recorder armed.");
			} catch (Exception ex) {
				lock (sync) {
					armed = false;
					ownsReplayBuffer = false;
				}
				RunOnUi(UnhookAllAccounts);
				SetError("Recorder could not be armed", ex);
			}
		}

		private async Task DisarmCoreAsync()
		{
			bool shouldStop;
			lock (sync) {
				shouldStop = currentCapture != null;
				armed = false;
			}
			if (shouldStop)
				await StopCaptureCoreAsync("ManualDisarm").ConfigureAwait(false);
			bool hadCaptureWarning;
			lock (sync) hadCaptureWarning = state == OrcaTradeRecorderState.Error;
			try {
				if (ownsReplayBuffer && obsClient.IsConnected)
					await obsClient.TryCallAsync("StopReplayBuffer", null, TimeSpan.FromSeconds(8)).ConfigureAwait(false);
			} finally {
				lock (sync) {
					ownsReplayBuffer = false;
					replayHealth = "Off";
					accountHealth = "Not monitoring";
				}
				RunOnUi(UnhookAllAccounts);
			}

			SetStatus(OrcaTradeRecorderState.Finalizing, "Finalizing pending trade clips");
			await FinalizePendingCoreAsync().ConfigureAwait(false);
			obsClient.Disconnect();
			lock (sync) {
				obsHealth = "Not connected";
				microphoneHealth = "Not checked";
				diskHealth = "Not checked";
				openPositions = new List<OrcaRecorderPosition>();
			}
			if (hadCaptureWarning)
				SetStatus(OrcaTradeRecorderState.Error, "Recorder disarmed with a capture warning; check the raw bundle and diagnostic log");
			else
				SetStatus(OrcaTradeRecorderState.Off, "Recorder is off");
			OrcaTradeRecorderDiagnostics.Write("Recorder disarmed.");
		}

		private async Task TestConnectionCoreAsync()
		{
			SetStatus(OrcaTradeRecorderState.Starting, "Testing OBS connection and recorder configuration");
			try {
				OrcaTradeRecorderSettings local = GetSettings();
				await EnsureObsReadyAsync(local, PrepareOutputDirectory(local), false).ConfigureAwait(false);
				SetStatus(OrcaTradeRecorderState.Off, "OBS connection and recorder configuration passed");
			} catch (Exception ex) {
				SetError("OBS connection test failed", ex);
			}
		}

		private async Task TestRecordingCoreAsync()
		{
			SetStatus(OrcaTradeRecorderState.Starting, "Preparing a 10-second OBS test recording");
			Exception failure = null;
			try {
				OrcaTradeRecorderSettings local = GetSettings();
				await EnsureObsReadyAsync(local, PrepareOutputDirectory(local), false).ConfigureAwait(false);
				await obsClient.CallAsync("StartRecord", null, TimeSpan.FromSeconds(10)).ConfigureAwait(false);
				SetStatus(OrcaTradeRecorderState.Recording, "Test recording in progress — speak into the configured microphone");
				await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
				Dictionary<string, object> response = await obsClient.CallAsync("StopRecord", null, TimeSpan.FromSeconds(15)).ConfigureAwait(false);
				string path = OrcaObsWebSocketClient.GetString(response, "outputPath");
				SetStatus(OrcaTradeRecorderState.Off, "Test recording saved: " + (string.IsNullOrEmpty(path) ? "check the OBS output folder" : path));
			} catch (Exception ex) {
				failure = ex;
			}
			if (failure != null) {
				await obsClient.TryCallAsync("StopRecord", null, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
				SetError("Test recording failed", failure);
			}
		}

		private async Task EnsureObsReadyAsync(OrcaTradeRecorderSettings local, string dailyOutput, bool allowActiveRecording)
		{
			ValidateLocalConfiguration(local, dailyOutput);
			SetHealth("Connecting", "Checking", "Checking", GetDiskHealth(local, dailyOutput));
			Exception initialConnectionFailure = null;
			try {
				await obsClient.ConnectAsync(local, TimeSpan.FromSeconds(3)).ConfigureAwait(false);
			} catch (Exception ex) {
				initialConnectionFailure = ex;
			}
			if (initialConnectionFailure != null) {
				if (!local.AutoLaunchObs)
					throw new InvalidOperationException("OBS is not reachable and automatic launch is disabled.", initialConnectionFailure);
				LaunchObs(local, true);
				Exception last = null;
				for (int attempt = 0; attempt < 30; attempt++) {
					await Task.Delay(750).ConfigureAwait(false);
					try {
						await obsClient.ConnectAsync(local, TimeSpan.FromSeconds(2)).ConfigureAwait(false);
						last = null;
						break;
					} catch (Exception ex) { last = ex; }
				}
				if (last != null)
					throw new InvalidOperationException("OBS launched but its WebSocket server did not become ready.", last);
			}

			Dictionary<string, object> profileData = await obsClient.CallAsync("GetProfileList", null, TimeSpan.FromSeconds(8)).ConfigureAwait(false);
			string currentProfile = OrcaObsWebSocketClient.GetString(profileData, "currentProfileName");
			if (!string.Equals(currentProfile, local.ObsProfile, StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("OBS is using profile '" + currentProfile + "'. Select the dedicated '" + local.ObsProfile + "' profile.");

			Dictionary<string, object> collectionData = await obsClient.CallAsync("GetSceneCollectionList", null, TimeSpan.FromSeconds(8)).ConfigureAwait(false);
			string currentCollection = OrcaObsWebSocketClient.GetString(collectionData, "currentSceneCollectionName");
			if (!string.Equals(currentCollection, local.ObsSceneCollection, StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("OBS is using scene collection '" + currentCollection + "'. Select '" + local.ObsSceneCollection + "'.");

			Dictionary<string, object> sceneData = await obsClient.CallAsync("GetSceneList", null, TimeSpan.FromSeconds(8)).ConfigureAwait(false);
			if (!OrcaObsWebSocketClient.ArrayContainsNamedValue(sceneData, "scenes", "sceneName", local.ObsScene))
				throw new InvalidOperationException("OBS scene '" + local.ObsScene + "' was not found.");
			await obsClient.CallAsync("SetCurrentProgramScene", new Dictionary<string, object> { { "sceneName", local.ObsScene } }, TimeSpan.FromSeconds(8)).ConfigureAwait(false);

			Dictionary<string, object> inputData = await obsClient.CallAsync("GetInputList", null, TimeSpan.FromSeconds(8)).ConfigureAwait(false);
			if (!OrcaObsWebSocketClient.ArrayContainsNamedValue(inputData, "inputs", "inputName", local.MicrophoneInput))
				throw new InvalidOperationException("OBS microphone input '" + local.MicrophoneInput + "' was not found.");
			Dictionary<string, object> muteData = await obsClient.CallAsync("GetInputMute", new Dictionary<string, object> { { "inputName", local.MicrophoneInput } }, TimeSpan.FromSeconds(8)).ConfigureAwait(false);
			if (OrcaObsWebSocketClient.GetBoolean(muteData, "inputMuted"))
				throw new InvalidOperationException("OBS microphone input '" + local.MicrophoneInput + "' is muted.");
			Dictionary<string, object> specialInputs = await obsClient.CallAsync("GetSpecialInputs", null, TimeSpan.FromSeconds(8)).ConfigureAwait(false);
			if (!string.IsNullOrWhiteSpace(OrcaObsWebSocketClient.GetString(specialInputs, "desktop1"))
				|| !string.IsNullOrWhiteSpace(OrcaObsWebSocketClient.GetString(specialInputs, "desktop2")))
				throw new InvalidOperationException("Desktop Audio is enabled in the dedicated OBS profile. Disable Desktop Audio so trade clips contain only the selected microphone.");

			Dictionary<string, object> streamStatus = await obsClient.CallAsync("GetStreamStatus", null, TimeSpan.FromSeconds(8)).ConfigureAwait(false);
			if (OrcaObsWebSocketClient.GetBoolean(streamStatus, "outputActive"))
				throw new InvalidOperationException("OBS is already streaming. Orca will not take control of an active OBS workflow.");
			Dictionary<string, object> recordStatus = await obsClient.CallAsync("GetRecordStatus", null, TimeSpan.FromSeconds(8)).ConfigureAwait(false);
			if (!allowActiveRecording && OrcaObsWebSocketClient.GetBoolean(recordStatus, "outputActive"))
				throw new InvalidOperationException("OBS is already recording. Stop the existing recording before arming Orca.");
			Dictionary<string, object> replayStatus = await obsClient.CallAsync("GetReplayBufferStatus", null, TimeSpan.FromSeconds(8)).ConfigureAwait(false);
			if (!allowActiveRecording && OrcaObsWebSocketClient.GetBoolean(replayStatus, "outputActive"))
				throw new InvalidOperationException("OBS Replay Buffer is already running outside Orca's ownership.");

			if (!allowActiveRecording) {
				string replayEnabled = local.PreRollSeconds > 0 ? "true" : "false";
				string replaySeconds = Math.Max(1, local.PreRollSeconds).ToString(CultureInfo.InvariantCulture);
				await SetObsProfileParameterAsync("SimpleOutput", "RecRB", replayEnabled).ConfigureAwait(false);
				await SetObsProfileParameterAsync("SimpleOutput", "RecRBTime", replaySeconds).ConfigureAwait(false);
				await SetObsProfileParameterAsync("SimpleOutput", "RecFormat2", "mkv").ConfigureAwait(false);
				await SetObsProfileParameterAsync("AdvOut", "RecRB", replayEnabled).ConfigureAwait(false);
				await SetObsProfileParameterAsync("AdvOut", "RecRBTime", replaySeconds).ConfigureAwait(false);
				await SetObsProfileParameterAsync("AdvOut", "RecFormat2", "mkv").ConfigureAwait(false);
			}
			await obsClient.CallAsync("SetRecordDirectory", new Dictionary<string, object> { { "recordDirectory", dailyOutput } }, TimeSpan.FromSeconds(8)).ConfigureAwait(false);
			SetHealth("Connected", ownsReplayBuffer ? "Running" : "Ready", "Ready: " + local.MicrophoneInput, GetDiskHealth(local, dailyOutput));
		}

		private Task<Dictionary<string, object>> SetObsProfileParameterAsync(string category, string name, string value)
		{
			return obsClient.CallAsync("SetProfileParameter", new Dictionary<string, object> {
				{ "parameterCategory", category },
				{ "parameterName", name },
				{ "parameterValue", value }
			}, TimeSpan.FromSeconds(8));
		}

		private void ValidateLocalConfiguration(OrcaTradeRecorderSettings local, string dailyOutput)
		{
			if (string.IsNullOrWhiteSpace(local.ObsExecutablePath) || !File.Exists(local.ObsExecutablePath))
				throw new FileNotFoundException("OBS executable was not found. Install OBS Studio or select obs64.exe in Recorder settings.", local.ObsExecutablePath);
			if (string.IsNullOrWhiteSpace(local.ObsPassword))
				throw new InvalidOperationException("Enter the password from OBS Tools > WebSocket Server Settings.");
			if (string.IsNullOrWhiteSpace(local.ObsProfile) || string.IsNullOrWhiteSpace(local.ObsSceneCollection) || string.IsNullOrWhiteSpace(local.ObsScene))
				throw new InvalidOperationException("OBS profile, scene collection, and scene are required.");
			if (string.IsNullOrWhiteSpace(local.MicrophoneInput))
				throw new InvalidOperationException("Select the microphone input in the dedicated OBS profile.");
			Directory.CreateDirectory(dailyOutput);
			string probe = Path.Combine(dailyOutput, ".orca-write-test");
			File.WriteAllText(probe, "ok", Encoding.UTF8);
			File.Delete(probe);
			long free = GetAvailableFreeSpace(dailyOutput);
			long required = (long)(local.MinimumFreeSpaceGb * 1024d * 1024d * 1024d);
			if (free < required)
				throw new IOException("Recording drive has only " + FormatGb(free) + " GB free; " + local.MinimumFreeSpaceGb.ToString("0.##", CultureInfo.InvariantCulture) + " GB is required.");
		}

		private static string PrepareOutputDirectory(OrcaTradeRecorderSettings local)
		{
			string daily = Path.Combine(local.OutputDirectory, DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
			Directory.CreateDirectory(daily);
			return daily;
		}

		private void LaunchObs(OrcaTradeRecorderSettings local, bool minimized)
		{
			if (string.IsNullOrWhiteSpace(local.ObsExecutablePath) || !File.Exists(local.ObsExecutablePath))
				throw new FileNotFoundException("OBS executable was not found.", local.ObsExecutablePath);
			string arguments = (minimized ? "--minimize-to-tray " : string.Empty)
				+ "--profile " + QuoteArgument(local.ObsProfile)
				+ " --collection " + QuoteArgument(local.ObsSceneCollection)
				+ " --scene " + QuoteArgument(local.ObsScene);
			Process.Start(new ProcessStartInfo {
				FileName = local.ObsExecutablePath,
				Arguments = arguments,
				WorkingDirectory = Path.GetDirectoryName(local.ObsExecutablePath),
				UseShellExecute = true,
				WindowStyle = minimized ? ProcessWindowStyle.Minimized : ProcessWindowStyle.Normal
			});
			OrcaTradeRecorderDiagnostics.Write("OBS launch requested for the configured profile and scene.");
		}

		private void StartCaptureFromCurrentPositions(bool startedWithOpenPosition)
		{
			List<OrcaRecorderPosition> snapshot;
			lock (sync) {
				if (!armed || currentCapture != null || openPositions.Count == 0)
					return;
				snapshot = ClonePositions(openPositions);
			}
			QueueOperation(() => StartCaptureCoreAsync(snapshot, startedWithOpenPosition), false);
		}

		private async Task StartCaptureCoreAsync(List<OrcaRecorderPosition> positions, bool startedWithOpenPosition)
		{
			lock (sync) {
				if (!armed || currentCapture != null)
					return;
				currentCapture = CreateManifest(positions, startedWithOpenPosition);
				currentPreRollPath = string.Empty;
				currentMainPath = string.Empty;
				currentNeedsRecoveryScan = false;
				recordingStartedUtc = DateTime.UtcNow;
			}
			SetStatus(OrcaTradeRecorderState.Recording, startedWithOpenPosition
				? "Recording existing open position — pre-arm footage is unavailable"
				: "Trade detected — recording started");
			Exception startFailure = null;
			try {
				Dictionary<string, object> recordStatus = await obsClient.CallAsync("GetRecordStatus", null, TimeSpan.FromSeconds(8)).ConfigureAwait(false);
				if (OrcaObsWebSocketClient.GetBoolean(recordStatus, "outputActive"))
					throw new InvalidOperationException("OBS reported an unexpected active recording before the trade capture started.");

				await obsClient.CallAsync("StartRecord", null, TimeSpan.FromSeconds(10)).ConfigureAwait(false);
				AddManifestEvent("RecordStarted", "OBS confirmed the main recording start.");
				if (!startedWithOpenPosition && settings.PreRollSeconds > 0) {
					Dictionary<string, object> priorReplay = await obsClient.TryCallAsync("GetLastReplayBufferReplay", null, TimeSpan.FromSeconds(4)).ConfigureAwait(false);
					string priorReplayPath = OrcaObsWebSocketClient.GetString(priorReplay, "savedReplayPath");
					await obsClient.CallAsync("SaveReplayBuffer", null, TimeSpan.FromSeconds(10)).ConfigureAwait(false);
					currentPreRollPath = await WaitForReplayPathAsync(priorReplayPath).ConfigureAwait(false);
					AddManifestEvent("ReplaySaved", string.IsNullOrEmpty(currentPreRollPath) ? "Replay path was not reported yet." : currentPreRollPath);
				}
				WriteCurrentManifest();
				OrcaTradeRecorderDiagnostics.Write("Trade capture started.");
			} catch (Exception ex) {
				startFailure = ex;
			}
			if (startFailure != null) {
				Dictionary<string, object> stop = await obsClient.TryCallAsync("StopRecord", null, TimeSpan.FromSeconds(6)).ConfigureAwait(false);
				currentMainPath = OrcaObsWebSocketClient.GetString(stop, "outputPath");
				AddManifestEvent("StartFailed", startFailure.Message);
				WriteCurrentManifest();
				SetError("Trade recording could not start", startFailure);
			}
		}

		private async Task<string> WaitForReplayPathAsync(string previous)
		{
			previous = previous ?? string.Empty;
			for (int attempt = 0; attempt < 20; attempt++) {
				await Task.Delay(250).ConfigureAwait(false);
				try {
					Dictionary<string, object> data = await obsClient.CallAsync("GetLastReplayBufferReplay", null, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
					string path = OrcaObsWebSocketClient.GetString(data, "savedReplayPath");
					if (!string.IsNullOrWhiteSpace(path) && !string.Equals(path, previous, StringComparison.OrdinalIgnoreCase))
						return path;
				} catch { }
			}
			return string.Empty;
		}

		private async Task StopCaptureCoreAsync(string reason)
		{
			OrcaRecorderCaptureManifest capture;
			OrcaTradeRecorderState stateBeforeStop;
			lock (sync) {
				capture = currentCapture;
				stateBeforeStop = state;
			}
			if (capture == null)
				return;

			SetStatus(OrcaTradeRecorderState.Finalizing, "Stopping OBS and preserving raw trade footage");
			Exception stopFailure = null;
			try {
				if (!obsClient.IsConnected)
					await ReconnectOrLaunchObsAsync().ConfigureAwait(false);
				Dictionary<string, object> status = await obsClient.CallAsync("GetRecordStatus", null, TimeSpan.FromSeconds(6)).ConfigureAwait(false);
				if (OrcaObsWebSocketClient.GetBoolean(status, "outputActive")) {
					Dictionary<string, object> stop = await obsClient.CallAsync("StopRecord", null, TimeSpan.FromSeconds(15)).ConfigureAwait(false);
					currentMainPath = OrcaObsWebSocketClient.GetString(stop, "outputPath");
				} else if (stateBeforeStop == OrcaTradeRecorderState.Recording || stateBeforeStop == OrcaTradeRecorderState.Tail) {
					lock (sync) currentNeedsRecoveryScan = true;
				}
			} catch (Exception ex) {
				stopFailure = ex;
				lock (sync) currentNeedsRecoveryScan = true;
			}
			capture.StoppedUtc = DateTime.UtcNow;
			AddManifestEvent(stopFailure == null ? "RecordStopped" : "StopFailed",
				stopFailure == null ? reason + (string.IsNullOrEmpty(currentMainPath) ? string.Empty : ": " + currentMainPath) : stopFailure.Message);
			try {
				PreserveCaptureBundle(capture);
				lock (sync) {
					pendingCaptures.Add(capture);
					currentCapture = null;
					currentPreRollPath = string.Empty;
					currentMainPath = string.Empty;
					currentNeedsRecoveryScan = false;
					recordingStartedUtc = DateTime.MinValue;
				}
			} catch (Exception ex) {
				SetError("Raw trade footage could not be bundled; OBS output files were not deleted", ex);
				return;
			}
			if (stopFailure != null) {
				SetError("OBS could not cleanly stop the trade recording; raw MKV recovery may still be available", stopFailure);
				return;
			}
			if (armed) {
				try {
					await EnsureReplayBufferRunningAsync().ConfigureAwait(false);
					SetStatus(OrcaTradeRecorderState.Armed, "Trade saved — armed for the next position");
				} catch (Exception ex) {
					SetError("Trade footage was saved, but the replay buffer could not be rearmed", ex);
					return;
				}
			}
			OrcaTradeRecorderDiagnostics.Write("Trade capture stopped and raw footage preserved.");
		}

		private async Task EnsureReplayBufferRunningAsync()
		{
			if (settings.PreRollSeconds <= 0) {
				lock (sync) {
					ownsReplayBuffer = false;
					replayHealth = "Disabled";
				}
				return;
			}
			Dictionary<string, object> status = await obsClient.TryCallAsync("GetReplayBufferStatus", null, TimeSpan.FromSeconds(6)).ConfigureAwait(false);
			if (!OrcaObsWebSocketClient.GetBoolean(status, "outputActive"))
				await obsClient.CallAsync("StartReplayBuffer", null, TimeSpan.FromSeconds(8)).ConfigureAwait(false);
			lock (sync) {
				ownsReplayBuffer = true;
				replayHealth = "Running";
			}
		}

		private OrcaRecorderCaptureManifest CreateManifest(List<OrcaRecorderPosition> positions, bool startedWithOpenPosition)
		{
			string id = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
			string daily = PrepareOutputDirectory(settings);
			string bundle = Path.Combine(daily, "capture-" + id);
			Directory.CreateDirectory(bundle);
			OrcaRecorderCaptureManifest manifest = new OrcaRecorderCaptureManifest {
				SchemaVersion = 1,
				CaptureId = id,
				StartedUtc = DateTime.UtcNow,
				PreRollSeconds = settings.PreRollSeconds,
				PostRollSeconds = settings.PostRollSeconds,
				StartedWithOpenPosition = startedWithOpenPosition,
				MaximumSimultaneousPositions = positions.Count,
				BundleDirectory = bundle
			};
			manifest.Accounts.AddRange(positions.Select(p => p.AccountName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase));
			manifest.Instruments.AddRange(positions.Select(p => p.InstrumentName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase));
			manifest.Events.Add(new OrcaRecorderManifestEvent { Utc = DateTime.UtcNow, Type = "CaptureCreated", Detail = startedWithOpenPosition ? "Started after arming with an existing position." : "Started on global flat-to-nonflat transition." });
			return manifest;
		}

		private void UpdateManifestPositions(List<OrcaRecorderPosition> positions)
		{
			lock (sync) {
				if (currentCapture == null)
					return;
				foreach (string account in positions.Select(p => p.AccountName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
					if (!currentCapture.Accounts.Contains(account, StringComparer.OrdinalIgnoreCase)) currentCapture.Accounts.Add(account);
				foreach (string instrument in positions.Select(p => p.InstrumentName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
					if (!currentCapture.Instruments.Contains(instrument, StringComparer.OrdinalIgnoreCase)) currentCapture.Instruments.Add(instrument);
				currentCapture.MaximumSimultaneousPositions = Math.Max(currentCapture.MaximumSimultaneousPositions, positions.Count);
			}
		}

		private void AddManifestEvent(string type, string detail)
		{
			lock (sync) {
				if (currentCapture != null)
					currentCapture.Events.Add(new OrcaRecorderManifestEvent { Utc = DateTime.UtcNow, Type = type, Detail = detail ?? string.Empty });
			}
		}

		private void WriteCurrentManifest()
		{
			OrcaRecorderCaptureManifest capture;
			lock (sync) capture = currentCapture;
			if (capture != null)
				WriteManifest(capture);
		}

		private void PreserveCaptureBundle(OrcaRecorderCaptureManifest capture)
		{
			if (capture == null)
				return;
			Directory.CreateDirectory(capture.BundleDirectory);
			List<string> candidates = new List<string>();
			if (!string.IsNullOrWhiteSpace(currentPreRollPath)) candidates.Add(currentPreRollPath);
			if (!string.IsNullOrWhiteSpace(currentMainPath)) candidates.Add(currentMainPath);
			string daily = Path.GetDirectoryName(capture.BundleDirectory);
			if (currentNeedsRecoveryScan) {
				try {
					DateTime fromUtc = capture.StartedUtc.AddMinutes(-2);
					DateTime toUtc = (capture.StoppedUtc == DateTime.MinValue ? DateTime.UtcNow : capture.StoppedUtc).AddMinutes(1);
					foreach (string file in Directory.GetFiles(daily).OrderBy(File.GetCreationTimeUtc).ThenBy(File.GetLastWriteTimeUtc)) {
						string extension = Path.GetExtension(file);
						if (!string.Equals(extension, ".mkv", StringComparison.OrdinalIgnoreCase) && !string.Equals(extension, ".mp4", StringComparison.OrdinalIgnoreCase))
							continue;
						DateTime writeUtc = File.GetLastWriteTimeUtc(file);
						if (writeUtc >= fromUtc && writeUtc <= toUtc)
							candidates.Add(file);
					}
				} catch { }
			}

			List<string> distinct = candidates.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
			capture.RawSegments.Clear();
			for (int index = 0; index < distinct.Count; index++) {
				string source = distinct[index];
				string label = string.Equals(source, currentPreRollPath, StringComparison.OrdinalIgnoreCase) ? "preroll" : "recording";
				string destination = Path.Combine(capture.BundleDirectory, (index + 1).ToString("00", CultureInfo.InvariantCulture) + "-" + label + Path.GetExtension(source));
				try {
					if (!string.Equals(source, destination, StringComparison.OrdinalIgnoreCase)) {
						if (File.Exists(destination))
							destination = Path.Combine(capture.BundleDirectory, (index + 1).ToString("00", CultureInfo.InvariantCulture) + "-" + label + "-" + Guid.NewGuid().ToString("N").Substring(0, 4) + Path.GetExtension(source));
						File.Move(source, destination);
					}
					capture.RawSegments.Add(destination);
				} catch (Exception ex) {
					capture.RawSegments.Add(source);
					capture.Events.Add(new OrcaRecorderManifestEvent { Utc = DateTime.UtcNow, Type = "RawMoveFailed", Detail = ex.Message });
				}
			}
			capture.FinalizationStatus = capture.RawSegments.Count == 0 ? "RawSegmentsNotLocated" : "Pending";
			WriteManifest(capture);
		}

		private static void WriteManifest(OrcaRecorderCaptureManifest capture)
		{
			if (capture == null || string.IsNullOrWhiteSpace(capture.BundleDirectory))
				return;
			Directory.CreateDirectory(capture.BundleDirectory);
			string path = Path.Combine(capture.BundleDirectory, "capture.json");
			string temporary = path + ".tmp";
			string json = new JavaScriptSerializer { MaxJsonLength = 1024 * 1024 }.Serialize(capture);
			File.WriteAllText(temporary, json, new UTF8Encoding(false));
			if (File.Exists(path)) File.Copy(temporary, path, true); else File.Move(temporary, path);
			if (File.Exists(temporary)) File.Delete(temporary);
		}

		private async Task FinalizePendingCoreAsync()
		{
			OrcaTradeRecorderSettings local = GetSettings();
			List<OrcaRecorderCaptureManifest> captures;
			lock (sync) captures = pendingCaptures.ToList();
			foreach (OrcaRecorderCaptureManifest capture in LoadPendingManifests(local.OutputDirectory))
				if (!captures.Any(x => string.Equals(x.CaptureId, capture.CaptureId, StringComparison.OrdinalIgnoreCase))) captures.Add(capture);

			foreach (OrcaRecorderCaptureManifest capture in captures) {
				if (capture.RawSegments == null || capture.RawSegments.Count == 0)
					continue;
				try {
					await OrcaTradeRecorderFinalizer.FinalizeAsync(capture, local).ConfigureAwait(false);
					OrcaTradeRecorderDiagnostics.Write("Finalized trade clip: " + capture.FinalVideoPath);
				} catch (Exception ex) {
					capture.FinalizationStatus = "Failed";
					capture.FinalizationError = ex.Message;
					capture.Events.Add(new OrcaRecorderManifestEvent { Utc = DateTime.UtcNow, Type = "FinalizationFailed", Detail = ex.Message });
					WriteManifest(capture);
					OrcaTradeRecorderDiagnostics.Write("Finalization failed for " + capture.CaptureId + ": " + ex.Message);
				}
			}
			lock (sync) pendingCaptures.RemoveAll(x => string.Equals(x.FinalizationStatus, "Complete", StringComparison.OrdinalIgnoreCase));
		}

		private static IEnumerable<OrcaRecorderCaptureManifest> LoadPendingManifests(string outputDirectory)
		{
			if (string.IsNullOrWhiteSpace(outputDirectory) || !Directory.Exists(outputDirectory))
				yield break;
			string[] files;
			try { files = Directory.GetFiles(outputDirectory, "capture.json", SearchOption.AllDirectories); }
			catch { yield break; }
			JavaScriptSerializer serializer = new JavaScriptSerializer { MaxJsonLength = 1024 * 1024 };
			foreach (string file in files) {
				OrcaRecorderCaptureManifest capture = null;
				try {
					capture = serializer.Deserialize<OrcaRecorderCaptureManifest>(File.ReadAllText(file));
					capture.BundleDirectory = Path.GetDirectoryName(file);
				} catch { }
				if (capture != null && !string.Equals(capture.FinalizationStatus, "Complete", StringComparison.OrdinalIgnoreCase))
					yield return capture;
			}
		}

		private void OnTimerTick(object sender, EventArgs e)
		{
			if (disposed)
				return;
			bool localArmed;
			OrcaTradeRecorderState localState;
			DateTime localTail;
			DateTime healthAt;
			lock (sync) {
				localArmed = armed;
				localState = state;
				localTail = tailDeadlineUtc;
				healthAt = nextObsHealthCheckUtc;
			}
			if (localArmed)
				ReconcileAccountsAndPositions();
			if (localState == OrcaTradeRecorderState.Tail && DateTime.UtcNow >= localTail) {
				bool flat;
				lock (sync) flat = openPositions.Count == 0;
				if (flat)
					QueueOperation(() => StopCaptureCoreAsync("AllAccountsFlatAfterTail"), false);
			}
			if (localArmed && DateTime.UtcNow >= healthAt && Interlocked.Exchange(ref healthCheckQueued, 1) == 0) {
				lock (sync) nextObsHealthCheckUtc = DateTime.UtcNow.AddSeconds(5);
				QueueOperation(ObsHealthCheckCoreAsync, false, () => Interlocked.Exchange(ref healthCheckQueued, 0));
			}
			PublishSnapshot();
		}

		private async Task ObsHealthCheckCoreAsync()
		{
			if (!armed)
				return;
			try {
				if (!obsClient.IsConnected)
					await ReconnectOrLaunchObsAsync().ConfigureAwait(false);
				Dictionary<string, object> replay = await obsClient.CallAsync("GetReplayBufferStatus", null, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
				bool replayActive = OrcaObsWebSocketClient.GetBoolean(replay, "outputActive");
				if (!replayActive && settings.PreRollSeconds > 0) {
					await obsClient.CallAsync("StartReplayBuffer", null, TimeSpan.FromSeconds(8)).ConfigureAwait(false);
					replayActive = true;
				}
				Dictionary<string, object> record = await obsClient.CallAsync("GetRecordStatus", null, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
				bool recordActive = OrcaObsWebSocketClient.GetBoolean(record, "outputActive");
				OrcaTradeRecorderState localState;
				lock (sync) {
					obsHealth = "Connected";
					replayHealth = settings.PreRollSeconds <= 0 ? "Disabled" : (replayActive ? "Running" : "Stopped");
					localState = state;
				}
				if ((localState == OrcaTradeRecorderState.Recording || localState == OrcaTradeRecorderState.Tail) && !recordActive) {
					await obsClient.CallAsync("StartRecord", null, TimeSpan.FromSeconds(8)).ConfigureAwait(false);
					lock (sync) currentNeedsRecoveryScan = true;
					AddManifestEvent("RecordingRecovered", "OBS recording was not active during health reconciliation; a continuation segment was started.");
					SetSummary("OBS recording recovered; the clip will include a continuation segment");
				}
			} catch (Exception ex) {
				lock (sync) obsHealth = "Disconnected — retrying";
				SetSummary("OBS connection lost; Orca will retry without blocking account events");
				obsClient.Disconnect();
				OrcaTradeRecorderDiagnostics.Write("OBS health check failed: " + ex.Message);
			}
		}

		private async Task ReconnectOrLaunchObsAsync()
		{
			OrcaTradeRecorderSettings local = GetSettings();
			try {
				await obsClient.ConnectAsync(local, TimeSpan.FromSeconds(3)).ConfigureAwait(false);
				return;
			} catch {
				if (!local.AutoLaunchObs)
					throw;
			}
			LaunchObs(local, true);
			Exception last = null;
			for (int attempt = 0; attempt < 30; attempt++) {
				await Task.Delay(750).ConfigureAwait(false);
				try {
					await obsClient.ConnectAsync(local, TimeSpan.FromSeconds(2)).ConfigureAwait(false);
					return;
				} catch (Exception ex) { last = ex; }
			}
			throw new InvalidOperationException("OBS could not be restarted during recording recovery.", last);
		}

		private void ReconcileAccountsAndPositions()
		{
			if (!dispatcher.CheckAccess()) {
				RequestReconcile();
				return;
			}
			bool localArmed;
			lock (sync) localArmed = armed;
			if (!localArmed)
				return;

			List<Account> accounts = new List<Account>();
			try { accounts.AddRange(Account.All.Where(a => a != null)); } catch { }
			foreach (Account account in accounts) {
				if (hookedAccounts.Add(account)) {
					account.PositionUpdate += OnPositionUpdate;
					account.ExecutionUpdate += OnExecutionUpdate;
				}
			}
			foreach (Account removed in hookedAccounts.Where(a => !accounts.Contains(a)).ToList()) {
				try {
					removed.PositionUpdate -= OnPositionUpdate;
					removed.ExecutionUpdate -= OnExecutionUpdate;
				} catch { }
				hookedAccounts.Remove(removed);
			}

			List<OrcaRecorderPosition> positions = new List<OrcaRecorderPosition>();
			foreach (Account account in accounts) {
				try {
					foreach (Position position in account.Positions) {
						if (position == null || position.Instrument == null || position.MarketPosition == MarketPosition.Flat || position.Quantity == 0)
							continue;
						positions.Add(new OrcaRecorderPosition {
							AccountName = account.Name ?? string.Empty,
							InstrumentName = position.Instrument.FullName ?? position.Instrument.MasterInstrument.Name,
							Quantity = Math.Abs(position.Quantity),
							MarketPosition = position.MarketPosition.ToString()
						});
					}
				} catch (Exception ex) {
					OrcaTradeRecorderDiagnostics.Write("Position snapshot failed for one account: " + ex.Message);
				}
			}

			int previousCount;
			OrcaTradeRecorderState localState;
			lock (sync) {
				previousCount = openPositions.Count;
				openPositions = positions;
				accountHealth = "Monitoring " + accounts.Count.ToString(CultureInfo.InvariantCulture) + " accounts";
				localState = state;
			}
			UpdateManifestPositions(positions);

			if (previousCount == 0 && positions.Count > 0 && localState == OrcaTradeRecorderState.Armed) {
				StartCaptureFromCurrentPositions(false);
			} else if (positions.Count == 0 && previousCount > 0 && localState == OrcaTradeRecorderState.Recording) {
				lock (sync) tailDeadlineUtc = DateTime.UtcNow.AddSeconds(settings.PostRollSeconds);
				AddManifestEvent("AllAccountsFlat", "Post-roll countdown started.");
				SetStatus(OrcaTradeRecorderState.Tail, "All accounts flat — recording post-roll");
			} else if (positions.Count > 0 && localState == OrcaTradeRecorderState.Tail) {
				AddManifestEvent("TailCancelled", "A new position opened during post-roll.");
				SetStatus(OrcaTradeRecorderState.Recording, "New position detected during post-roll — continuing the same clip");
			}
			PublishSnapshot();
		}

		private void OnPositionUpdate(object sender, PositionEventArgs e)
		{
			RequestReconcile();
		}

		private void OnExecutionUpdate(object sender, ExecutionEventArgs e)
		{
			RequestReconcile();
		}

		private void RequestReconcile()
		{
			if (disposed || Interlocked.Exchange(ref reconcileQueued, 1) == 1)
				return;
			dispatcher.InvokeAsync(() => {
				Interlocked.Exchange(ref reconcileQueued, 0);
				ReconcileAccountsAndPositions();
			}, DispatcherPriority.Background);
		}

		private void UnhookAllAccounts()
		{
			if (!dispatcher.CheckAccess()) {
				dispatcher.InvokeAsync(UnhookAllAccounts);
				return;
			}
			foreach (Account account in hookedAccounts.ToList()) {
				try {
					account.PositionUpdate -= OnPositionUpdate;
					account.ExecutionUpdate -= OnExecutionUpdate;
				} catch { }
			}
			hookedAccounts.Clear();
		}

		private void QueueOperation(Func<Task> operation, bool reportError)
		{
			QueueOperation(operation, reportError, null);
		}

		private void QueueOperation(Func<Task> operation, bool reportError, Action completed)
		{
			Task.Run(async () => {
				await operationGate.WaitAsync().ConfigureAwait(false);
				try {
					if (!disposed)
						await operation().ConfigureAwait(false);
				} catch (Exception ex) {
					if (reportError) SetError("Recorder operation failed", ex);
					else OrcaTradeRecorderDiagnostics.Write("Recorder operation failed: " + ex);
				} finally {
					operationGate.Release();
					if (completed != null) completed();
				}
			});
		}

		private void SetStatus(OrcaTradeRecorderState newState, string newSummary)
		{
			lock (sync) {
				state = newState;
				summary = newSummary ?? string.Empty;
			}
			OrcaTradeRecorderDiagnostics.Write("State=" + newState + " " + newSummary);
			PublishSnapshot();
		}

		private void SetSummary(string newSummary)
		{
			lock (sync) summary = newSummary ?? string.Empty;
			PublishSnapshot();
		}

		private void SetHealth(string obs, string replay, string microphone, string disk)
		{
			lock (sync) {
				obsHealth = obs;
				replayHealth = replay;
				microphoneHealth = microphone;
				diskHealth = disk;
			}
			PublishSnapshot();
		}

		private void SetError(string context, Exception ex)
		{
			string message = context + ": " + (ex == null ? "Unknown error" : ex.Message);
			lock (sync) {
				state = OrcaTradeRecorderState.Error;
				summary = message;
			}
			OrcaTradeRecorderDiagnostics.Write(message + (ex == null ? string.Empty : Environment.NewLine + ex));
			PublishSnapshot();
		}

		private void PublishSnapshot()
		{
			EventHandler handler = SnapshotChanged;
			if (handler == null)
				return;
			RunOnUi(() => {
				try { handler(this, EventArgs.Empty); } catch { }
			});
		}

		private void RunOnUi(Action action)
		{
			if (action == null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
				return;
			if (dispatcher.CheckAccess()) action(); else dispatcher.InvokeAsync(action);
		}

		private static List<OrcaRecorderPosition> ClonePositions(IEnumerable<OrcaRecorderPosition> source)
		{
			return source.Select(p => new OrcaRecorderPosition { AccountName = p.AccountName, InstrumentName = p.InstrumentName, Quantity = p.Quantity, MarketPosition = p.MarketPosition }).ToList();
		}

		private static long GetAvailableFreeSpace(string path)
		{
			string root = Path.GetPathRoot(Path.GetFullPath(path));
			return new DriveInfo(root).AvailableFreeSpace;
		}

		private static string GetDiskHealth(OrcaTradeRecorderSettings local, string path)
		{
			try { return FormatGb(GetAvailableFreeSpace(path)) + " GB free"; }
			catch { return "Unavailable"; }
		}

		private static string FormatGb(long bytes)
		{
			return (bytes / 1024d / 1024d / 1024d).ToString("0.0", CultureInfo.InvariantCulture);
		}

		private static string QuoteArgument(string value)
		{
			return "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";
		}

		public void Dispose()
		{
			if (disposed)
				return;
			disposed = true;
			try {
				if (dispatcher.CheckAccess()) {
					timer.Stop();
					timer.Tick -= OnTimerTick;
					UnhookAllAccounts();
				} else {
					dispatcher.InvokeAsync(() => {
						timer.Stop();
						timer.Tick -= OnTimerTick;
						UnhookAllAccounts();
					});
				}
			} catch { }
			try {
				if (currentCapture != null && obsClient.IsConnected)
					obsClient.TryCallAsync("StopRecord", null, TimeSpan.FromSeconds(2)).Wait(2500);
			} catch { }
			obsClient.Disconnect();
			OrcaTradeRecorderDiagnostics.Write("Runtime disposed.");
		}
	}

	internal sealed class OrcaObsWebSocketClient
	{
		private readonly JavaScriptSerializer serializer = new JavaScriptSerializer { MaxJsonLength = 1024 * 1024 };
		private ClientWebSocket socket;
		private int requestSequence;

		public bool IsConnected { get { return socket != null && socket.State == WebSocketState.Open; } }

		public async Task ConnectAsync(OrcaTradeRecorderSettings settings, TimeSpan timeout)
		{
			if (IsConnected)
				return;
			Disconnect();
			ClientWebSocket candidate = new ClientWebSocket();
			CancellationTokenSource cts = new CancellationTokenSource(timeout);
			try {
				Uri uri = new Uri("ws://" + settings.ObsHost + ":" + settings.ObsPort.ToString(CultureInfo.InvariantCulture));
				await candidate.ConnectAsync(uri, cts.Token).ConfigureAwait(false);
				socket = candidate;
				Dictionary<string, object> hello = await ReceiveMessageAsync(TimeSpan.FromSeconds(8)).ConfigureAwait(false);
				if (GetInt(hello, "op") != 0)
					throw new InvalidOperationException("OBS did not send the expected WebSocket Hello message.");
				Dictionary<string, object> helloData = GetDictionary(hello, "d");
				Dictionary<string, object> authentication = GetDictionary(helloData, "authentication");
				Dictionary<string, object> identifyData = new Dictionary<string, object> {
					{ "rpcVersion", 1 },
					{ "eventSubscriptions", 0 }
				};
				if (authentication != null && authentication.Count > 0) {
					string challenge = GetString(authentication, "challenge");
					string salt = GetString(authentication, "salt");
					identifyData["authentication"] = BuildAuthentication(settings.ObsPassword, salt, challenge);
				}
				await SendMessageAsync(new Dictionary<string, object> { { "op", 1 }, { "d", identifyData } }, TimeSpan.FromSeconds(8)).ConfigureAwait(false);
				Dictionary<string, object> identified = await ReceiveMessageAsync(TimeSpan.FromSeconds(8)).ConfigureAwait(false);
				if (GetInt(identified, "op") != 2)
					throw new InvalidOperationException("OBS rejected the WebSocket identification request. Check the password.");
			} catch {
				if (ReferenceEquals(socket, candidate)) socket = null;
				candidate.Dispose();
				throw;
			} finally {
				cts.Dispose();
			}
		}

		public async Task<Dictionary<string, object>> CallAsync(string requestType, Dictionary<string, object> requestData, TimeSpan timeout)
		{
			if (!IsConnected)
				throw new InvalidOperationException("OBS WebSocket is not connected.");
			string requestId = Interlocked.Increment(ref requestSequence).ToString(CultureInfo.InvariantCulture);
			Dictionary<string, object> data = new Dictionary<string, object> {
				{ "requestType", requestType },
				{ "requestId", requestId }
			};
			if (requestData != null)
				data["requestData"] = requestData;
			await SendMessageAsync(new Dictionary<string, object> { { "op", 6 }, { "d", data } }, timeout).ConfigureAwait(false);

			DateTime deadline = DateTime.UtcNow + timeout;
			while (DateTime.UtcNow < deadline) {
				Dictionary<string, object> message = await ReceiveMessageAsync(deadline - DateTime.UtcNow).ConfigureAwait(false);
				if (GetInt(message, "op") != 7)
					continue;
				Dictionary<string, object> response = GetDictionary(message, "d");
				if (!string.Equals(GetString(response, "requestId"), requestId, StringComparison.Ordinal))
					continue;
				Dictionary<string, object> status = GetDictionary(response, "requestStatus");
				if (!GetBoolean(status, "result"))
					throw new InvalidOperationException("OBS " + requestType + " failed: " + GetString(status, "comment"));
				return GetDictionary(response, "responseData") ?? new Dictionary<string, object>();
			}
			throw new TimeoutException("OBS did not answer " + requestType + " within " + timeout.TotalSeconds.ToString("0", CultureInfo.InvariantCulture) + " seconds.");
		}

		public async Task<Dictionary<string, object>> TryCallAsync(string requestType, Dictionary<string, object> requestData, TimeSpan timeout)
		{
			try { return await CallAsync(requestType, requestData, timeout).ConfigureAwait(false); }
			catch { return new Dictionary<string, object>(); }
		}

		private async Task SendMessageAsync(Dictionary<string, object> message, TimeSpan timeout)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(serializer.Serialize(message));
			CancellationTokenSource cts = new CancellationTokenSource(timeout);
			try {
				await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token).ConfigureAwait(false);
			} finally { cts.Dispose(); }
		}

		private async Task<Dictionary<string, object>> ReceiveMessageAsync(TimeSpan timeout)
		{
			if (timeout <= TimeSpan.Zero)
				throw new TimeoutException("OBS response timed out.");
			byte[] buffer = new byte[8192];
			using (MemoryStream stream = new MemoryStream())
			using (CancellationTokenSource cts = new CancellationTokenSource(timeout)) {
				while (true) {
					WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token).ConfigureAwait(false);
					if (result.MessageType == WebSocketMessageType.Close)
						throw new IOException("OBS closed the WebSocket connection.");
					stream.Write(buffer, 0, result.Count);
					if (result.EndOfMessage)
						break;
					if (stream.Length > 1024 * 1024)
						throw new InvalidDataException("OBS returned an unexpectedly large WebSocket message.");
				}
				string json = Encoding.UTF8.GetString(stream.ToArray());
				return serializer.Deserialize<Dictionary<string, object>>(json);
			}
		}

		private static string BuildAuthentication(string password, string salt, string challenge)
		{
			using (SHA256 sha = SHA256.Create()) {
				string secret = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes((password ?? string.Empty) + (salt ?? string.Empty))));
				return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(secret + (challenge ?? string.Empty))));
			}
		}

		public void Disconnect()
		{
			ClientWebSocket current = socket;
			socket = null;
			if (current == null)
				return;
			try { current.Abort(); } catch { }
			try { current.Dispose(); } catch { }
		}

		public static Dictionary<string, object> GetDictionary(Dictionary<string, object> source, string key)
		{
			if (source == null || string.IsNullOrEmpty(key))
				return null;
			object value;
			if (!source.TryGetValue(key, out value) || value == null)
				return null;
			return value as Dictionary<string, object>;
		}

		public static string GetString(Dictionary<string, object> source, string key)
		{
			if (source == null)
				return string.Empty;
			object value;
			return source.TryGetValue(key, out value) && value != null ? Convert.ToString(value, CultureInfo.InvariantCulture) : string.Empty;
		}

		public static int GetInt(Dictionary<string, object> source, string key)
		{
			if (source == null)
				return 0;
			object value;
			if (!source.TryGetValue(key, out value) || value == null)
				return 0;
			try { return Convert.ToInt32(value, CultureInfo.InvariantCulture); } catch { return 0; }
		}

		public static bool GetBoolean(Dictionary<string, object> source, string key)
		{
			if (source == null)
				return false;
			object value;
			if (!source.TryGetValue(key, out value) || value == null)
				return false;
			try { return Convert.ToBoolean(value, CultureInfo.InvariantCulture); } catch { return false; }
		}

		public static bool ArrayContainsNamedValue(Dictionary<string, object> source, string arrayKey, string nameKey, string expected)
		{
			if (source == null)
				return false;
			object raw;
			if (!source.TryGetValue(arrayKey, out raw) || raw == null)
				return false;
			object[] array = raw as object[];
			if (array == null) {
				System.Collections.ArrayList list = raw as System.Collections.ArrayList;
				if (list != null) array = list.ToArray();
			}
			if (array == null)
				return false;
			foreach (object item in array) {
				Dictionary<string, object> dictionary = item as Dictionary<string, object>;
				if (dictionary != null && string.Equals(GetString(dictionary, nameKey), expected, StringComparison.OrdinalIgnoreCase))
					return true;
			}
			return false;
		}
	}

	internal static class OrcaTradeRecorderFinalizer
	{
		public static async Task FinalizeAsync(OrcaRecorderCaptureManifest capture, OrcaTradeRecorderSettings settings)
		{
			if (capture == null)
				throw new ArgumentNullException("capture");
			if (string.IsNullOrWhiteSpace(settings.FfmpegPath) || !File.Exists(settings.FfmpegPath))
				throw new FileNotFoundException("FFmpeg was not found. Raw segments remain preserved.", settings.FfmpegPath);
			List<string> segments = capture.RawSegments.Where(File.Exists).ToList();
			if (segments.Count == 0)
				throw new FileNotFoundException("No raw recording segments were found.");

			string bundle = capture.BundleDirectory;
			Directory.CreateDirectory(bundle);
			string concatPath = Path.Combine(bundle, "segments.concat.txt");
			StringBuilder list = new StringBuilder();
			foreach (string segment in segments)
				list.Append("file '").Append(segment.Replace("'", "'\\''")).AppendLine("'");
			File.WriteAllText(concatPath, list.ToString(), new UTF8Encoding(false));
			string partial = Path.Combine(bundle, "trade-" + capture.CaptureId + ".partial.mp4");
			string final = Path.Combine(bundle, "trade-" + capture.CaptureId + ".mp4");
			if (File.Exists(partial)) File.Delete(partial);
			string arguments = "-hide_banner -loglevel error -y -f concat -safe 0 -i " + Quote(concatPath) + " -c copy " + Quote(partial);
			ProcessResult result = await RunProcessAsync(settings.FfmpegPath, arguments, TimeSpan.FromMinutes(3)).ConfigureAwait(false);
			if (result.ExitCode != 0 || !File.Exists(partial) || new FileInfo(partial).Length == 0)
				throw new InvalidOperationException("FFmpeg join failed: " + Trim(result.Error, 800));

			string ffprobe = Path.Combine(Path.GetDirectoryName(settings.FfmpegPath), "ffprobe.exe");
			if (!File.Exists(ffprobe))
				throw new FileNotFoundException("ffprobe.exe is required to validate the final recording.", ffprobe);
			ProcessResult probe = await RunProcessAsync(ffprobe, "-v error -show_entries stream=codec_type -of default=noprint_wrappers=1:nokey=1 " + Quote(partial), TimeSpan.FromSeconds(30)).ConfigureAwait(false);
			if (probe.ExitCode != 0 || probe.Output.IndexOf("video", StringComparison.OrdinalIgnoreCase) < 0 || probe.Output.IndexOf("audio", StringComparison.OrdinalIgnoreCase) < 0)
				throw new InvalidDataException("Final recording validation did not find both video and microphone audio streams.");

			if (File.Exists(final))
				final = Path.Combine(bundle, "trade-" + capture.CaptureId + "-" + DateTime.Now.ToString("HHmmss", CultureInfo.InvariantCulture) + ".mp4");
			File.Move(partial, final);
			capture.FinalVideoPath = final;
			capture.FinalizationStatus = "Complete";
			capture.FinalizationError = string.Empty;
			capture.Events.Add(new OrcaRecorderManifestEvent { Utc = DateTime.UtcNow, Type = "FinalizationComplete", Detail = final });
			WriteManifest(capture);
		}

		private static async Task<ProcessResult> RunProcessAsync(string executable, string arguments, TimeSpan timeout)
		{
			return await Task.Run(() => {
				using (Process process = new Process()) {
					process.StartInfo = new ProcessStartInfo {
						FileName = executable,
						Arguments = arguments,
						UseShellExecute = false,
						CreateNoWindow = true,
						WindowStyle = ProcessWindowStyle.Hidden,
						RedirectStandardOutput = true,
						RedirectStandardError = true
					};
					process.Start();
					string output = process.StandardOutput.ReadToEnd();
					string error = process.StandardError.ReadToEnd();
					if (!process.WaitForExit((int)Math.Min(int.MaxValue, timeout.TotalMilliseconds))) {
						try { process.Kill(); } catch { }
						throw new TimeoutException(Path.GetFileName(executable) + " did not finish within " + timeout.TotalSeconds.ToString("0", CultureInfo.InvariantCulture) + " seconds.");
					}
					return new ProcessResult { ExitCode = process.ExitCode, Output = output ?? string.Empty, Error = error ?? string.Empty };
				}
			}).ConfigureAwait(false);
		}

		private static void WriteManifest(OrcaRecorderCaptureManifest capture)
		{
			string path = Path.Combine(capture.BundleDirectory, "capture.json");
			string temporary = path + ".tmp";
			string json = new JavaScriptSerializer { MaxJsonLength = 1024 * 1024 }.Serialize(capture);
			File.WriteAllText(temporary, json, new UTF8Encoding(false));
			if (File.Exists(path)) File.Copy(temporary, path, true); else File.Move(temporary, path);
			if (File.Exists(temporary)) File.Delete(temporary);
		}

		private static string Quote(string value) { return "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\""; }
		private static string Trim(string value, int length) { return string.IsNullOrEmpty(value) || value.Length <= length ? value : value.Substring(0, length); }

		private sealed class ProcessResult
		{
			public int ExitCode;
			public string Output;
			public string Error;
		}
	}

	internal static class OrcaTradeRecorderDiagnostics
	{
		private static readonly ConcurrentQueue<string> Queue = new ConcurrentQueue<string>();
		private static int queuedCount;
		private static int writerScheduled;
		private const int MaxQueuedLines = 500;

		private static string LogPath
		{
			get {
				return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NinjaTrader 8", "OrcaTradeRecorder.log");
			}
		}

		public static void Write(string message)
		{
			if (string.IsNullOrWhiteSpace(message) || Interlocked.Increment(ref queuedCount) > MaxQueuedLines) {
				Interlocked.Decrement(ref queuedCount);
				return;
			}
			Queue.Enqueue(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + " " + message.Replace("password", "credential"));
			if (Interlocked.Exchange(ref writerScheduled, 1) == 0)
				Task.Run((Action)Flush);
		}

		private static void Flush()
		{
			try {
				List<string> lines = new List<string>();
				string line;
				while (Queue.TryDequeue(out line)) {
					Interlocked.Decrement(ref queuedCount);
					lines.Add(line);
				}
				if (lines.Count > 0) {
					string directory = Path.GetDirectoryName(LogPath);
					Directory.CreateDirectory(directory);
					if (File.Exists(LogPath) && new FileInfo(LogPath).Length > 2 * 1024 * 1024) {
						string archive = LogPath + ".1";
						if (File.Exists(archive)) File.Delete(archive);
						File.Move(LogPath, archive);
					}
					File.AppendAllLines(LogPath, lines, new UTF8Encoding(false));
				}
			} catch { }
			finally {
				Interlocked.Exchange(ref writerScheduled, 0);
				if (!Queue.IsEmpty && Interlocked.Exchange(ref writerScheduled, 1) == 0)
					Task.Run((Action)Flush);
			}
		}
	}

	internal sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
	{
		public static readonly ReferenceEqualityComparer<T> Instance = new ReferenceEqualityComparer<T>();
		public bool Equals(T x, T y) { return ReferenceEquals(x, y); }
		public int GetHashCode(T obj) { return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj); }
	}

	public sealed class OrcaTradeRecorderWindow : NTWindow
	{
		private static OrcaTradeRecorderWindow instance;
		private readonly OrcaTradeRecorderRuntime runtime;
		private readonly TextBlock stateText;
		private readonly TextBlock summaryText;
		private readonly TextBlock healthText;
		private readonly TextBlock tradeText;
		private readonly Button armButton;
		private readonly TextBox obsPathBox;
		private readonly TextBox hostBox;
		private readonly TextBox portBox;
		private readonly PasswordBox passwordBox;
		private readonly TextBox profileBox;
		private readonly TextBox collectionBox;
		private readonly TextBox sceneBox;
		private readonly TextBox microphoneBox;
		private readonly TextBox outputBox;
		private readonly TextBox ffmpegBox;
		private readonly TextBox preRollBox;
		private readonly TextBox postRollBox;
		private readonly TextBox freeSpaceBox;
		private readonly CheckBox autoLaunchBox;

		private OrcaTradeRecorderWindow(OrcaTradeRecorderRuntime runtime)
		{
			this.runtime = runtime;
			Title = "Orca Trade Recorder";
			Width = 690;
			Height = 760;
			MinWidth = 600;
			MinHeight = 600;
			Background = new SolidColorBrush(Color.FromRgb(24, 25, 29));
			Foreground = Brushes.WhiteSmoke;

			StackPanel root = new StackPanel { Margin = new Thickness(16) };
			stateText = new TextBlock { FontSize = 22, FontWeight = FontWeights.SemiBold, Foreground = Brushes.WhiteSmoke };
			summaryText = new TextBlock { Margin = new Thickness(0, 4, 0, 12), TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(Color.FromRgb(205, 207, 215)) };
			root.Children.Add(stateText);
			root.Children.Add(summaryText);

			Border healthCard = new Border { Background = new SolidColorBrush(Color.FromRgb(38, 39, 45)), BorderBrush = new SolidColorBrush(Color.FromRgb(72, 74, 84)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 12) };
			StackPanel healthStack = new StackPanel();
			healthText = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.WhiteSmoke };
			tradeText = new TextBlock { Margin = new Thickness(0, 6, 0, 0), TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(Color.FromRgb(179, 205, 235)) };
			healthStack.Children.Add(healthText);
			healthStack.Children.Add(tradeText);
			healthCard.Child = healthStack;
			root.Children.Add(healthCard);

			WrapPanel controls = new WrapPanel { Margin = new Thickness(0, 0, 0, 14) };
			armButton = MakeButton("Arm", 110);
			Button testConnection = MakeButton("Test Connection", 120);
			Button testRecording = MakeButton("Test Recording", 120);
			Button openObs = MakeButton("Open OBS", 90);
			controls.Children.Add(armButton);
			controls.Children.Add(testConnection);
			controls.Children.Add(testRecording);
			controls.Children.Add(openObs);
			root.Children.Add(controls);

			TextBlock settingsHeader = new TextBlock { Text = "RECORDER SETTINGS", FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(151, 170, 194)), Margin = new Thickness(0, 0, 0, 7) };
			root.Children.Add(settingsHeader);
			Grid settingsGrid = new Grid();
			settingsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(165) });
			settingsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

			obsPathBox = AddTextRow(settingsGrid, 0, "OBS executable", string.Empty);
			hostBox = AddTextRow(settingsGrid, 1, "WebSocket host", string.Empty);
			portBox = AddTextRow(settingsGrid, 2, "WebSocket port", string.Empty);
			passwordBox = AddPasswordRow(settingsGrid, 3, "WebSocket password");
			profileBox = AddTextRow(settingsGrid, 4, "OBS profile", string.Empty);
			collectionBox = AddTextRow(settingsGrid, 5, "Scene collection", string.Empty);
			sceneBox = AddTextRow(settingsGrid, 6, "Capture scene", string.Empty);
			microphoneBox = AddTextRow(settingsGrid, 7, "Microphone input", string.Empty);
			outputBox = AddTextRow(settingsGrid, 8, "Output directory", string.Empty);
			ffmpegBox = AddTextRow(settingsGrid, 9, "FFmpeg executable", string.Empty);
			preRollBox = AddTextRow(settingsGrid, 10, "Pre-roll seconds", string.Empty);
			postRollBox = AddTextRow(settingsGrid, 11, "Post-roll seconds", string.Empty);
			freeSpaceBox = AddTextRow(settingsGrid, 12, "Minimum free GB", string.Empty);
			autoLaunchBox = AddCheckRow(settingsGrid, 13, "Launch OBS when arming");
			root.Children.Add(settingsGrid);

			Button save = MakeButton("Save Settings", 120);
			save.Margin = new Thickness(0, 12, 0, 0);
			root.Children.Add(save);
			TextBlock note = new TextBlock {
				Text = "Use a dedicated OBS profile. Configure the selected scene for the full trading monitor by default, or choose another trader-created window/region scene. Disable Desktop Audio and leave only the selected microphone enabled.",
				TextWrapping = TextWrapping.Wrap,
				Foreground = new SolidColorBrush(Color.FromRgb(168, 170, 180)),
				Margin = new Thickness(0, 12, 0, 0)
			};
			root.Children.Add(note);

			ScrollViewer scroll = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
			Content = scroll;
			LoadSettings();
			RefreshSnapshot();

			armButton.Click += OnArmClick;
			testConnection.Click += delegate { if (SaveSettingsFromUi()) runtime.TestConnection(); };
			testRecording.Click += delegate { if (SaveSettingsFromUi()) runtime.TestRecording(); };
			openObs.Click += delegate { if (SaveSettingsFromUi()) runtime.OpenObs(); };
			save.Click += delegate { SaveSettingsFromUi(); };
			runtime.SnapshotChanged += OnSnapshotChanged;
			Closed += OnClosed;
		}

		public static void ShowOrActivate(OrcaTradeRecorderRuntime runtime)
		{
			if (instance != null) {
				if (instance.WindowState == WindowState.Minimized) instance.WindowState = WindowState.Normal;
				instance.Activate();
				return;
			}
			instance = new OrcaTradeRecorderWindow(runtime);
			instance.Show();
		}

		private void OnClosed(object sender, EventArgs e)
		{
			runtime.SnapshotChanged -= OnSnapshotChanged;
			instance = null;
		}

		private void OnArmClick(object sender, RoutedEventArgs e)
		{
			OrcaTradeRecorderSnapshot snapshot = runtime.GetSnapshot();
			if (!snapshot.IsArmed && snapshot.State != OrcaTradeRecorderState.Starting && snapshot.State != OrcaTradeRecorderState.Finalizing) {
				if (!SaveSettingsFromUi())
					return;
				runtime.Arm();
				return;
			}
			if ((snapshot.State == OrcaTradeRecorderState.Recording || snapshot.State == OrcaTradeRecorderState.Tail)
				&& MessageBox.Show(this, "A trade recording is active. Stop, preserve, and finalize it now?", "Disarm Orca Trade Recorder", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
				return;
			runtime.Disarm();
		}

		private void OnSnapshotChanged(object sender, EventArgs e)
		{
			if (Dispatcher.CheckAccess()) RefreshSnapshot(); else Dispatcher.InvokeAsync(RefreshSnapshot);
		}

		private void RefreshSnapshot()
		{
			OrcaTradeRecorderSnapshot snapshot = runtime.GetSnapshot();
			stateText.Text = snapshot.State.ToString().ToUpperInvariant();
			stateText.Foreground = StateBrush(snapshot.State);
			summaryText.Text = snapshot.Summary;
			healthText.Text = "OBS: " + snapshot.ObsHealth + "    Replay: " + snapshot.ReplayHealth
				+ Environment.NewLine + "Microphone: " + snapshot.MicrophoneHealth
				+ Environment.NewLine + "Disk: " + snapshot.DiskHealth + "    Accounts: " + snapshot.AccountHealth;
			tradeText.Text = "Open positions: " + snapshot.OpenPositionCount.ToString(CultureInfo.InvariantCulture)
				+ (string.IsNullOrEmpty(snapshot.ActiveInstruments) ? string.Empty : "    Instruments: " + snapshot.ActiveInstruments)
				+ Environment.NewLine + "Recording: " + FormatElapsed(snapshot.RecordingElapsed)
				+ "    Pending clips: " + snapshot.PendingClipCount.ToString(CultureInfo.InvariantCulture);
			armButton.Content = snapshot.IsArmed || snapshot.State == OrcaTradeRecorderState.Starting ? "Disarm" : "Arm";
			armButton.Background = snapshot.IsArmed ? new SolidColorBrush(Color.FromRgb(128, 47, 57)) : new SolidColorBrush(Color.FromRgb(38, 111, 74));
		}

		private void LoadSettings()
		{
			OrcaTradeRecorderSettings settings = runtime.GetSettings();
			obsPathBox.Text = settings.ObsExecutablePath;
			hostBox.Text = settings.ObsHost;
			portBox.Text = settings.ObsPort.ToString(CultureInfo.InvariantCulture);
			passwordBox.Password = settings.ObsPassword ?? string.Empty;
			profileBox.Text = settings.ObsProfile;
			collectionBox.Text = settings.ObsSceneCollection;
			sceneBox.Text = settings.ObsScene;
			microphoneBox.Text = settings.MicrophoneInput;
			outputBox.Text = settings.OutputDirectory;
			ffmpegBox.Text = settings.FfmpegPath;
			preRollBox.Text = settings.PreRollSeconds.ToString(CultureInfo.InvariantCulture);
			postRollBox.Text = settings.PostRollSeconds.ToString(CultureInfo.InvariantCulture);
			freeSpaceBox.Text = settings.MinimumFreeSpaceGb.ToString("0.##", CultureInfo.InvariantCulture);
			autoLaunchBox.IsChecked = settings.AutoLaunchObs;
		}

		private bool SaveSettingsFromUi()
		{
			try {
				int port, pre, post;
				double free;
				if (!int.TryParse(portBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out port)) throw new FormatException("WebSocket port must be a number.");
				if (!int.TryParse(preRollBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out pre)) throw new FormatException("Pre-roll seconds must be a number.");
				if (!int.TryParse(postRollBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out post)) throw new FormatException("Post-roll seconds must be a number.");
				if (!double.TryParse(freeSpaceBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out free)) throw new FormatException("Minimum free GB must be a number.");
				runtime.SaveSettings(new OrcaTradeRecorderSettings {
					ObsExecutablePath = obsPathBox.Text,
					ObsHost = hostBox.Text,
					ObsPort = port,
					ObsPassword = passwordBox.Password,
					ObsProfile = profileBox.Text,
					ObsSceneCollection = collectionBox.Text,
					ObsScene = sceneBox.Text,
					MicrophoneInput = microphoneBox.Text,
					OutputDirectory = outputBox.Text,
					FfmpegPath = ffmpegBox.Text,
					PreRollSeconds = pre,
					PostRollSeconds = post,
					MinimumFreeSpaceGb = free,
					AutoLaunchObs = autoLaunchBox.IsChecked == true
				});
				return true;
			} catch (Exception ex) {
				MessageBox.Show(this, ex.Message, "Orca Trade Recorder Settings", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
		}

		private static TextBox AddTextRow(Grid grid, int row, string label, string value)
		{
			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			TextBlock caption = new TextBlock { Text = label, Foreground = new SolidColorBrush(Color.FromRgb(199, 201, 210)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 4, 8, 4) };
			TextBox box = new TextBox { Text = value, Margin = new Thickness(0, 3, 0, 3), Background = new SolidColorBrush(Color.FromRgb(49, 50, 57)), Foreground = Brushes.WhiteSmoke, BorderBrush = new SolidColorBrush(Color.FromRgb(79, 81, 91)) };
			Grid.SetRow(caption, row); Grid.SetColumn(caption, 0);
			Grid.SetRow(box, row); Grid.SetColumn(box, 1);
			grid.Children.Add(caption); grid.Children.Add(box);
			return box;
		}

		private static PasswordBox AddPasswordRow(Grid grid, int row, string label)
		{
			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			TextBlock caption = new TextBlock { Text = label, Foreground = new SolidColorBrush(Color.FromRgb(199, 201, 210)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 4, 8, 4) };
			PasswordBox box = new PasswordBox { Margin = new Thickness(0, 3, 0, 3), Background = new SolidColorBrush(Color.FromRgb(49, 50, 57)), Foreground = Brushes.WhiteSmoke, BorderBrush = new SolidColorBrush(Color.FromRgb(79, 81, 91)) };
			Grid.SetRow(caption, row); Grid.SetColumn(caption, 0);
			Grid.SetRow(box, row); Grid.SetColumn(box, 1);
			grid.Children.Add(caption); grid.Children.Add(box);
			return box;
		}

		private static CheckBox AddCheckRow(Grid grid, int row, string label)
		{
			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			CheckBox box = new CheckBox { Content = label, Foreground = Brushes.WhiteSmoke, Margin = new Thickness(0, 7, 0, 4) };
			Grid.SetRow(box, row); Grid.SetColumn(box, 1);
			grid.Children.Add(box);
			return box;
		}

		private static Button MakeButton(string text, double width)
		{
			return new Button { Content = text, MinWidth = width, Margin = new Thickness(0, 0, 8, 5), Padding = new Thickness(9, 5, 9, 5), Background = new SolidColorBrush(Color.FromRgb(54, 56, 64)), Foreground = Brushes.WhiteSmoke, BorderBrush = new SolidColorBrush(Color.FromRgb(84, 86, 97)) };
		}

		private static Brush StateBrush(OrcaTradeRecorderState state)
		{
			if (state == OrcaTradeRecorderState.Armed) return new SolidColorBrush(Color.FromRgb(83, 205, 134));
			if (state == OrcaTradeRecorderState.Recording || state == OrcaTradeRecorderState.Tail) return new SolidColorBrush(Color.FromRgb(255, 101, 111));
			if (state == OrcaTradeRecorderState.Error) return new SolidColorBrush(Color.FromRgb(255, 177, 86));
			return Brushes.WhiteSmoke;
		}

		private static string FormatElapsed(TimeSpan value)
		{
			if (value < TimeSpan.Zero) value = TimeSpan.Zero;
			return ((int)value.TotalHours).ToString("00", CultureInfo.InvariantCulture) + ":" + value.Minutes.ToString("00", CultureInfo.InvariantCulture) + ":" + value.Seconds.ToString("00", CultureInfo.InvariantCulture);
		}
	}
}
