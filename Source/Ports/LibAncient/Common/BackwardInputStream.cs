﻿/******************************************************************************/
/* This source, or parts thereof, may be used in any software as long the     */
/* license of NostalgicPlayer is keep. See the LICENSE file for more          */
/* information.                                                               */
/******************************************************************************/
using Polycode.NostalgicPlayer.Ports.LibAncient.Exceptions;
using Buffer = Polycode.NostalgicPlayer.Ports.LibAncient.Common.Buffers.Buffer;

namespace Polycode.NostalgicPlayer.Ports.LibAncient.Common
{
	/// <summary>
	/// Read from a buffer backwards
	/// </summary>
	internal class BackwardInputStream : IInputStream
	{
		private readonly Buffer buffer;

		private size_t currentOffset;
		private readonly size_t endOffset;

		/********************************************************************/
		/// <summary>
		/// Constructor
		/// </summary>
		/********************************************************************/
		public BackwardInputStream(Buffer buffer, size_t startOffset, size_t endOffset)
		{
			this.buffer = buffer;
			currentOffset = endOffset;
			this.endOffset = startOffset;

			if ((currentOffset < this.endOffset) || (currentOffset > buffer.Size()) || (this.endOffset > buffer.Size()))
				throw new DecompressionException();
		}

		#region IInputStream implementation
		/********************************************************************/
		/// <summary>
		/// Return the current position
		/// </summary>
		/********************************************************************/
		public size_t GetOffset()
		{
			return currentOffset;
		}



		/********************************************************************/
		/// <summary>
		/// Read a single byte
		/// </summary>
		/********************************************************************/
		public uint8_t ReadByte()
		{
			if (currentOffset <= endOffset)
				throw new DecompressionException();

			uint8_t ret = buffer[--currentOffset];

			return ret;
		}



		/********************************************************************/
		/// <summary>
		/// Read a 16-bit integer in big-endian format
		/// </summary>
		/********************************************************************/
		public uint16_t ReadBE16()
		{
			uint16_t b0 = ReadByte();
			uint16_t b1 = ReadByte();

			return (uint16_t)((b1 << 8) | b0);
		}



		/********************************************************************/
		/// <summary>
		/// Read a 32-bit integer in big-endian format
		/// </summary>
		/********************************************************************/
		public uint32_t ReadBE32()
		{
			uint32_t b0 = ReadByte();
			uint32_t b1 = ReadByte();
			uint32_t b2 = ReadByte();
			uint32_t b3 = ReadByte();

			return (b3 << 24) | (b2 << 16) | (b1 << 8) | b0;
		}



		/********************************************************************/
		/// <summary>
		/// Read a 16-bit integer in little-endian format
		/// </summary>
		/********************************************************************/
		public uint16_t ReadLE16()
		{
			uint16_t b0 = ReadByte();
			uint16_t b1 = ReadByte();

			return (uint16_t)((b0 << 8) | b1);
		}



		/********************************************************************/
		/// <summary>
		/// Read a 32-bit integer in little-endian format
		/// </summary>
		/********************************************************************/
		public uint32_t ReadLE32()
		{
			uint32_t b0 = ReadByte();
			uint32_t b1 = ReadByte();
			uint32_t b2 = ReadByte();
			uint32_t b3 = ReadByte();

			return (b0 << 24) | (b1 << 16) | (b2 << 8) | b3;
		}
		#endregion
	}
}
