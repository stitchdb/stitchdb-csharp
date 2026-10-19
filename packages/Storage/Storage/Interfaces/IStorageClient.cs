using StitchDB.Core.Interfaces;

namespace StitchDB.Storage.Interfaces
{
    public interface IStorageClient<TBucket, TFileObject> : IStorageBucketApi<TBucket>, IGettableHeaders
        where TBucket : Bucket
        where TFileObject : FileObject
    {
        IStorageFileApi<TFileObject> From(string id);
    }
}