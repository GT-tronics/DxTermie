using System;
using System.IO;
using System.IO.Ports;
using System.Collections;
using System.Threading;
using Virtual.CommDriver;

namespace DxTermie
{
    /// <summary> CommPort class creates a singleton instance
    /// of SerialPort (System.IO.Ports) </summary>
    /// <remarks> When ready, you open the port.
    ///   <code>
    ///   CommPort com = CommPort.Instance;
    ///   com.StatusChanged += OnStatusChanged;
    ///   com.DataReceived += OnDataReceived;
    ///   com.Open();
    ///   </code>
    ///   Notice that delegates are used to handle status and data events.
    ///   When settings are changed, you close and reopen the port.
    ///   <code>
    ///   CommPort com = CommPort.Instance;
    ///   com.Close();
    ///   com.PortName = "COM4";
    ///   com.Open();
    ///   </code>
    /// </remarks>
	public sealed class CommPort
    {
        HidPort _serialPort;
		Thread _readThread;
		volatile bool _keepReading;

        //begin Singleton pattern
        static readonly CommPort instance = new CommPort();

		// Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static CommPort()
        {
        }

        CommPort()
        {
			_serialPort = new HidPort();
			_readThread = null;
			_keepReading = false;
		}

		public static CommPort Instance
        {
            get
            {
                return instance;
            }
        }
        //end Singleton pattern

		//begin Observer pattern
        public delegate void EventHandler(string param);
        public EventHandler StatusChanged;
        public EventHandler DataReceived;
        //end Observer pattern

        public void UsbDeviceRemoved()
        {
            if( _serialPort.IsOpen && _serialPort.isRemoved )
            {
                Close();
            }
        }

        public void UsbDeviceAdded()
        {
            if( !_serialPort.IsOpen )
            {
                Open();
            }
        }

        private void StartReading()
		{
			if (!_keepReading)
			{
				_keepReading = true;
				_readThread = new Thread(ReadPort);
				_readThread.Start();
			}
		}

		private void StopReading()
		{
			if (_keepReading)
			{
				_keepReading = false;
				_readThread.Join();	//block until exits
				_readThread = null;
			}
		}

		/// <summary> Get the data and pass it on. </summary>
		private void ReadPort()
		{
			while (_keepReading)
			{
				if (_serialPort.IsOpen)
				{
					byte[] readBuffer = new byte[32];
					try
					{
						// If there are bytes available on the serial port,
						// Read returns up to "count" bytes, but will not block (wait)
						// for the remaining bytes. If there are no bytes available
						// on the serial port, Read will block until at least one byte
						// is available on the port, up until the ReadTimeout milliseconds
						// have elapsed, at which time a TimeoutException will be thrown.
						int count = _serialPort.Read(readBuffer, 0, readBuffer.Length);
                        if( count > 0 )
                        {
                            String SerialIn = System.Text.Encoding.ASCII.GetString(readBuffer, 0, count);
                            DataReceived(SerialIn);
                        }
                    }
					catch (TimeoutException) { }
                    catch (Exception ex)
                    {
                        //Close();
                    }
                }
				else
				{
					TimeSpan waitTime = new TimeSpan(0, 0, 0, 0, 250);
					Thread.Sleep(waitTime);
				}
			}
		}

		/// <summary> Open the serial port with current settings. </summary>
        public void Open()
        {
			Close();

            try
            {
                _serialPort.PortName = Settings.Port.PortName;
                _serialPort.BaudRate = Settings.Port.BaudRate;
                _serialPort.Parity = Settings.Port.Parity;
                _serialPort.DataBits = Settings.Port.DataBits;
                _serialPort.StopBits = Settings.Port.StopBits;
                _serialPort.Handshake = Settings.Port.Handshake;

				// Set the read/write timeouts
				_serialPort.ReadTimeout = 50;
				_serialPort.WriteTimeout = 50;

				_serialPort.Open();
				StartReading();
			}
            catch (IOException)
            {
                StatusChanged(String.Format("{0} does not exist", Settings.Port.PortName));
            }
            catch (UnauthorizedAccessException)
            {
                StatusChanged(String.Format("{0} already in use", Settings.Port.PortName));
            }
            catch (Exception ex)
            {
                StatusChanged(String.Format("{0}", ex.ToString()));
            }

            // Update the status
            if (_serialPort.IsOpen)
            {
                string p = _serialPort.Parity.ToString().Substring(0, 1);   //First char
                string h = _serialPort.Handshake.ToString();
                if (_serialPort.Handshake == Handshake.None)
                    h = "no handshake"; // more descriptive than "None"

                StatusChanged(String.Format("{0}: {1} bps, {2}{3}{4}, {5}",
                    _serialPort.PortName, _serialPort.BaudRate,
                    _serialPort.DataBits, p, (int)_serialPort.StopBits, h));
            }
            else
            {
                StatusChanged(String.Format("{0} not connected", Settings.Port.PortName));
            }
        }

        /// <summary> Close the serial port. </summary>
        public void Close()
        {
			StopReading();
			_serialPort.Close();
            StatusChanged("connection closed");
        }

        /// <summary> Get the status of the serial port. </summary>
        public bool IsOpen
        {
            get
            {
                return _serialPort.IsOpen;
            }
        }

        /// <summary> Get a list of the available ports. Already opened ports
        /// are not returend. </summary>
        public string[] GetAvailablePorts()
        {
            return HidPort.GetPortNames();
        }

        /// <summary>Send data to the serial port after appending line ending. </summary>
        /// <param name="data">An string containing the data to send. </param>
        public void Send(string data)
        {
            if (IsOpen)
            {
                string lineEnding = "";
                switch (Settings.Option.AppendToSend)
                {
                    case Settings.Option.AppendType.AppendCR:
                        lineEnding = "\r"; break;
                    case Settings.Option.AppendType.AppendLF:
                        lineEnding = "\n"; break;
                    case Settings.Option.AppendType.AppendCRLF:
                        lineEnding = "\r\n"; break;
                }

                try
                {
                    _serialPort.Write(data + lineEnding);
                }
                catch(IOException)
                {
                    Close();
                }

            }
        }

        public void ChangeGpio(UInt16 onMask, UInt16 offMask)
        {
            if( IsOpen )
            {
                _serialPort.ChangeGpio(onMask, offMask);
            }
        }
    }
}