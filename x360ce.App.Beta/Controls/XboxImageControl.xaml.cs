﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for XboxImageControl.xaml
	/// </summary>
	public partial class XboxImageControl : UserControl
	{
		public XboxImageControl()
		{
			InitializeComponent();
		}

		PadControlImager Imager;
		ImageInfos Infos;

		public void InitializeImages(ImageInfos imageInfos, PadControlImager imager)
		{
			Infos = imageInfos;
			Imager = imager;
			foreach (var ii in imageInfos)
			{
				var nameCode = GetNameCode(ii.Code);
				var button = FindName(nameCode.ToString()) as Button;
				ii.ButtonControl = button;
				SetImage(ii.Code, NavImageType.Normal, false);
			}
		}

		public LayoutCode GetNameCode(LayoutCode code)
		{
			if (code == LayoutCode.LeftThumbAxisX)
				return LayoutCode.LeftThumbRight;
			if (code == LayoutCode.LeftThumbAxisY)
				return LayoutCode.LeftThumbUp;
			if (code == LayoutCode.RightThumbAxisX)
				return LayoutCode.RightThumbRight;
			if (code == LayoutCode.RightThumbAxisY)
				return LayoutCode.RightThumbUp;
			return code;
		}

		public void SetImage(LayoutCode code, NavImageType type, bool visible)
		{
			var nameCode = GetNameCode(code);
			var button = FindName(nameCode.ToString()) as Button;
			if (button == null)
				return;
			var content = (ContentControl)button.Content;
			var name = button.Name;
			var rx = new Regex("(Up|Left|Right|Down)$");
			var ms = rx.Matches(name);
			var m = ms.Count > 0 ? ms[0].Value : "";
			var resourceName = string.Format("Nav{0}{1}", m, type);
			var vb = FindResource(resourceName) as Viewbox;
			content.Content = visible ? vb : null;
		}

		public static System.Drawing.Bitmap CopyRegionIntoImage(System.Drawing.Bitmap source, int x, int y, int width, int height)
		{
			var region = new System.Drawing.Rectangle(x, y, width, height);
			return source.Clone(region, source.PixelFormat);
		}

		public System.Drawing.Bitmap ClipToCircle(System.Drawing.Bitmap original, float x, float y, float radius)
		{
			var copy = new System.Drawing.Bitmap(original);
			using (var g = System.Drawing.Graphics.FromImage(copy))
			{
				var r = new System.Drawing.RectangleF(x - radius, y - radius, radius * 2, radius * 2);
				var path = new System.Drawing.Drawing2D.GraphicsPath();
				path.AddEllipse(r);
				g.Clip = new System.Drawing.Region(path);
				g.DrawImage(original, 0, 0);
				return copy;
			}
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			var control = (Button)sender;
			var name = control.Name;
			LayoutCode code;
			if (!Enum.TryParse(name, false, out code))
				return;
			SetImage(code, NavImageType.Record, true);
			if (Imager.Recorder.Recording)
				return;
			var comboBox = Infos.Where(x => x.Code == code).Select(x => x.Control).First();
			var map = SettingsManager.Current.SettingsMap.First(x => x.Control == comboBox);
			StartRecording(map);
			// LeftThumbAxisX
			// LeftThumbAxisY
			// RightThumbAxisX
			// RightThumbAxisY
		}

		public Action<SettingsMapItem> StartRecording;

		private void MainGrid_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
		{
			Imager.ShowLeftThumbButtons = InRange(e, LeftThumbGrid);
			Imager.ShowRightThumbButtons = InRange(e, RightThumbGrid);
			Imager.ShowDPadButtons = InRange(e, DPadGrid);
			Imager.ShowMainButtons = InRange(e, MainButtonsGrid);
			Imager.ShowMenuButtons = InRange(e, MenuButtonsGrid);
			Imager.ShowTopButtons = InRange(e, TopButtonsGrid);
		}

		bool InRange(System.Windows.Input.MouseEventArgs e, FrameworkElement control)
		{
			var p = e.GetPosition(control);
			return
				Math.Abs(p.X - (control.Width / 2F)) < control.Width / 2F &&
				Math.Abs(p.Y - (control.Height / 2F)) < control.Height / 2F;
		}
	}

}