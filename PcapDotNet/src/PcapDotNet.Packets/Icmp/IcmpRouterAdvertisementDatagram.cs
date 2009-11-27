using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PcapDotNet.Packets.IpV4;

namespace PcapDotNet.Packets.Icmp
{
    public class IcmpRouterAdvertisementEntry
    {
        public IcmpRouterAdvertisementEntry(IpV4Address routerAddress, int routerAddressPreference)
        {
            _routerAddress = routerAddress;
            _routerAddressPreference = routerAddressPreference;
        }

        public IpV4Address RouterAddress
        {
            get { return _routerAddress;}
        }

        public int RouterAddressPreference
        {
            get {return _routerAddressPreference; }
        }

        public bool Equals(IcmpRouterAdvertisementEntry other)
        {
            return other != null &&
                   RouterAddress == other.RouterAddress &&
                   RouterAddressPreference == other.RouterAddressPreference;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IcmpRouterAdvertisementEntry);
        }

        private readonly IpV4Address _routerAddress;
        private readonly int _routerAddressPreference;
    }

    /// <summary>
    /// RFC 1256.
    /// <pre>
    /// +-----+-----------+-----------------+----------+
    /// | Bit | 0-7       | 8-15            | 16-31    |
    /// +-----+-----------+-----------------+----------+
    /// | 0   | Num Addrs | Addr Entry Size | Lifetime |
    /// +-----+-----------+-----------------+----------+
    /// | 32  | Router Address[1]                      |
    /// +-----+----------------------------------------+
    /// | 64  | Preference Level[1]                    |
    /// +-----+----------------------------------------+
    /// | 96  | Router Address[2]                      |
    /// +-----+----------------------------------------+
    /// | 128 | Preference Level[2]                    |
    /// +-----+----------------------------------------+
    /// |  .  |                   .                    |
    /// |  .  |                   .                    |
    /// |  .  |                   .                    |
    /// </pre>
    /// </summary>
    public class IcmpRouterAdvertisementDatagram : IcmpTypedDatagram
    {
        public const int DefaultAddressEntrySize = 2;

        private class Offset
        {
            public const int NumAddresses = 0;
            public const int AddressEntrySize = 1;
            public const int Lifetime = 2;
            public const int Addresses = 4;
        }

        /// <summary>
        /// The number of router addresses advertised in this message.
        /// </summary>
        public byte NumAddresses
        {
            get { return this[Offset.NumAddresses]; }
        }

        /// <summary>
        /// The number of 32-bit words of information per each router address (2, in the version of the protocol described here).
        /// </summary>
        public byte AddressEntrySize
        {
            get { return this[Offset.AddressEntrySize]; }
        }

        /// <summary>
        /// The maximum number of seconds that the router addresses may be considered valid.
        /// </summary>
        public ushort LifetimeSeconds
        {
            get { return ReadUShort(Offset.Lifetime, Endianity.Big); }
        }

        /// <summary>
        /// The maximum time that the router addresses may be considered valid.
        /// </summary>
        public TimeSpan Lifetime
        {
            get { return TimeSpan.FromSeconds(LifetimeSeconds); }
        }

        /// <summary>
        /// The pairs of sending router's IP address(es) on the interface from which this message is sent
        /// and the preferability of each Router Address[i] as a default router address, relative to other router addresses on the same subnet.
        /// A signed, twos-complement value; higher values mean more preferable.
        /// </summary>
        public ReadOnlyCollection<IcmpRouterAdvertisementEntry> Entries
        {
            get
            {
                if (_entries == null)
                {
                    IcmpRouterAdvertisementEntry[] entries = new IcmpRouterAdvertisementEntry[NumAddresses];
                    int currentOffset = Offset.Addresses;
                    for (int i = 0; i != entries.Length && currentOffset + IpV4Address.SizeOf <= Length; ++i)
                    {
                        entries[i] = new IcmpRouterAdvertisementEntry(ReadIpV4Address(currentOffset, Endianity.Big),
                                                                      ReadInt(currentOffset + IpV4Address.SizeOf, Endianity.Big));
                        currentOffset += AddressEntrySize * IpV4Address.SizeOf;
                    }
                    _entries = new ReadOnlyCollection<IcmpRouterAdvertisementEntry>(entries);
                }

                return _entries;
            }
        }

        internal IcmpRouterAdvertisementDatagram(byte[] buffer, int offset, int length)
            : base(buffer, offset, length)
        {
        }

        internal static int GetHeaderAdditionalLength(int numEntries)
        {
            return numEntries * DefaultAddressEntrySize * IpV4Address.SizeOf;
        }

        internal static void WriteHeaderAdditional(byte[] buffer, int offset,
                                                   IEnumerable<IcmpRouterAdvertisementEntry> entries)
        {
            foreach (IcmpRouterAdvertisementEntry entry in entries)
            {
                buffer.Write(ref offset, entry.RouterAddress, Endianity.Big);
                buffer.Write(ref offset, entry.RouterAddressPreference, Endianity.Big);
            }
        }

        private ReadOnlyCollection<IcmpRouterAdvertisementEntry> _entries;
    }
}