using System;
using TinCan;

namespace Float.TinCan.LocalLRSServer
{
    /// <summary>
    /// Statement event arguments.
    /// </summary>
    public class StatementEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StatementEventArgs"/> class.
        /// </summary>
        /// <param name="statement">The statement related to this event.</param>
        public StatementEventArgs(Statement statement)
        {
            Statement = statement ?? throw new ArgumentNullException(nameof(statement));
        }

        /// <summary>
        /// Gets the statement related to this event.
        /// </summary>
        /// <value>The event statement.</value>
        public Statement Statement { get; }

        /// <summary>
        /// Returns a <see cref="string"/> that represents the current <see cref="StatementEventArgs"/>.
        /// </summary>
        /// <returns>A <see cref="string"/> that represents the current <see cref="StatementEventArgs"/>.</returns>
        public override string ToString()
        {
            return $"[StatementEventArgs: Statement={Statement}]";
        }
    }
}
