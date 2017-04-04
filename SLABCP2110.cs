using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace SLABCP2110_DLL
{
    public class SLABCP2110
    {
        /////////////////////////////////////////////////////////////////////////////
        // SLABCP2110.h
        // For SLABHIDtoUART.dll version 2.0
        // and Silicon Labs CP2110 HID to UART
        /////////////////////////////////////////////////////////////////////////////

        /////////////////////////////////////////////////////////////////////////////
        // Includes
        /////////////////////////////////////////////////////////////////////////////

        /////////////////////////////////////////////////////////////////////////////
        // Pin Definitions
        /////////////////////////////////////////////////////////////////////////////

        // Pin Config Mode Array Indices
        public const int CP2110_INDEX_GPIO_0_CLK = 0;
        public const int CP2110_INDEX_GPIO_1_RTS = 1;
        public const int CP2110_INDEX_GPIO_2_CTS = 2;
        public const int CP2110_INDEX_GPIO_3_RS485 = 3;
        public const int CP2110_INDEX_GPIO_4_TX_TOGGLE = 4;
        public const int CP2110_INDEX_GPIO_5_RX_TOGGLE = 5;
        public const int CP2110_INDEX_GPIO_6 = 6;
        public const int CP2110_INDEX_GPIO_7 = 7;
        public const int CP2110_INDEX_GPIO_8 = 8;
        public const int CP2110_INDEX_GPIO_9 = 9;
        public const int CP2110_INDEX_TX = 10;
        public const int CP2110_INDEX_SUSPEND = 11;
        public const int CP2110_INDEX_SUSPEND_BAR = 12;

        // Pin Bitmasks
        public const ushort CP2110_MASK_GPIO_0_CLK = 0x0001;
        public const ushort CP2110_MASK_GPIO_1_RTS = 0x0002;
        public const ushort CP2110_MASK_GPIO_2_CTS = 0x0004;
        public const ushort CP2110_MASK_GPIO_3_RS485 = 0x0008;
        public const ushort CP2110_MASK_TX = 0x0010;
        public const ushort CP2110_MASK_RX = 0x0020;
        public const ushort CP2110_MASK_GPIO_4_TX_TOGGLE = 0x0040;
        public const ushort CP2110_MASK_GPIO_5_RX_TOGGLE = 0x0080;
        public const ushort CP2110_MASK_SUSPEND_BAR = 0x0100;
        // NA
        public const ushort CP2110_MASK_GPIO_6 = 0x0400;
        public const ushort CP2110_MASK_GPIO_7 = 0x0800;
        public const ushort CP2110_MASK_GPIO_8 = 0x1000;
        public const ushort CP2110_MASK_GPIO_9 = 0x2000;
        public const ushort CP2110_MASK_SUSPEND = 0x4000;

        /////////////////////////////////////////////////////////////////////////////
        // Exported Library Functions
        /////////////////////////////////////////////////////////////////////////////

        // HidUart_SetPinConfig
		[DllImport("SLABHIDtoUART.dll")]
        public static extern int HidUart_SetPinConfig(IntPtr device, byte[] pinConfig, bool useSuspendValues, ushort suspendValue, ushort suspendMode, byte rs485Level, byte clkDiv);

        // HidUart_GetPinConfig
		[DllImport("SLABHIDtoUART.dll")]
        public static extern int HidUart_GetPinConfig(IntPtr device, byte[] pinConfig, ref bool useSuspendValues, ref ushort suspendValue, ref ushort suspendMode, ref byte rs485Level, ref byte clkDiv);
    }
}
