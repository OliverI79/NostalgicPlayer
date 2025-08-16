﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
using System.IO;
using System.Runtime.CompilerServices;
using Polycode.NostalgicPlayer.Kit.C;
using Polycode.NostalgicPlayer.Ports.LibXmp.Containers;

namespace Polycode.NostalgicPlayer.Ports.LibXmp
{
	/// <summary>
	/// Helper methods to read data from a file
	/// </summary>
	internal static class DataIo
	{
		/********************************************************************/
		/// <summary>
		/// 
		/// </summary>
		/********************************************************************/
		public static uint8 Read8(Stream f, out int err)
		{
			try
			{
				c_int a = Read_Byte(f);

				err = 0;
				return (uint8)a;
			}
			catch (EndOfStreamException)
			{
				err = Constants.EOF;
				return 0xff;
			}
			catch (IOException ex)
			{
				err = ex.HResult;
				return 0xff;
			}
		}



		/********************************************************************/
		/// <summary>
		/// 
		/// </summary>
		/********************************************************************/
		public static int8 Read8S(Stream f, out int err)
		{
			try
			{
				c_int a = Read_Byte(f);

				err = 0;
				return (int8)a;
			}
			catch (EndOfStreamException)
			{
				err = Constants.EOF;
				return 0;
			}
			catch (IOException ex)
			{
				err = ex.HResult;
				return 0;
			}
		}



		/********************************************************************/
		/// <summary>
		/// 
		/// </summary>
		/********************************************************************/
		public static uint16 Read16L(Stream f, out int err)
		{
			try
			{
				c_int a = Read_Byte(f);
				c_int b = Read_Byte(f);

				err = 0;
				return (uint16)((b << 8) | a);
			}
			catch (EndOfStreamException)
			{
				err = Constants.EOF;
				return 0xffff;
			}
			catch (IOException ex)
			{
				err = ex.HResult;
				return 0xffff;
			}
		}



		/********************************************************************/
		/// <summary>
		/// 
		/// </summary>
		/********************************************************************/
		public static uint16 Read16B(Stream f, out int err)
		{
			try
			{
				c_int a = Read_Byte(f);
				c_int b = Read_Byte(f);

				err = 0;
				return (uint16)((a << 8) | b);
			}
			catch (EndOfStreamException)
			{
				err = Constants.EOF;
				return 0xffff;
			}
			catch (IOException ex)
			{
				err = ex.HResult;
				return 0xffff;
			}
		}



		/********************************************************************/
		/// <summary>
		/// 
		/// </summary>
		/********************************************************************/
		public static uint32 Read24L(Stream f, out int err)
		{
			try
			{
				c_int a = Read_Byte(f);
				c_int b = Read_Byte(f);
				c_int c = Read_Byte(f);

				err = 0;
				return (uint32)((c << 16) | (b << 8) | a);
			}
			catch (EndOfStreamException)
			{
				err = Constants.EOF;
				return 0xffffffff;
			}
			catch (IOException ex)
			{
				err = ex.HResult;
				return 0xffffffff;
			}
		}



		/********************************************************************/
		/// <summary>
		/// 
		/// </summary>
		/********************************************************************/
		public static uint32 Read24B(Stream f, out int err)
		{
			try
			{
				c_int a = Read_Byte(f);
				c_int b = Read_Byte(f);
				c_int c = Read_Byte(f);

				err = 0;
				return (uint32)((a << 16) | (b << 8) | c);
			}
			catch (EndOfStreamException)
			{
				err = Constants.EOF;
				return 0xffffffff;
			}
			catch (IOException ex)
			{
				err = ex.HResult;
				return 0xffffffff;
			}
		}



		/********************************************************************/
		/// <summary>
		/// 
		/// </summary>
		/********************************************************************/
		public static uint32 Read32L(Stream f, out int err)
		{
			try
			{
				c_int a = Read_Byte(f);
				c_int b = Read_Byte(f);
				c_int c = Read_Byte(f);
				c_int d = Read_Byte(f);

				err = 0;
				return (uint32)((d << 24) | (c << 16) | (b << 8) | a);
			}
			catch (EndOfStreamException)
			{
				err = Constants.EOF;
				return 0xffffffff;
			}
			catch (IOException ex)
			{
				err = ex.HResult;
				return 0xffffffff;
			}
		}



		/********************************************************************/
		/// <summary>
		/// 
		/// </summary>
		/********************************************************************/
		public static uint32 Read32B(Stream f, out int err)
		{
			try
			{
				c_int a = Read_Byte(f);
				c_int b = Read_Byte(f);
				c_int c = Read_Byte(f);
				c_int d = Read_Byte(f);

				err = 0;
				return (uint32)((a << 24) | (b << 16) | (c << 8) | d);
			}
			catch (EndOfStreamException)
			{
				err = Constants.EOF;
				return 0xffffffff;
			}
			catch (IOException ex)
			{
				err = ex.HResult;
				return 0xffffffff;
			}
		}



		/********************************************************************/
		/// <summary>
		/// 
		/// </summary>
		/********************************************************************/
		public static uint16 ReadMem16L(CPointer<uint8> m)
		{
			uint32 a = m[0];
			uint32 b = m[1];

			return (uint16)((b << 8) | a);
		}



		/********************************************************************/
		/// <summary>
		/// 
		/// </summary>
		/********************************************************************/
		public static uint16 ReadMem16B(CPointer<uint8> m)
		{
			uint32 a = m[0];
			uint32 b = m[1];

			return (uint16)((a << 8) | b);
		}



		/********************************************************************/
		/// <summary>
		/// 
		/// </summary>
		/********************************************************************/
		public static uint32 ReadMem24L(CPointer<uint8> m)
		{
			uint32 a = m[0];
			uint32 b = m[1];
			uint32 c = m[2];

			return (c << 16) | (b << 8) | a;
		}



		/********************************************************************/
		/// <summary>
		/// 
		/// </summary>
		/********************************************************************/
		public static uint32 ReadMem24B(CPointer<uint8> m)
		{
			uint32 a = m[0];
			uint32 b = m[1];
			uint32 c = m[2];

			return (a << 16) | (b << 8) | c;
		}



		/********************************************************************/
		/// <summary>
		/// 
		/// </summary>
		/********************************************************************/
		public static uint32 ReadMem32L(CPointer<uint8> m)
		{
			uint32 a = m[0];
			uint32 b = m[1];
			uint32 c = m[2];
			uint32 d = m[3];

			return (d << 24) | (c << 16) | (b << 8) | a;
		}



		/********************************************************************/
		/// <summary>
		/// 
		/// </summary>
		/********************************************************************/
		public static uint32 ReadMem32B(CPointer<uint8> m)
		{
			uint32 a = m[0];
			uint32 b = m[1];
			uint32 c = m[2];
			uint32 d = m[3];

			return (a << 24) | (b << 16) | (c << 8) | d;
		}

		#region Private methods
		/********************************************************************/
		/// <summary>
		/// 
		/// </summary>
		/********************************************************************/
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int Read_Byte(Stream f)
		{
			int x = f.ReadByte();
			if (x < 0)
				throw new EndOfStreamException();

			return x;
		}
		#endregion
	}
}
