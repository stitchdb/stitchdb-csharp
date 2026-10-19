using System.Threading.Tasks;
using StitchDB.Realtime.Interfaces;
using StitchDB.Postgrest.Interfaces;
using StitchDB.Postgrest.Models;
using static StitchDB.Realtime.PostgresChanges.PostgresChangesOptions;

namespace StitchDB.Interfaces
{
    /// <summary>
    /// Contract representing a stitchdb wrapped postgrest <see cref="IPostgrestTable{TModel}"/>
    /// </summary>
    /// <typeparam name="TModel">Model that inherits from <see cref="BaseModel"/> that represents this Table</typeparam>
    /// <typeparam name="TChannel">Class that implements <see cref="IRealtimeChannel"/></typeparam>
    public interface IStitchDBTable<TModel, TChannel> : IPostgrestTable<TModel>
        where TModel : BaseModel, new()
        where TChannel : IRealtimeChannel
    {
        /// <summary>
        /// Add a realtime listener to this table.
        /// </summary>
        /// <param name="listenType"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        Task<TChannel> On(ListenType listenType, IRealtimeChannel.PostgresChangesHandler handler);
    }
}