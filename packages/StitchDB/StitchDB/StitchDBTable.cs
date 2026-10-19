using System.Collections.Generic;
using System.Threading.Tasks;
using StitchDB.Postgrest;
using StitchDB.Postgrest.Interfaces;
using StitchDB.Postgrest.Models;
using StitchDB.Interfaces;
using StitchDB.Realtime;
using StitchDB.Realtime.Interfaces;
using static StitchDB.Realtime.PostgresChanges.PostgresChangesOptions;

namespace StitchDB
{
    /// <summary>
    /// A StitchDB wrapper for a Postgrest Table.
    /// </summary>
    /// <typeparam name="TModel">Model that implements <see cref="BaseModel"/></typeparam>
    public class StitchDBTable<TModel> : Table<TModel>, IStitchDBTable<TModel, RealtimeChannel>
        where TModel : BaseModel, new()
    {
        private RealtimeChannel? _channel;
        private readonly IPostgrestClient _postgrestClient;
        private readonly IRealtimeClient<RealtimeSocket, RealtimeChannel> _realtimeClient;
        private readonly string _schema;

        /// <summary>
        /// A StitchDB wrapper for a Postgrest table.
        /// </summary>
        /// <param name="postgrestClient"></param>
        /// <param name="realtimeClient"></param>
        /// <param name="schema"></param>
        public StitchDBTable(IPostgrestClient postgrestClient,
            IRealtimeClient<RealtimeSocket, RealtimeChannel> realtimeClient, string schema = "public") : base(
            postgrestClient.BaseUrl, Postgrest.Client.SerializerSettings(postgrestClient.Options),
            postgrestClient.Options)
        {
            _postgrestClient = postgrestClient;
            _realtimeClient = realtimeClient;
            _schema = schema;
            GetHeaders = postgrestClient.GetHeaders;
        }

        /// <inheritdoc />
        public async Task<RealtimeChannel> On(ListenType listenType, IRealtimeChannel.PostgresChangesHandler handler)
        {
            if (_channel == null)
            {
                var parameters = new Dictionary<string, string>();

                // Matches the upstream client behaviour.
                var headers = _postgrestClient?.GetHeaders?.Invoke();
                if (headers != null && headers.TryGetValue("Authorization", out var header))
                {
                    parameters.Add("user_token", header.Split(' ')[1]);
                }

                _channel = _realtimeClient.Channel("realtime", _schema, TableName, parameters: parameters);
            }

            if (_realtimeClient.Socket == null || !_realtimeClient.Socket.IsConnected)
                await _realtimeClient.ConnectAsync();

            _channel.AddPostgresChangeHandler(listenType, handler);

            await _channel.Subscribe();
            return _channel;
        }
    }
}