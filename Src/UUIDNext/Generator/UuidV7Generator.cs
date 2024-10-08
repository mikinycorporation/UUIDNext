﻿using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using UUIDNext.Tools;

namespace UUIDNext.Generator
{
    /// <summary>
    /// Generate a UUID version 7 based on RFC 9562
    /// </summary>
    public class UuidV7Generator : UuidTimestampGeneratorBase
    {
        protected override byte Version => 7;

        protected override int SequenceBitSize => 12;

        protected override Guid New(DateTime date)
        {
            /* We implement the first example given in section 4.4.4.1 of the RFC
              0                   1                   2                   3
              0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
             +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
             |                           unix_ts_ms                          |
             +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
             |          unix_ts_ms           |  ver  |       rand_a          |
             +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
             |var|                        rand_b                             |
             +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
             |                            rand_b                             |
             +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
             */

            // Extra 2 bytes in front to prepend timestamp data.
            Span<byte> buffer = stackalloc byte[18];

            // Offset to the bytes that are used in UUIDv7.
            var bytes = buffer.Slice(2);

            long timestampInMs = ((DateTimeOffset)date).ToUnixTimeMilliseconds();

            SetSequence(bytes.Slice(6,2), ref timestampInMs);
            SetTimestamp(buffer.Slice(0, 8), timestampInMs);
            RandomNumberGeneratorPolyfill.Fill(bytes.Slice(8, 8));

            return CreateGuidFromBigEndianBytes(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetTimestamp(Span<byte> bytes, long timestampInMs)
        {
            BinaryPrimitives.TryWriteInt64BigEndian(bytes, timestampInMs);
        }

        [Obsolete("Use UuidDecoder.DecodeUuidV7 instead. This function will be removed in the next version")]
        public static (long timestampMs, short sequence) Decode(Guid guid) => UuidDecoder.DecodeUuidV7(guid);
    }
}
