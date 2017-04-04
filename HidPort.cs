using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Runtime.InteropServices;
using SLABHIDTOUART_DLL;

namespace Virtual.CommDriver
{
    public sealed class HidPort
    {
        public const UInt16 VID = 0x10c4;
        public const UInt16 PID = 0xea80;

        public string PortName { get; set; }
        public System.IO.Ports.Parity Parity { get; set; }
        public System.IO.Ports.StopBits StopBits { get; set; }
        public System.IO.Ports.Handshake Handshake { get; set; }
        public int BaudRate { get; set; }
        public int DataBits { get; set; }
        public bool IsOpen { get; private set; }
        public int ReadBufferSize
        {
            get
            {
                ushort txFifoSz = 0, rxFifoSz  = 0;
                byte errStatus = 0, lnBrkStatus = 0;
                SLABHIDTOUART.HidUart_GetUartStatus(mHidUart, ref txFifoSz, ref rxFifoSz, ref errStatus, ref lnBrkStatus);
                return (int)rxFifoSz;
            }

        }
        public int ReadTimeout { get; set; }
        public int WriteBufferSize
        {
            get
            {
                ushort txFifoSz = 0, rxFifoSz = 0;
                byte errStatus = 0, lnBrkStatus = 0;
                SLABHIDTOUART.HidUart_GetUartStatus(mHidUart, ref txFifoSz, ref rxFifoSz, ref errStatus, ref lnBrkStatus);
                return (int)txFifoSz;
            }
        }
        public int WriteTimeout { get; set; }
        public bool isRemoved
        {
            get
            {
                if (mHidUart == IntPtr.Zero)
                {
                    return false;
                }

                ushort vid = 0, pid = 0, relNum = 0;
                int status = SLABHIDTOUART.HidUart_GetOpenedAttributes(mHidUart, ref vid, ref pid, ref relNum);
                if (status != SLABHIDTOUART.HID_UART_SUCCESS)
                {
                    return true;
                }

                return false;
            }
        }

        private IntPtr mHidUart = IntPtr.Zero;

        public HidPort()
        {
            PortName = "00000001";
            StopBits = StopBits.One;
            DataBits = 8;
            Handshake = Handshake.None;
            Parity = Parity.None;
            BaudRate = 115200;
        }

        //
        // Summary:
        //     Gets an array of serial port names for the current computer.
        //
        // Returns:
        //     An array of serial port names for the current computer.
        //
        // Exceptions:
        //   T:System.ComponentModel.Win32Exception:
        //     The serial port names could not be queried.
        public static string[] GetPortNames()
        {
            uint numDevices = 0;
            List<string> strList = new List<string>();

            if (SLABHIDTOUART.HidUart_GetNumDevices(ref numDevices, VID, PID) == SLABHIDTOUART.HID_UART_SUCCESS)
            {
                StringBuilder deviceString = new StringBuilder(512);
                for (uint i = 0; i < numDevices; i++)
                {
                    if (SLABHIDTOUART.HidUart_GetString(i, VID, PID, deviceString, SLABHIDTOUART.HID_UART_GET_SERIAL_STR) == SLABHIDTOUART.HID_UART_SUCCESS)
                    {
                        strList.Add(deviceString.ToString());
                    }
                }
            }

            return strList.ToArray();
        }

        //
        // Summary:
        //     Closes the port connection, sets the System.IO.Ports.SerialPort.IsOpen property
        //     to false, and disposes of the internal System.IO.Stream object.
        //
        // Exceptions:
        //   T:System.IO.IOException:
        //     The port is in an invalid state.- or -An attempt to set the state of the underlying
        //     port failed. For example, the parameters passed from this System.IO.Ports.SerialPort
        //     object were invalid.
        public void Close()
        {
            if( mHidUart != IntPtr.Zero)
            {
                if (SLABHIDTOUART.HidUart_Close(mHidUart) != SLABHIDTOUART.HID_UART_SUCCESS)
                {
                    throw new System.IO.IOException();
                }
            }

            IsOpen = false;
            mHidUart = IntPtr.Zero;
        }

        //
        // Summary:
        //     Discards data from the serial driver's receive buffer.
        //
        // Exceptions:
        //   T:System.IO.IOException:
        //     The port is in an invalid state. - or -An attempt to set the state of the underlying
        //     port failed. For example, the parameters passed from this System.IO.Ports.SerialPort
        //     object were invalid.
        //
        //   T:System.InvalidOperationException:
        //     The stream is closed. This can occur because the System.IO.Ports.SerialPort.Open
        //     method has not been called or the System.IO.Ports.SerialPort.Close method has
        //     been called.
        public void DiscardInBuffer()
        {

        }

