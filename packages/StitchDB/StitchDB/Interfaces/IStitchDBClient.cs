using System.Threading.Tasks;
using StitchDB.Functions.Interfaces;
using StitchDB.Gotrue;
using StitchDB.Gotrue.Interfaces;
using StitchDB.Postgrest.Interfaces;
using StitchDB.Postgrest.Models;
using StitchDB.Postgrest.Responses;
using StitchDB.Realtime.Interfaces;
using StitchDB.Storage;
using StitchDB.Storage.Interfaces;

namespace StitchDB.Interfaces;

/// <summary>
/// Contract for what a StitchDBClient should implement
/// </summary>
/// <typeparam name="TUser">Model representing User</typeparam>
/// <typeparam name="TSession">Model representing Session</typeparam>
/// <typeparam name="TSocket">Class that conforms to <see cref="IRealtimeSocket"/></typeparam>
/// <typeparam name="TChannel">Class that conforms to <see cref="IRealtimeChannel"/></typeparam>
/// <typeparam name="TBucket">Model representing <see cref="Bucket"/></typeparam>
/// <typeparam name="TFileObject">Model representing <see cref="FileObject"/></typeparam>
public interface IStitchDBClient<TUser, TSession, TSocket, TChannel, TBucket, TFileObject>
    where TUser : User
    where TSession : Session
    where TSocket : IRealtimeSocket
    where TChannel : IRealtimeChannel
    where TBucket : Bucket
    where TFileObject : FileObject
{
    /// <summary>
    /// The Gotrue Auth Instance
    /// </summary>
    IGotrueClient<TUser, TSession> Auth { get; set; }

    /// <summary>
    /// Creates a Gotrue Admin Auth Client
    /// </summary>
    /// <param name="serviceKey"></param>
    /// <returns></returns>
    IGotrueAdminClient<User> AdminAuth(string serviceKey);

    /// <summary>
    /// The StitchDB Functions Client
    /// </summary>
    IFunctionsClient Functions { get; set; }

    /// <summary>
    /// The Postgrest Client
    /// </summary>
    IPostgrestClient Postgrest { get; set; }

    /// <summary>
    /// The Realtime Client
    /// </summary>
    IRealtimeClient<TSocket, TChannel> Realtime { get; set; }

    /// <summary>
    /// The Storage Client
    /// </summary>
    IStorageClient<TBucket, TFileObject> Storage { get; set; }

    /// <summary>
    /// Used for interacting with a Postgrest Table + Model. Provides helpers
    /// to be able to add realtime listeners and queries. 
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    /// <returns></returns>
    IStitchDBTable<TModel, TChannel> From<TModel>() where TModel : BaseModel, new();


    /// <summary>
    /// Initializes a stitchdb client according to the provided <see cref="StitchDBOptions"/>.
    /// If option is enabled:
    /// - Will connect to realtime instance <see cref="StitchDBOptions.AutoConnectRealtime"/>
    /// - Will restore session using a <see cref="IGotrueSessionPersistence{TSession}"/> specified in <see cref="StitchDBOptions.SessionHandler"/>
    /// </summary>
    /// <returns></returns>
    Task<IStitchDBClient<TUser, TSession, TSocket, TChannel, TBucket, TFileObject>> InitializeAsync();

    /// <summary>
    /// Perform a stored procedure call.
    /// </summary>
    /// <param name="procedureName">The function name to call</param>
    /// <param name="parameters">The parameters to pass to the function call</param>
    /// <returns></returns>
    Task<BaseResponse> Rpc(string procedureName, object? parameters);

    /// <summary>
    /// Perform a stored procedure call.
    /// </summary>
    /// <param name="procedureName">The function name to call</param>
    /// <param name="parameters">The parameters to pass to the function call</param>
    /// <typeparam name="TModeledResponse">A type used for hydrating the HTTP response content (hydration through System.Text.Json)</typeparam>
    /// <returns>A hydrated model</returns>
    Task<TModeledResponse?> Rpc<TModeledResponse>(string procedureName, object? parameters);
}
