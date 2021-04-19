﻿using MongoDB.Driver;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Liquid.Repository.MongoDb.Tests.Mock
{
    [ExcludeFromCodeCoverage]
    class AsyncCursorMock<TDocument> : IAsyncCursor<TDocument>
    {
        public IEnumerable<TDocument> Current { get; set; }

        public AsyncCursorMock(IEnumerable<TDocument> documents)
        {
            Current = documents;
        }

        public void Dispose()
        {
            Current.GetEnumerator().Dispose();
        }

        public bool MoveNext(CancellationToken cancellationToken = default)
        {
            return Current.GetEnumerator().MoveNext();
        }

        public async Task<bool> MoveNextAsync(CancellationToken cancellationToken = default)
        {
            return Current.GetEnumerator().MoveNext();
        }
    }
}