        //
        // Summary:
        //     Discards data from the serial driver's transmit buffer.
        //
        // Exceptions:
        //   T:System.IO.IOException:
        //     The port is in an invalid state. - or - An attempt to set the state of the underlying
        //     port failed. For example, the parameters passed from this System.IO.Ports.SerialPort
        //     object were invalid.
        //
        //   T:System.InvalidOperationException:
        //     The stream is closed. This can occur because the System.IO.Ports.SerialPort.Open
        //     method has not been called or the System.IO.Ports.SerialPort.Close method has
        //     been called.
        public void DiscardOutBuffer()
        {

        }

        //
        // Summary:
        //     Opens a new serial port connection.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The specified port is open.
        //
        //   T:System.ArgumentOutOfRangeException:
        //     One or more of the properties for this instance are invalid. For example, the
        //     System.IO.Ports.SerialPort.Parity, System.IO.Ports.SerialPort.DataBits, or System.IO.Ports.SerialPort.Handshake
        //     properties are not valid values; the System.IO.Ports.SerialPort.BaudRate is less
        //     than or equal to zero; the System.IO.Ports.SerialPort.ReadTimeout or System.IO.Ports.SerialPort.WriteTimeout
        //     property is less than zero and is not System.IO.Ports.SerialPort.InfiniteTimeout.
        //
        //   T:System.ArgumentException:
        //     The port name does not begin with "COM". - or -The file type of the port is not
        //     supported.
        //
        //   T:System.IO.IOException:
        //     The port is in an invalid state. - or - An attempt to set the state of the underlying
        //     port failed. For example, the parameters passed from this System.IO.Ports.SerialPort
        //     object were invalid.
        //
        //   T:System.UnauthorizedAccessException:
        //     Access is denied to the port.
        public void Open()
        {
            uint numDevices = 0;
            if (SLABHIDTOUART.HidUart_GetNumDevices(ref numDevices, VID, PID) == SLABHIDTOUART.HID_UART_SUCCESS)
            {
                for (UInt32 i = 0; i < numDevices; i++)
                {
                    StringBuilder deviceString = new StringBuilder(512);

                    // Search through all HID devices for a matching serial string
                    if (SLABHIDTOUART.HidUart_GetString(i, VID, PID, deviceString, SLABHIDTOUART.HID_UART_GET_SERIAL_STR) == SLABHIDTOUART.HID_UART_SUCCESS)
                    {
                        // Found a matching device
                        if (PortName == deviceString.ToString())
                        {
                            int status;

                            // Open the device
                            {
                                status = SLABHIDTOUART.HidUart_Open(ref mHidUart, i, VID, PID);
                                if (status != SLABHIDTOUART.HID_UART_SUCCESS)
                                {
                                    throw new System.UnauthorizedAccessException();
                                }
                            }

                            // Configure UART speed
                            {
                                status = SLABHIDTOUART.HidUart_SetUartConfig(mHidUart, 
                                                (uint)BaudRate, 
                                                (byte)(DataBits - 5), 
                                                (byte)Parity, 
                                                (byte)StopBits, 
                                                (byte)Handshake);
                                if (status != SLABHIDTOUART.HID_UART_SUCCESS)
                                {
                                    throw new System.ArgumentOutOfRangeException();
                                }

                                uint vBaudRate = 0;
                                byte vDataBits = 0;
                                byte vParity = 0;
                                byte vStopBits = 0;
                                byte vFlowControl = 0;

                                status = SLABHIDTOUART.HidUart_GetUartConfig(mHidUart,
                                                ref vBaudRate,
                                                ref vDataBits,
                                                ref vParity,
                                                ref vStopBits,
                                                ref vFlowControl);
                                if (status != SLABHIDTOUART.HID_UART_SUCCESS)
                                {
                                    if (vBaudRate != BaudRate ||
                                        vDataBits != DataBits ||
                                        vParity != (byte)Parity ||
                                        vStopBits != (byte)StopBits ||
                                        vFlowControl != (byte)Handshake)
                                    {
                                        throw new System.ArgumentOutOfRangeException();
                                    }
                                }
                            }

                            // Configured the UART read/write timeout
                            {
                                status = SLABHIDTOUART.HidUart_SetTimeouts(mHidUart, (uint)ReadTimeout, (uint)WriteTimeout);
                                if (status != SLABHIDTOUART.HID_UART_SUCCESS)
                                {
                                    throw new System.ArgumentOutOfRangeException();
                                }
                            }

                            IsOpen = true;
                            return;
                        }
                    }
                }
            }
            throw new System.IO.IOException();
        }

