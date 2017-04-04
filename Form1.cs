using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;


namespace DxTermie
{
    internal static class UsbNotification
    {
        public const int DbtDevicearrival = 0x8000; // system detected a new device        
        public const int DbtDeviceremovecomplete = 0x8004; // device is gone      
        public const int WmDevicechange = 0x0219; // device change event      
        private const int DbtDevtypDeviceinterface = 5;
        private static readonly Guid GuidDevinterfaceUSBDevice = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED"); // USB devices
        private static IntPtr notificationHandle;

        /// <summary>
        /// Registers a window to receive notifications when USB devices are plugged or unplugged.
        /// </summary>
        /// <param name="windowHandle">Handle to the window receiving notifications.</param>
        public static void RegisterUsbDeviceNotification(IntPtr windowHandle)
        {
            DevBroadcastDeviceinterface dbi = new DevBroadcastDeviceinterface
            {
                DeviceType = DbtDevtypDeviceinterface,
                Reserved = 0,
                ClassGuid = GuidDevinterfaceUSBDevice,
                Name = 0
            };

            dbi.Size = Marshal.SizeOf(dbi);
            IntPtr buffer = Marshal.AllocHGlobal(dbi.Size);
            Marshal.StructureToPtr(dbi, buffer, true);

            notificationHandle = RegisterDeviceNotification(windowHandle, buffer, 0);
        }

        /// <summary>
        /// Unregisters the window for USB device notifications
        /// </summary>
        public static void UnregisterUsbDeviceNotification()
        {
            UnregisterDeviceNotification(notificationHandle);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotification(IntPtr recipient, IntPtr notificationFilter, int flags);

        [DllImport("user32.dll")]
        private static extern bool UnregisterDeviceNotification(IntPtr handle);

        [StructLayout(LayoutKind.Sequential)]
        private struct DevBroadcastDeviceinterface
        {
            internal int Size;
            internal int DeviceType;
            internal int Reserved;
            internal Guid ClassGuid;
            internal short Name;
        }
    }

    public partial class Form1 : Form
    {
		/// <summary>
		/// Class to keep track of string and color for lines in output window.
		/// </summary>
		private class Line
		{
			public string Str;
			public Color ForeColor;

			public Line(string str, Color color)
			{
				Str = str;
				ForeColor = color;
			}
		};

		ArrayList lines = new ArrayList();

        Font origFont;
        Font monoFont;

        String copyStr;
        bool bRec;
        bool bCliOn = true;

		public Form1()
        {
            InitializeComponent();

            splitContainer1.FixedPanel = FixedPanel.Panel1;
            splitContainer2.FixedPanel = FixedPanel.Panel2;

            AcceptButton = button5; //Send
            CancelButton = button4; //Close

			outputList_Initialize();

			Settings.Read();
            TopMost = Settings.Option.StayOnTop;

			// let form use multiple fonts
            origFont = Font;
            FontFamily ff = new FontFamily("Courier New");
            monoFont = new Font(ff, 8, FontStyle.Regular);
            Font = Settings.Option.MonoFont ? monoFont : origFont;

            // Register for usb device add/removal notification
            UsbNotification.RegisterUsbDeviceNotification(this.Handle);

            CommPort com = CommPort.Instance;
            com.StatusChanged += OnStatusChanged;
            com.DataReceived += OnDataReceived;
            com.Open();

            button6.Text = "Local";
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == UsbNotification.WmDevicechange)
            {
                CommPort com = CommPort.Instance;

                switch ((int)m.WParam)
                {
                    case UsbNotification.DbtDeviceremovecomplete:
                        com.UsbDeviceRemoved(); 
                        break;
                    case UsbNotification.DbtDevicearrival:
                        com.UsbDeviceAdded(); 
                        break;
                }
            }
        }


        // shutdown the worker thread when the form closes
        protected override void OnClosed(EventArgs e)
		{
			CommPort com = CommPort.Instance;
			com.Close();

			base.OnClosed(e);
		}

