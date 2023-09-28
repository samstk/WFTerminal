using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WFTerminal
{
    /// <summary>
    /// A terminal stream writer, which can be used
    /// to redirect the console's standard output to a terminal
    /// </summary>
    internal class TerminalStreamWriter : TextWriter
    {
        /// <summary>
        /// Gets the terminal that is being written to.
        /// </summary>
        public Terminal Terminal { get; private set; }

        /// <summary>
        /// Creates the stream writer with the given terminal.
        /// </summary>
        /// <param name="terminal"></param>
        public TerminalStreamWriter(Terminal terminal)
        {
            Terminal = terminal;
        }

        public override void Write(char value)
        {
            // base.Write(value);
            Terminal.Write(value.ToString());
        }

        public override void Write(string? value)
        {
            // base.Write(value);
            if (value != null)
                Terminal.Write(value);
        }

        public override Encoding Encoding => Encoding.ASCII;
    }
}
