using System;
using System.Collections.Generic;

namespace SpectrumRepoMod.Rewards
{
    public enum RewardCategory
    {
        Monster,
        UtilityItem,
        Valuable
    }

    public sealed class RewardWeights
    {
        public double Monster { get; set; } = 1.0;
        public double UtilityItem { get; set; } = 1.0;
        public double Valuable { get; set; } = 1.0;

        public void Validate()
        {
            ValidateWeight(Monster, nameof(Monster));
            ValidateWeight(UtilityItem, nameof(UtilityItem));
            ValidateWeight(Valuable, nameof(Valuable));
            if (Monster + UtilityItem + Valuable <= 0.0)
                throw new ArgumentException("At least one reward category must have a positive weight.");
        }

        private static void ValidateWeight(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0)
                throw new ArgumentOutOfRangeException(name, "Reward weights must be finite and non-negative.");
        }
    }

    public sealed class CombinedRewardSelection<T>
    {
        public RewardCategory Category { get; }
        public T Entry { get; }

        internal CombinedRewardSelection(RewardCategory category, T entry)
        {
            Category = category;
            Entry = entry;
        }
    }

    /// <summary>
    /// Deterministic combined reward selection. The same command ID, weights and
    /// ordered candidate lists always produce the same result, making gateway
    /// retries safe without rerolling a redemption.
    /// </summary>
    public static class CombinedRewardSelector
    {
        public static CombinedRewardSelection<T> Select<T>(
            string commandId,
            IReadOnlyList<T> monsters,
            IReadOnlyList<T> utilityItems,
            IReadOnlyList<T> valuables,
            RewardWeights weights = null)
        {
            if (string.IsNullOrWhiteSpace(commandId))
                throw new ArgumentException("A stable command ID is required.", nameof(commandId));

            monsters = monsters ?? Array.Empty<T>();
            utilityItems = utilityItems ?? Array.Empty<T>();
            valuables = valuables ?? Array.Empty<T>();
            weights = weights ?? new RewardWeights();
            weights.Validate();

            var candidates = new List<CategoryCandidates<T>>(3);
            AddIfAvailable(candidates, RewardCategory.Monster, monsters, weights.Monster);
            AddIfAvailable(candidates, RewardCategory.UtilityItem, utilityItems, weights.UtilityItem);
            AddIfAvailable(candidates, RewardCategory.Valuable, valuables, weights.Valuable);

            if (candidates.Count == 0)
                throw new InvalidOperationException("No eligible R.E.P.O. reward entries are currently available.");

            var random = new StableRandom(CreateSeed(commandId));
            double totalWeight = 0.0;
            foreach (var candidate in candidates)
                totalWeight += candidate.Weight;

            double roll = random.NextUnitDouble() * totalWeight;
            CategoryCandidates<T> selectedCategory = candidates[candidates.Count - 1];
            foreach (var candidate in candidates)
            {
                roll -= candidate.Weight;
                if (roll < 0.0)
                {
                    selectedCategory = candidate;
                    break;
                }
            }

            int index = random.NextInt(selectedCategory.Entries.Count);
            return new CombinedRewardSelection<T>(selectedCategory.Category, selectedCategory.Entries[index]);
        }

        private static void AddIfAvailable<T>(
            List<CategoryCandidates<T>> target,
            RewardCategory category,
            IReadOnlyList<T> entries,
            double weight)
        {
            if (entries.Count > 0 && weight > 0.0)
                target.Add(new CategoryCandidates<T>(category, entries, weight));
        }

        private static uint CreateSeed(string value)
        {
            // FNV-1a over UTF-16 code units; independent of process-randomized string hashing.
            uint hash = 2166136261;
            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= 16777619;
            }
            return hash == 0 ? 0x9E3779B9u : hash;
        }

        private sealed class CategoryCandidates<T>
        {
            public RewardCategory Category { get; }
            public IReadOnlyList<T> Entries { get; }
            public double Weight { get; }

            public CategoryCandidates(RewardCategory category, IReadOnlyList<T> entries, double weight)
            {
                Category = category;
                Entries = entries;
                Weight = weight;
            }
        }

        private sealed class StableRandom
        {
            private uint _state;

            public StableRandom(uint seed)
            {
                _state = seed == 0 ? 0x9E3779B9u : seed;
            }

            private uint NextUInt()
            {
                // xorshift32: compact deterministic PRNG for command-id keyed selection.
                uint value = _state;
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                _state = value;
                return value;
            }

            public double NextUnitDouble()
            {
                return (NextUInt() & 0x00FFFFFFu) / 16777216.0;
            }

            public int NextInt(int exclusiveMaximum)
            {
                if (exclusiveMaximum <= 0)
                    throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));
                return (int)(NextUnitDouble() * exclusiveMaximum);
            }
        }
    }
}