		/// <summary>
		/// output string to log file
		/// </summary>
		/// <param name="stringOut">string to output</param>
		public void logFile_writeLine(string stringOut)
		{
			if (Settings.Option.LogFileName != "")
			{
				Stream myStream = File.Open(Settings.Option.LogFileName,
					FileMode.Append, FileAccess.Write, FileShare.Read);
				if (myStream != null)
				{
					StreamWriter myWriter = new StreamWriter(myStream, Encoding.UTF8);
					myWriter.WriteLine(stringOut);
					myWriter.Close();
				}
			}
		}

		#region Output window

		string filterString = "";
		bool scrolling = true;
		Color receivedColor = Color.Green;
		Color sentColor = Color.Blue;

		/// <summary>
		/// context menu for the output window
		/// </summary>
		ContextMenu popUpMenu;

		/// <summary>
		/// check to see if filter matches string
		/// </summary>
		/// <param name="s">string to check</param>
		/// <returns>true if matches filter</returns>
		bool outputList_ApplyFilter(String s)
		{
			if (filterString == "")
			{
				return true;
			}
			else if (s == "")
			{
				return false;
			}
			else if (Settings.Option.FilterUseCase)
			{
				return (s.IndexOf(filterString) != -1);
			}
			else
			{
			    string upperString = s.ToUpper();
			    string upperFilter = filterString.ToUpper();
				return (upperString.IndexOf(upperFilter) != -1);
			}
		}

		/// <summary>
		/// clear the output window
		/// </summary>
		void outputList_ClearAll()
		{
			lines.Clear();
			partialLine = null;

			outputList.Items.Clear();
		}

		/// <summary>
		/// refresh the output window
		/// </summary>
		void outputList_Refresh()
		{
			outputList.BeginUpdate();
			outputList.Items.Clear();
			foreach (Line line in lines)
			{
				if (outputList_ApplyFilter(line.Str))
				{
					outputList.Items.Add(line);
				}
			}
			outputList.EndUpdate();
			outputList_Scroll();
		}

		/// <summary>
		/// add a new line to output window
		/// </summary>
		Line outputList_Add(string str, Color color)
		{
			Line newLine = new Line(str, color);
			lines.Add(newLine);

			if (outputList_ApplyFilter(newLine.Str))
			{
				outputList.Items.Add(newLine);
				outputList_Scroll();
			}

			return newLine;
		}

		/// <summary>
		/// Update a line in the output window.
		/// </summary>
		/// <param name="line">line to update</param>
		void outputList_Update(Line line)
		{
			// should we add to output?
			if (outputList_ApplyFilter(line.Str))
			{
				// is the line already displayed?
				bool found = false;
				for (int i = 0; i < outputList.Items.Count; ++i)
				{
					int index = (outputList.Items.Count - 1) - i;
					if (line == outputList.Items[index])
					{
						// is item visible?
						int itemsPerPage = (int)(outputList.Height / outputList.ItemHeight);
						if (index >= outputList.TopIndex &&
							index < (outputList.TopIndex + itemsPerPage))
						{
							// is there a way to refresh just one line
							// without redrawing the entire listbox?
							// changing the item value has no effect
							outputList.Refresh();
						}
						found = true;
						break;
					}
				}
				if (!found)
				{
					// not found, so add it
					outputList.Items.Add(line);
				}
			}
		}

		/// <summary>
		/// Initialize the output window
		/// </summary>
		private void outputList_Initialize()
		{
			// owner draw for listbox so we can add color
			outputList.DrawMode = DrawMode.OwnerDrawFixed;
			outputList.DrawItem += new DrawItemEventHandler(outputList_DrawItem);
			outputList.ClearSelected();

			// build the outputList context menu
			popUpMenu = new ContextMenu();
			popUpMenu.MenuItems.Add("&Copy", new EventHandler(outputList_Copy));
			popUpMenu.MenuItems[0].Visible = true;
			popUpMenu.MenuItems[0].Enabled = false;
			popUpMenu.MenuItems[0].Shortcut = Shortcut.CtrlC;
			popUpMenu.MenuItems[0].ShowShortcut = true;
			popUpMenu.MenuItems.Add("Copy All", new EventHandler(outputList_CopyAll));
			popUpMenu.MenuItems[1].Visible = true;
			popUpMenu.MenuItems.Add("Select &All", new EventHandler(outputList_SelectAll));
			popUpMenu.MenuItems[2].Visible = true;
			popUpMenu.MenuItems[2].Shortcut = Shortcut.CtrlA;
			popUpMenu.MenuItems[2].ShowShortcut = true;
			popUpMenu.MenuItems.Add("Clear Selected", new EventHandler(outputList_ClearSelected));
			popUpMenu.MenuItems[3].Visible = true;
			outputList.ContextMenu = popUpMenu;
		}

