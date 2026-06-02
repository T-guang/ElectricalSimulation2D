using System.Collections.Generic;
using System.Linq;

namespace ElectricalSim.Practice.Netlist
{
    public sealed class UnionFind<T>
    {
        private readonly Dictionary<T, T> parent = new Dictionary<T, T>();

        public void Add(T item)
        {
            if (!parent.ContainsKey(item))
            {
                parent[item] = item;
            }
        }

        public T Find(T item)
        {
            if (!parent.ContainsKey(item))
            {
                Add(item);
            }

            if (EqualityComparer<T>.Default.Equals(parent[item], item))
            {
                return item;
            }

            parent[item] = Find(parent[item]);
            return parent[item];
        }

        public void Union(T first, T second)
        {
            Add(first);
            Add(second);

            var firstRoot = Find(first);
            var secondRoot = Find(second);
            if (!EqualityComparer<T>.Default.Equals(firstRoot, secondRoot))
            {
                parent[firstRoot] = secondRoot;
            }
        }

        public bool AreConnected(T first, T second)
        {
            if (!parent.ContainsKey(first) || !parent.ContainsKey(second))
            {
                return false;
            }

            return EqualityComparer<T>.Default.Equals(Find(first), Find(second));
        }

        public IReadOnlyList<IReadOnlyList<T>> GetGroups()
        {
            var groups = new Dictionary<T, List<T>>();
            foreach (var item in parent.Keys.ToList())
            {
                var root = Find(item);
                if (!groups.TryGetValue(root, out var group))
                {
                    group = new List<T>();
                    groups[root] = group;
                }

                group.Add(item);
            }

            return groups.Values.Select(g => (IReadOnlyList<T>)g).ToList();
        }
    }
}