        //
        // Summary:
        //     Reads a number of bytes from the System.IO.Ports.SerialPort input buffer and
        //     writes those bytes into a byte array at the specified offset.
        //
        // Parameters:
        //   buffer:
        //     The byte array to write the input to.
        //
        //   offset:
        //     The offset in the buffer array to begin writing.
        //
        //   count:
        //     The number of bytes to read.
        //
        // Returns:
        //     The number of bytes read.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The buffer passed is null.
        //
        //   T:System.InvalidOperationException:
        //     The specified port is not open.
        //
        //   T:System.ArgumentOutOfRangeException:
        //     The offset or count parameters are outside a valid region of the buffer being
        //     passed. Either offset or count is less than zero.
        //
        //   T:System.ArgumentException:
        //     offset plus count is greater than the length of the buffer.
        //
        //   T:System.TimeoutException:
        //     No bytes were available to read.
        public int Read(byte[] buffer, int offset, int count)
        {
            if (mHidUart == IntPtr.Zero)
            {
                throw new System.InvalidOperationException();
            }

            if (buffer == null)
            {
                throw new System.ArgumentNullException();
            }

            if (buffer.Length < offset + count)
            {
                throw new System.ArgumentOutOfRangeException();
            }

            uint byteRead = 0;
            SLABHIDTOUART.HidUart_Read(mHidUart, buffer, (uint)count, ref byteRead);

            return (int)byteRead;
        }

        //
        // Summary:
        //     Writes the specified string to the serial port.
        //
        // Parameters:
        //   text:
        //     The string for output.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The specified port is not open.
        //
        //   T:System.ArgumentNullException:
        //     str is null.
        //
        //   T:System.ServiceProcess.TimeoutException:
        //     The operation did not complete before the time-out period ended.
        public void Write(string text)
        {
            if (mHidUart == IntPtr.Zero)
            {
                throw new System.InvalidOperationException();
            }

            byte[] bytes = Encoding.UTF8.GetBytes(text);
            uint bytesWritten = 0;
            int status = SLABHIDTOUART.HidUart_Write(mHidUart, bytes, (uint)bytes.Length, ref bytesWritten);
            if( status == SLABHIDTOUART.HID_UART_WRITE_TIMED_OUT )
            {
                throw new System.TimeoutException();
            }
            else if( status != SLABHIDTOUART.HID_UART_SUCCESS )
            {
                throw new System.IO.IOException();
            }
        }

        //
        // Summary:
        //     Writes a specified number of bytes to the serial port using data from a buffer.
        //
        // Parameters:
        //   buffer:
        //     The byte array that contains the data to write to the port.
        //
        //   offset:
        //     The zero-based byte offset in the buffer parameter at which to begin copying
        //     bytes to the port.
        //
        //   count:
        //     The number of bytes to write.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The buffer passed is null.
        //
        //   T:System.InvalidOperationException:
        //     The specified port is not open.
        //
        //   T:System.ArgumentOutOfRangeException:
        //     The offset or count parameters are outside a valid region of the buffer being
        //     passed. Either offset or count is less than zero.
        //
        //   T:System.ArgumentException:
        //     offset plus count is greater than the length of the buffer.
        //
        //   T:System.ServiceProcess.TimeoutException:
        //     The operation did not complete before the time-out period ended.
        public void Write(byte[] buffer, int offset, int count)
        {
            if (mHidUart == IntPtr.Zero)
            {
                throw new System.InvalidOperationException();
            }

            if (buffer == null)
            {
                throw new System.ArgumentNullException();
            }

            if (buffer.Length >= offset + count)
            {
                throw new System.ArgumentOutOfRangeException();
            }

            uint bytesWritten = 0;
            byte[] bytes = new byte[count];
            Buffer.BlockCopy(buffer, offset, bytes, 0, count);
            int status = SLABHIDTOUART.HidUart_Write(mHidUart, bytes, (uint)count, ref bytesWritten);
            if (status == SLABHIDTOUART.HID_UART_WRITE_TIMED_OUT)
            {
                throw new System.TimeoutException();
            }
            else if (status != SLABHIDTOUART.HID_UART_SUCCESS)
            {
                throw new System.IO.IOException();
            }
        }

        public void ChangeGpio(UInt16 onMask, UInt16 offMask)
        {
            if (mHidUart == IntPtr.Zero)
            {
                throw new System.InvalidOperationException();
            }

            UInt16 latch = 0 ;

            SLABHIDTOUART.HidUart_ReadLatch(mHidUart, ref latch);

            latch &= (UInt16)~offMask;
            latch |= onMask;

            SLABHIDTOUART.HidUart_WriteLatch(mHidUart, latch, (UInt16)(onMask | offMask));
        }
    }
}
