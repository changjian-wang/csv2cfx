using CSV2CFX.Interfaces;
using RabbitMQ.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CSV2CFX.Services
{
    public class RabbitMQConnectionFactory : IRabbitMQConnectionFactory, IAsyncDisposable
    {
        private readonly IConnectionFactory _connectionFactory;
        private IConnection? _connection;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private bool _disposed = false;

        public RabbitMQConnectionFactory(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task<IConnection> CreateConnectionAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RabbitMQConnectionFactory));

            if (_connection == null || !_connection.IsOpen)
            {
                await _semaphore.WaitAsync();
                try
                {
                    if (_connection == null || !_connection.IsOpen)
                    {
                        _connection?.Dispose();
                        _connection = await _connectionFactory.CreateConnectionAsync();
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            return _connection;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;

            await _semaphore.WaitAsync();
            try
            {
                if (_connection != null)
                {
                    await _connection.CloseAsync();
                    _connection.Dispose();
                    _connection = null;
                }
            }
            finally
            {
                _semaphore.Release();
                _semaphore.Dispose();
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}