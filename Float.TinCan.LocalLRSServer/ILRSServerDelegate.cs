using System.Net;
using TinCan.Documents;

namespace Float.TinCan.LocalLRSServer
{
    /// <summary>
    /// Defines an object that can create custom responses for a local LRS server to return.
    /// </summary>
    public interface ILRSServerDelegate
    {
        /// <summary>
        /// Create a response for a single profile ID request.
        /// </summary>
        /// <returns>The profile document for the given profile identifier.</returns>
        /// <param name="profileId">Profile identifier.</param>
        AgentProfileDocument AgentProfileDocumentForProfileId(string profileId);

        /// <summary>
        /// Gets the allowed origins for the LRS server.
        /// If null or not implemented this will default to file://.
        /// </summary>
        /// <returns>The header value for Access-Control-Allow-Origin.</returns>
        string GetAccessConrolAllowOrigin();

        /// <summary>
        /// Delegate method to alter a response for AgentProfile.
        /// </summary>
        /// <param name="request">The HTTP request to get query params from.</param>
        /// <param name="response">The current HTTP response.</param>
        /// <param name="profileDocument">The profile document.</param>
        public void AlterAgentProfileResponse(HttpListenerRequest request, ref HttpListenerResponse response, ref AgentProfileDocument profileDocument);
    }
}