		/// <summary>
		/// draw item with color in output window
		/// </summary>
		void outputList_DrawItem(object sender, DrawItemEventArgs e)
		{
			e.DrawBackground();
			if (e.Index >= 0 && e.Index < outputList.Items.Count)
			{
				Line line = (Line)outputList.Items[e.Index];

				// if selected, make the text color readable
				Color color = line.ForeColor;
				if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
				{
					color = Color.Black;	// make it readable
				}

				e.Graphics.DrawString(line.Str, e.Font, new SolidBrush(color),
					e.Bounds, StringFormat.GenericDefault);
			}
			e.DrawFocusRectangle();
		}

		/// <summary>
		/// Scroll to bottom of output window
		/// </summary>
		void outputList_Scroll()
		{
			if (scrolling)
			{
				int itemsPerPage = (int)(outputList.Height / outputList.ItemHeight);
				outputList.TopIndex = outputList.Items.Count - itemsPerPage;
			}
		}

		/// <summary>
		/// Enable/Disable copy selection in output window
		/// </summary>
		private void outputList_SelectedIndexChanged(object sender, EventArgs e)
		{
			popUpMenu.MenuItems[0].Enabled = (outputList.SelectedItems.Count > 0);
		}

		/// <summary>
		/// copy selection in output window to clipboard
		/// </summary>
		private void outputList_Copy(object sender, EventArgs e)
		{
			int iCount = outputList.SelectedItems.Count;
			if (iCount > 0)
			{
				String[] source = new String[iCount];
				for (int i = 0; i < iCount; ++i)
				{
					source[i] = ((Line)outputList.SelectedItems[i]).Str;
				}

				String dest = String.Join("\r\n", source);
				Clipboard.SetText(dest);
			}
		}

		/// <summary>
		/// copy all lines in output window
		/// </summary>
		private void outputList_CopyAll(object sender, EventArgs e)
		{
			int iCount = outputList.Items.Count;
			if (iCount > 0)
			{
				String[] source = new String[iCount];
				for (int i = 0; i < iCount; ++i)
				{
					source[i] = ((Line)outputList.Items[i]).Str;
				}

				String dest = String.Join("\r\n", source);
				Clipboard.SetText(dest);
			}
		}

		/// <summary>
		/// select all lines in output window
		/// </summary>
		private void outputList_SelectAll(object sender, EventArgs e)
		{
			outputList.BeginUpdate();
			for (int i = 0; i < outputList.Items.Count; ++i)
			{
			    outputList.SetSelected(i, true);
			}
			outputList.EndUpdate();
		}

		/// <summary>
		/// clear selected in output window
		/// </summary>
		private void outputList_ClearSelected(object sender, EventArgs e)
		{
			outputList.ClearSelected();
			outputList.SelectedItem = -1;
		}

		#endregion

		#region Event handling - data received and status changed

		/// <summary>
		/// Prepare a string for output by converting non-printable characters.
		/// </summary>
		/// <param name="StringIn">input string to prepare.</param>
		/// <returns>output string.</returns>
		private String PrepareData(String StringIn)
		{
			// The names of the first 32 characters
			string[] charNames = { "NUL", "SOH", "STX", "ETX", "EOT",
				"ENQ", "ACK", "BEL", "BS", "TAB", "LF", "VT", "FF", "CR", "SO", "SI",
				"DLE", "DC1", "DC2", "DC3", "DC4", "NAK", "SYN", "ETB", "CAN", "EM", "SUB",
				"ESC", "FS", "GS", "RS", "US", "Space"};

			string StringOut = "";

			foreach (char c in StringIn)
            {
                if (Settings.Option.HexOutput)
                {
                    StringOut = StringOut + String.Format("{0:X2} ", (int)c);
                }
                else if (c < 32 && c != 9)
                {
                    StringOut = StringOut + "<" + charNames[c] + ">";

                    //Uglier "Termite" style
                    //StringOut = StringOut + String.Format("[{0:X2}]", (int)c);
                }
                else
                {
                    StringOut = StringOut + c;
                }
            }
			return StringOut;
		}

