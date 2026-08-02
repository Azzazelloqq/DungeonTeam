using System;
using System.Collections.Generic;

namespace DungeonTeam.Feedback.Runtime.Audio
{
    internal enum VoiceRejectionReason
    {
        None,
        Cooldown,
        OwnerLimit,
        Capacity
    }

    internal readonly struct VoiceAllocation
    {
        private VoiceAllocation(
            bool succeeded,
            int slotIndex,
            bool replaced,
            VoiceRejectionReason rejectionReason)
        {
            Succeeded = succeeded;
            SlotIndex = slotIndex;
            Replaced = replaced;
            RejectionReason = rejectionReason;
        }

        public bool Succeeded { get; }
        public int SlotIndex { get; }
        public bool Replaced { get; }
        public VoiceRejectionReason RejectionReason { get; }

        public static VoiceAllocation Accepted(int slotIndex, bool replaced)
        {
            return new VoiceAllocation(true, slotIndex, replaced, VoiceRejectionReason.None);
        }

        public static VoiceAllocation Rejected(VoiceRejectionReason reason)
        {
            return new VoiceAllocation(false, -1, false, reason);
        }
    }

    internal sealed class VoiceAllocator
    {
        private readonly VoiceSlot[] _slots;
        private readonly Dictionary<object, double> _lastStartedAt = new();

        public VoiceAllocator(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _slots = new VoiceSlot[capacity];
        }

        public int Capacity => _slots.Length;

        public int ActiveCount { get; private set; }

        public VoiceAllocation TryAcquire(
            object owner,
            int ownerLimit,
            int priority,
            double cooldown,
            double now)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            if (ownerLimit <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ownerLimit));
            }

            if (cooldown < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(cooldown));
            }

            if (_lastStartedAt.TryGetValue(owner, out var lastStartedAt) &&
                now - lastStartedAt < cooldown)
            {
                return VoiceAllocation.Rejected(VoiceRejectionReason.Cooldown);
            }

            var ownedCount = 0;
            var freeSlot = -1;
            for (var index = 0; index < _slots.Length; index++)
            {
                ref var slot = ref _slots[index];
                if (!slot.IsOccupied)
                {
                    freeSlot = freeSlot < 0 ? index : freeSlot;
                    continue;
                }

                if (ReferenceEquals(slot.Owner, owner))
                {
                    ownedCount++;
                }
            }

            if (ownedCount >= ownerLimit)
            {
                return VoiceAllocation.Rejected(VoiceRejectionReason.OwnerLimit);
            }

            if (freeSlot >= 0)
            {
                Occupy(freeSlot, owner, priority, now, incrementCount: true);
                return VoiceAllocation.Accepted(freeSlot, replaced: false);
            }

            var victim = FindVictim(priority);
            if (victim < 0)
            {
                return VoiceAllocation.Rejected(VoiceRejectionReason.Capacity);
            }

            Occupy(victim, owner, priority, now, incrementCount: false);
            return VoiceAllocation.Accepted(victim, replaced: true);
        }

        public bool IsOwnedBy(int slotIndex, object owner)
        {
            ValidateSlotIndex(slotIndex);
            return _slots[slotIndex].IsOccupied &&
                   ReferenceEquals(_slots[slotIndex].Owner, owner);
        }

        public void Release(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            if (!_slots[slotIndex].IsOccupied)
            {
                return;
            }

            _slots[slotIndex] = default;
            ActiveCount--;
        }

        public void ForgetOwner(object owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            _lastStartedAt.Remove(owner);
        }

        public void Clear()
        {
            Array.Clear(_slots, 0, _slots.Length);
            _lastStartedAt.Clear();
            ActiveCount = 0;
        }

        private int FindVictim(int requestedPriority)
        {
            var victim = -1;
            for (var index = 0; index < _slots.Length; index++)
            {
                ref var slot = ref _slots[index];
                if (!slot.IsOccupied || slot.Priority >= requestedPriority)
                {
                    continue;
                }

                if (victim < 0 ||
                    slot.Priority < _slots[victim].Priority ||
                    slot.Priority == _slots[victim].Priority &&
                    slot.StartedAt < _slots[victim].StartedAt)
                {
                    victim = index;
                }
            }

            return victim;
        }

        private void Occupy(
            int slotIndex,
            object owner,
            int priority,
            double now,
            bool incrementCount)
        {
            _slots[slotIndex] = new VoiceSlot(owner, priority, now);
            _lastStartedAt[owner] = now;
            if (incrementCount)
            {
                ActiveCount++;
            }
        }

        private void ValidateSlotIndex(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            }
        }

        private readonly struct VoiceSlot
        {
            public VoiceSlot(object owner, int priority, double startedAt)
            {
                Owner = owner;
                Priority = priority;
                StartedAt = startedAt;
            }

            public object Owner { get; }
            public int Priority { get; }
            public double StartedAt { get; }
            public bool IsOccupied => Owner != null;
        }
    }
}
