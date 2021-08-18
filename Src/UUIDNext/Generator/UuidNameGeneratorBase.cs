﻿using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace UUIDNext.Generator
{
    public abstract class UuidNameGeneratorBase : UuidGeneratorBase
    {
        protected abstract ThreadLocal<HashAlgorithm> HashAlgorithm { get; }

        public Guid New(Guid namespaceId, string name)
        {
            var bytes = GetUuidBytes(namespaceId, name);
            return CreateGuidFromBytes(bytes);
        }

        private Span<byte> GetUuidBytes(Guid namespaceId, string name)
        {
            //Convert the name to a canonical sequence of octets (as defined by the standards or conventions of its name space);
            var utf8NameByteCount = Encoding.UTF8.GetByteCount(name);
            Span<byte> utf8NameBytes = (utf8NameByteCount > 256) ? new byte[utf8NameByteCount] : stackalloc byte[utf8NameByteCount];
            Encoding.UTF8.GetBytes(name, utf8NameBytes);

            //put the name space ID in network byte order.
            Span<byte> namespaceBytes = stackalloc byte[16];
            namespaceId.TryWriteBytes(namespaceBytes);
            SwitchByteOrderIfNeeded(namespaceBytes);

            //Compute the hash of the name space ID concatenated with the name.
            int bytesToHashCount = namespaceBytes.Length + utf8NameBytes.Length;
            Span<byte> bytesToHash = (utf8NameByteCount > 256) ? new byte[bytesToHashCount] : stackalloc byte[bytesToHashCount];
            namespaceBytes.CopyTo(bytesToHash);
            utf8NameBytes.CopyTo(bytesToHash[namespaceBytes.Length..]);

            Span<byte> hash = new byte[HashAlgorithm.Value.HashSize / 8];
            HashAlgorithm.Value.TryComputeHash(bytesToHash, hash, out var _);
            SwitchByteOrderIfNeeded(hash);
            return hash[0..16];
        }

        private static void SwitchByteOrderIfNeeded(Span<byte> guidByteArray)
        {
            if (!BitConverter.IsLittleEndian)
            {
                // On Big Endian architecture everything is in network byte order so we don't need to switch
                return;
            }

            Permut(guidByteArray, 0, 3);
            Permut(guidByteArray, 1, 2);
            Permut(guidByteArray, 5, 4);
            Permut(guidByteArray, 6, 7);

            static void Permut(Span<byte> array, int indexSource, int indexDest)
            {
                var temp = array[indexDest];
                array[indexDest] = array[indexSource];
                array[indexSource] = temp;
            }
        }
    }
}