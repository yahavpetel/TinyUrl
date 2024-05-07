namespace TinyUrl.Services
{
    public class LRUCache<TKey, TValue>
    {
        private readonly int _capacity;
        private readonly Dictionary<TKey, LinkedListNode<(TKey, TValue)>> _cache;
        private readonly LinkedList<(TKey, TValue)> _list;

        public LRUCache(int capacity)
        {
            _capacity = capacity;
            _cache = new Dictionary<TKey, LinkedListNode<(TKey, TValue)>>();
            _list = new LinkedList<(TKey, TValue)>();
        }

        public void Add(TKey key, TValue value)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                _list.Remove(node);
                _list.AddFirst(node);
                node.Value = (key, value);
            }
            else
            {
                if (_cache.Count >= _capacity)
                {
                    var last = _list.Last;
                    _cache.Remove(last.Value.Item1);
                    _list.RemoveLast();
                }
                var newNode = _list.AddFirst((key, value));
                _cache[key] = newNode;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                value = node.Value.Item2;
                _list.Remove(node);
                _list.AddFirst(node);
                return true;
            }
            value = default(TValue);
            return false;
        }
    }

}
