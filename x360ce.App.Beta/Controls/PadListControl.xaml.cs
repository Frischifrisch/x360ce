﻿using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Controls;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for UserSettingMapListControl.xaml
	/// </summary>
	public partial class PadListControl : UserControl
	{
		public PadListControl()
		{
			InitializeComponent();
		}

		MapTo _MappedTo;
		UserSetting _UserSetting;
		SortableBindingList<Engine.Data.UserSetting> mappedUserSettings = new SortableBindingList<Engine.Data.UserSetting>();

		private void UserSettings_Items_ListChanged(object sender, ListChangedEventArgs e)
		{
			// Make sure there is no crash when function gets called from another thread.
			ControlsHelper.BeginInvoke(() =>
			{
				UpdateMappedUserSettings();
			});
		}

		object DevicesToMapDataGridViewLock = new object();

		void UpdateMappedUserSettings()
		{
			lock (DevicesToMapDataGridViewLock)
			{
				var grid = MainDataGrid;
				var game = SettingsManager.CurrentGame;
				// Get rows which must be displayed on the list.
				var itemsToShow = SettingsManager.UserSettings.ItemsToArraySyncronized()
					// Filter devices by controller.	
					.Where(x => x.MapTo == (int)_MappedTo)
					// Filter devices by selected game (no items will be shown if game is not selected).
					.Where(x => game != null && x.FileName == game.FileName && x.FileProductName == game.FileProductName)
					.ToList();
				var itemsToRemove = mappedUserSettings.Except(itemsToShow).ToArray();
				var itemsToInsert = itemsToShow.Except(mappedUserSettings).ToArray();
				// If columns will be hidden or shown then...
				if (itemsToRemove.Length > 0 || itemsToInsert.Length > 0)
				{
					// Do removal.
					foreach (var item in itemsToRemove)
						mappedUserSettings.Remove(item);
					// Do adding.
					foreach (var item in itemsToInsert)
						mappedUserSettings.Add(item);
				}
				foreach (var item in itemsToInsert)
					grid.SelectedItems.Add(item);
				var visibleCount = mappedUserSettings.Count();
				var title = string.Format("Enable {0} Mapped Device{1}", visibleCount, visibleCount == 1 ? "" : "s");
				if (mappedUserSettings.Count(x => x.IsEnabled) > 1)
					title += " (Combine)";
				ControlsHelper.SetText(EnabledLabel, title);
			}
		}

		public void SetBinding(MapTo mappedTo, UserSetting userSetting)
		{
			_MappedTo = mappedTo;
			_UserSetting = userSetting;
		}

		public void UpdateFromCurrentGame()
		{
			var game = SettingsManager.CurrentGame;
			var flag = AppHelper.GetMapFlag(_MappedTo);
			// Update Virtual.
			var virt = game != null && ((MapToMask)game.EnableMask).HasFlag(flag);
			EnabledCheckBox.IsChecked = virt;
			EnabledContentControl.Content = virt
				? Resources[Icons_Default.Icon_checkbox]
				: Resources[Icons_Default.Icon_checkbox_unchecked];         
			// Update AutoMap.
			var auto = game != null && ((MapToMask)game.AutoMapMask).HasFlag(flag);
			AutoMapCheckBox.IsChecked = auto;
			AutoMapContentControl.Content = auto
				? Resources[Icons_Default.Icon_checkbox]
				: Resources[Icons_Default.Icon_checkbox_unchecked];
			MainDataGrid.IsEnabled = !auto;
			MainDataGrid.Background = auto
				? SystemColors.ControlBrush
				: SystemColors.WindowBrush;
			//MainDataGrid.DefaultCellStyle.BackColor = auto
			//	? SystemColors.Control
			//	: SystemColors.Window;
			if (auto)
			{
				foreach (var item in mappedUserSettings)
					item.MapTo = (int)MapTo.None;
			}
			UpdateMappedUserSettings();
			UpdateGridButtons();
		}


		void UpdateGridButtons()
		{
			var grid = MainDataGrid;
			var game = SettingsManager.CurrentGame;
			var flag = AppHelper.GetMapFlag(_MappedTo);
			var auto = game != null && ((MapToMask)game.AutoMapMask).HasFlag(flag);
			// Buttons must be disabled if AutoMapping enabled for the game.
			RemoveButton.IsEnabled = !auto && grid.SelectedItems.Count > 0;
			AddButton.IsEnabled = !auto;
		}

		async private void EnabledCheckBox_Click(object sender, RoutedEventArgs e)
		{
			var box = (CheckBox)sender;
			var newValue = box.IsChecked ?? false;
			// ShowSystemDevicesButton.IsChecked = newValue;
			EnabledContentControl.Content = newValue
				? Resources[Icons_Default.Icon_checkbox]
				: Resources[Icons_Default.Icon_checkbox_unchecked];
			// Process.
			var game = SettingsManager.CurrentGame;
			// If no game selected then ignore click.
			if (game == null)
				return;
			var flag = AppHelper.GetMapFlag(_MappedTo);
			var value = (MapToMask)game.EnableMask;
			var type = game.EmulationType;
			var autoMap = value.HasFlag(flag);
			// Invert flag value.
			var enableMask = autoMap
				// Remove AUTO.
				? (int)(value & ~flag)
				// Add AUTO.	
				: (int)(value | flag);
			// Update emulation type.
			EmulationType? newType = null;
			// If emulation enabled and game is not using virtual type then...
			if (enableMask > 0 && type != (int)EmulationType.Virtual)
				newType = EmulationType.Virtual;
			// If emulation disabled, but game use virtual emulation then...
			if (enableMask == 0 && type == (int)EmulationType.Virtual)
				newType = EmulationType.None;
			// Set values.
			game.EnableMask = enableMask;
			if (newType.HasValue)
				game.EmulationType = (int)newType.Value;
		}

		private void AutoMapCheckBox_Click(object sender, RoutedEventArgs e)
		{
			var box = (CheckBox)sender;
			var newValue = box.IsChecked ?? false;
			// ShowSystemDevicesButton.IsChecked = newValue;
			AutoMapContentControl.Content = newValue
				? Resources[Icons_Default.Icon_checkbox]
				: Resources[Icons_Default.Icon_checkbox_unchecked];
			// Process.
			var game = SettingsManager.CurrentGame;
			// If no game selected then ignore click.
			if (game == null)
				return;
			var flag = AppHelper.GetMapFlag(_MappedTo);
			var value = (MapToMask)game.AutoMapMask;
			var autoMap = value.HasFlag(flag);
			// If AUTO enabled then...
			if (autoMap)
			{
				// Remove AUTO.
				game.AutoMapMask = (int)(value & ~flag);
			}
			else
			{
				// Add AUTO.
				game.AutoMapMask = (int)(value | flag);
			}
		}

		private void AddButton_Click(object sender, RoutedEventArgs e)
		{
			var game = SettingsManager.CurrentGame;
			// Return if game is not selected.
			if (game == null)
				return;
			// Show form which allows to select device.
			var selectedUserDevices = MainWindow.Current.ShowDeviceForm();
			// Return if no devices were selected.
			if (selectedUserDevices == null)
				return;
			// Check if device already have old settings before adding new ones.
			var noOldSettings = SettingsManager.GetSettings(game.FileName, _MappedTo).Count == 0;
			SettingsManager.MapGamePadDevices(game, _MappedTo, selectedUserDevices,
				SettingsManager.Options.HidGuardianConfigureAutomatically);
			var hasNewSettings = SettingsManager.GetSettings(game.FileName, _MappedTo).Count > 0;
			// If new devices mapped and button is not enabled then...
			if (noOldSettings && hasNewSettings && !(EnabledCheckBox.IsChecked == true))
			{
				// Enable mapping.
				EnabledCheckBox_Click(EnabledCheckBox, null);
			}
			SettingsManager.Current.RaiseSettingsChanged(null);
		}

		private void RemoveButton_Click(object sender, RoutedEventArgs e)
		{
			var win = new MessageBoxWindow();
			var text = "Do you really want to remove selected user setting?";
			var result = win.ShowDialog(text,
				"X360CE - Remove?", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question, System.Windows.MessageBoxResult.No);
			if (result != System.Windows.MessageBoxResult.Yes)
				return;
			var game = SettingsManager.CurrentGame;
			// Return if game is not selected.
			if (game == null)
				return;
			var settingsOld = SettingsManager.GetSettings(game.FileName, _MappedTo);
			SettingsManager.UnMapGamePadDevices(game, _UserSetting,
				SettingsManager.Options.HidGuardianConfigureAutomatically);
			var settingsNew = SettingsManager.GetSettings(game.FileName, _MappedTo);
			// if all devices unmapped and mapping is enabled then...
			if (settingsOld.Count > 0 && settingsNew.Count == 0 && (EnabledCheckBox.IsChecked == true))
			{
				// Disable mapping.
				EnabledCheckBox_Click(null, null);
			}
		}

		private void UseXInputStateCheckBox_Click(object sender, RoutedEventArgs e)
		{
			var box = (CheckBox)sender;
			var newValue = box.IsChecked ?? false;
			// ShowSystemDevicesButton.IsChecked = newValue;
			UseXInputStateContentControl.Content = newValue
				? Resources[Icons_Default.Icon_checkbox]
				: Resources[Icons_Default.Icon_checkbox_unchecked];

			ControlsHelper.BeginInvoke(() =>
			{
				SettingsManager.Options.GetXInputStates = !SettingsManager.Options.GetXInputStates;
			});
		}

		private void RefreshButton_Click(object sender, RoutedEventArgs e)
		{

		}

		private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateGridButtons();
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			var o = SettingsManager.Options;
			MainDataGrid.ItemsSource = mappedUserSettings;
			SettingsManager.LoadAndMonitor(o, nameof(o.GetXInputStates), EnabledCheckBox, null, null, System.Windows.Data.BindingMode.OneWay);
			SettingsManager.UserSettings.Items.ListChanged += UserSettings_Items_ListChanged;
			UserSettings_Items_ListChanged(null, null);
			UpdateGridButtons();
		}

		/*
		private void MappedDevicesDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = (DataGridView)sender;
			var viewRow = grid.Rows[e.RowIndex];
			var column = grid.Columns[e.ColumnIndex];
			var item = (Engine.Data.UserSetting)viewRow.DataBoundItem;
			if (column == IsOnlineColumn)
			{
				e.Value = item.IsOnline
					? Properties.Resources.bullet_square_glass_green
					: Properties.Resources.bullet_square_glass_grey;
			}
			else if (column == ConnectionClassColumn)
			{
				var device = SettingsManager.GetDevice(item.InstanceGuid);
				e.Value = device.ConnectionClass == Guid.Empty
					? new Bitmap(16, 16)
					: JocysCom.ClassLibrary.IO.DeviceDetector.GetClassIcon(device.ConnectionClass, 16)?.ToBitmap();
			}
			else if (column == InstanceIdColumn)
			{
				// Hide device Instance GUID from public eyes. Show part of checksum.
				e.Value = EngineHelper.GetID(item.InstanceGuid);
			}
			else if (column == SettingIdColumn)
			{
				// Hide device Setting GUID from public eyes. Show part of checksum.
				e.Value = EngineHelper.GetID(item.PadSettingChecksum);
			}
			else if (column == VendorNameColumn)
			{
				var device = SettingsManager.GetDevice(item.InstanceGuid);
				e.Value = device == null
					? ""
					: device.DevManufacturer;
			}
		}

		private void MappedDevicesDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = (DataGridView)sender;
			var column = grid.Columns[e.ColumnIndex];
			// If user clicked on the CheckBox column then...
			if (column == IsEnabledColumn)
			{
				var row = grid.Rows[e.RowIndex];
				var item = (Engine.Data.UserSetting)row.DataBoundItem;
				// Changed check (enabled state) of the current item.
				item.IsEnabled = !item.IsEnabled;
			}
		}

		*/

	}
}
