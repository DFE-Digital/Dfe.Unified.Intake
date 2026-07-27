using Microsoft.AspNetCore.Http;

namespace Dfe.Unified.Intake.Tests.Support
{
    public sealed class FakeSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public FakeSession(string id = "test-session")
        {
            Id = id;
        }

        public bool IsAvailable => true;

        public string Id { get; }

        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => _store.Remove(key);

        public void Set(string key, byte[] value) => _store[key] = value;

        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
    }
}
