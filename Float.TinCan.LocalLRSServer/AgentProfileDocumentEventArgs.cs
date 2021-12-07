using System;
using TinCan.Documents;

namespace Float.TinCan.LocalLRSServer
{
    /// <summary>
    /// Agent profile event arguments.
    /// </summary>
    public class AgentProfileDocumentEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AgentProfileDocumentEventArgs"/> class.
        /// </summary>
        /// <param name="agentProfileDocument">Agent profile document.</param>
        public AgentProfileDocumentEventArgs(AgentProfileDocument agentProfileDocument)
        {
            AgentProfileDocument = agentProfileDocument ?? throw new ArgumentNullException(nameof(agentProfileDocument));
        }

        /// <summary>
        /// Gets the agent profile document.
        /// </summary>
        /// <value>The agent profile document.</value>
        public AgentProfileDocument AgentProfileDocument { get; }

        /// <summary>
        /// Returns a <see cref="string"/> that represents the current <see cref="AgentProfileDocumentEventArgs"/>.
        /// </summary>
        /// <returns>A <see cref="string"/> that represents the current <see cref="AgentProfileDocumentEventArgs"/>.</returns>
        public override string ToString()
        {
            return $"[AgentProfileEventArgs: AgentProfileDocument={AgentProfileDocument}]";
        }
    }
}
