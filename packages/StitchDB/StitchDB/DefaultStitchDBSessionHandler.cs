using StitchDB.Gotrue;
using StitchDB.Gotrue.Interfaces;

namespace StitchDB
{
    /// <summary>
    /// Represents the default session handler for Gotrue - it does nothing by default.
    /// </summary>
    public class DefaultStitchDBSessionHandler : IGotrueSessionPersistence<Session>
    {
        /// <summary>
        /// Default Session Save (does nothing by default)
        /// </summary>
        /// <param name="session"></param>
        public void SaveSession(Session session)
        {
        }

        /// <summary>
        /// Default Session Destroyer (does nothing by default)
        /// </summary>
        public void DestroySession()
        {
        }

        /// <summary>
        /// Default Session Loader (does nothing by default)
        /// </summary>
        /// <returns></returns>
        public Session? LoadSession()
        {
            return null;
        }
    }
}