		/// <summary>
		/// Partial line for AddData().
		/// </summary>
		private Line partialLine = null;

		/// <summary>
		/// Add data to the output.
		/// </summary>
		/// <param name="StringIn"></param>
		/// <returns></returns>
		private Line AddData(String StringIn)
		{
			String StringOut = PrepareData(StringIn);

			// if we have a partial line, add to it.
			if (partialLine != null)
			{
				// tack it on
				partialLine.Str = partialLine.Str + StringOut;
				outputList_Update(partialLine);
				return partialLine;
			}

			return outputList_Add(StringOut, receivedColor);
		}

		// delegate used for Invoke
		internal delegate void StringDelegate(string data);

		/// <summary>
		/// Handle data received event from serial port.
		/// </summary>
		/// <param name="data">incoming data</param>
		public void OnDataReceived(string dataIn)
        {
            //Handle multi-threading
            if (InvokeRequired)
            {
				Invoke(new StringDelegate(OnDataReceived), new object[] { dataIn });
                return;
            }

			// pause scrolling to speed up output of multiple lines
			bool saveScrolling = scrolling;
			scrolling = false;

#if false
            // Extract +TEMP and +HUD strings and send to clipboard
            {
                int idx;
                copyStr += dataIn;

                if (!bRec && (idx = copyStr.IndexOf("+LSCAN:0,")) != -1)
                {
                    if ((idx = copyStr.IndexOf("\r\n", idx)) != -1)
                    {
                        copyStr = copyStr.Remove(0, idx + 2);
                        bRec = true;
                    }
                }

                if (bRec && (idx = copyStr.IndexOf("+LSCAN:1,")) != -1)
                {
                    Clipboard.Clear();
                    Clipboard.SetText(copyStr.Substring(0, idx));
                    bRec = false;
                }
            }
#endif

            // if we detect a line terminator, add line to output
            int index;
			while (dataIn.Length > 0 &&
				((index = dataIn.IndexOf("\r")) != -1 ||
				(index = dataIn.IndexOf("\n")) != -1))
            {
                String StringIn = dataIn.Substring(0, index++);
                if( dataIn.Substring(index,1).Equals("\n") )
                {
                    index++;
                }
                dataIn = dataIn.Remove(0, index);

                logFile_writeLine(AddData(StringIn).Str);
				partialLine = null;	// terminate partial line
            }

			// if we have data remaining, add a partial line
			if (dataIn.Length > 0)
			{
				partialLine = AddData(dataIn);
			}

			// restore scrolling
			scrolling = saveScrolling;
			outputList_Scroll();
		}

		/// <summary>
		/// Update the connection status
		/// </summary>
		public void OnStatusChanged(string status)
		{
			//Handle multi-threading
			if (InvokeRequired)
			{
				Invoke(new StringDelegate(OnStatusChanged), new object[] { status });
				return;
			}

			textBox1.Text = status;
		}

#endregion

#region User interaction

		/// <summary>
		/// toggle connection status
		/// </summary>
		private void textBox1_Click(object sender, MouseEventArgs e)
		{
			CommPort com = CommPort.Instance;
			if (com.IsOpen)
			{
				com.Close();
			}
			else
			{
				com.Open();
			}
			outputList.Focus();
		}

		/// <summary>
		/// Change filter
		/// </summary>
		private void textBox2_TextChanged(object sender, EventArgs e)
        {
            filterString = textBox2.Text;
			outputList_Refresh();
		}

		/// <summary>
		/// Show settings dialog
		/// </summary>
		private void button1_Click(object sender, EventArgs e)
		{
			TopMost = false;

			Form2 form2 = new Form2();
			form2.ShowDialog();

			TopMost = Settings.Option.StayOnTop;
			Font = Settings.Option.MonoFont ? monoFont : origFont;
		}

		/// <summary>
		/// Clear the output window
		/// </summary>
		private void button2_Click(object sender, EventArgs e)
		{
			outputList_ClearAll();
		}

		/// <summary>
		/// Show about dialog
		/// </summary>
		private void button3_Click(object sender, EventArgs e)
		{
			TopMost = false;

			AboutBox about = new AboutBox();
			about.ShowDialog();

			TopMost = Settings.Option.StayOnTop;
		}

		/// <summary>
		/// Close the application
		/// </summary>
		private void button4_Click(object sender, EventArgs e)
		{
			Close();
		}

        /// <summary>
        /// If character 0-9 a-f A-F, then return hex digit value ?
        /// </summary>
        private static int GetHexDigit(char c)
        {
            if ('0' <= c && c <= '9') return (c-'0');
            if ('a' <= c && c <= 'f') return (c-'a')+10;
            if ('A' <= c && c <= 'F') return (c-'A')+10;
            return 0;
        }

        /// <summary>
        /// Parse states for ConvertEscapeSequences()
        /// </summary>
        public enum Expecting : byte
        {
            ANY = 1,
            ESCAPED_CHAR,
            HEX_1ST_DIGIT,
            HEX_2ND_DIGIT
        };

        /// <summary>
        /// Convert escape sequences
        /// </summary>
        private string ConvertEscapeSequences(string s)
        {
            Expecting expecting = Expecting.ANY;

            int hexNum = 0;
            string outs = "";
            foreach (char c in s)
            {
                switch (expecting)
                {
                    case Expecting.ANY:
                        if (c == '\\')
                            expecting = Expecting.ESCAPED_CHAR;
                        else
                            outs += c;
                        break;
                    case Expecting.ESCAPED_CHAR:
                        if (c == 'x')
                        {
                            expecting = Expecting.HEX_1ST_DIGIT;
                        }
                        else
                        {
                            char c2 = c;
                            switch (c)
                            {
                                case 'n': c2 = '\n'; break;
                                case 'r': c2 = '\r'; break;
                                case 't': c2 = '\t'; break;
                            }
                            outs += c2;
                            expecting = Expecting.ANY;
                        }
                        break;
                    case Expecting.HEX_1ST_DIGIT:
                        hexNum = GetHexDigit(c)*16;
                        expecting = Expecting.HEX_2ND_DIGIT;
                        break;
                    case Expecting.HEX_2ND_DIGIT:
                        hexNum += GetHexDigit(c);
                        outs += (char)hexNum;
                        expecting = Expecting.ANY;
                        break;
                }
            }
            return outs;
        }

		/// <summary>
		/// Send command
		/// </summary>
		private void button5_Click(object sender, EventArgs e)
        {
            string command = comboBox1.Text;
            comboBox1.Items.Add(comboBox1.Text);
            //comboBox1.Text = "";
            comboBox1.Focus();

            if (command.Length > 0)
            {
                command = ConvertEscapeSequences(command);

                CommPort com = CommPort.Instance;
                com.Send(command);

                if (Settings.Option.LocalEcho)
                {
                    outputList_Add(command + "\n", sentColor);
                }
            }
        }

        /// <summary>
        /// send file to serial port
        /// </summary>
        private void button6_Click(object sender, EventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog();
#if false
            dialog.RestoreDirectory = false;
            dialog.Title = "Select a file";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                String text = System.IO.File.ReadAllText(dialog.FileName);

                CommPort com = CommPort.Instance;
                com.Send(text);

                if (Settings.Option.LocalEcho)
                {
                    outputList_Add("SendFile " + dialog.FileName + "," +
                        text.Length.ToString() + " byte(s)\n", sentColor);
                }
            } 
#endif
            CommPort com = CommPort.Instance;
            if( bCliOn )
            {
                com.ChangeGpio(0x2000, 0x0000);
                bCliOn = false;
                button6.Text = "Remote";
            }
            else
            {
                com.ChangeGpio(0x0000, 0x2000);
                bCliOn = true;
                button6.Text = "Local";
            }
        }

        /// <summary>
        /// toggle scrolling
        /// </summary>
        private void button7_Click(object sender, EventArgs e)
		{
			scrolling = !scrolling;
			outputList_Scroll();
		}

#endregion
	}
}
