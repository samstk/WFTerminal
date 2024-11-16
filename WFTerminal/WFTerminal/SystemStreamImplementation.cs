using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WFTerminal
{
    public delegate string SystemStreamReadToEnd<StreamType>(StreamType stream);
    public delegate void SystemStreamWrite<StreamType>(StreamType stream, byte[] content);
    public delegate void SystemStreamSeek<StreamType>(StreamType stream, int position);

    /// <summary>
    /// An implementation for a system stream within an external package.
    /// </summary>
    /// <remarks>
    /// It is necessary to map all the different functions for use with this terminal.
    /// </remarks>
    public sealed class SystemStreamImplementation<StreamType>
        where StreamType : Stream
    {
        private SystemStreamReadToEnd<StreamType> _ReadToEnd;
        private SystemStreamWrite<StreamType> _Write;
        private SystemStreamSeek<StreamType> _Seek;

        public SystemStreamImplementation(SystemStreamReadToEnd<StreamType> readToEnd, SystemStreamWrite<StreamType> write, SystemStreamSeek<StreamType> seek)
        {
            _ReadToEnd = readToEnd;
            _Write = write;
            _Seek = seek;
        }

        public void Seek(StreamType stream, int position)
        {
            _Seek(stream, position);
        }

        public string ReadToEnd(StreamType stream)
        {
            return _ReadToEnd(stream);
        }

        public void Write(StreamType stream, byte[] content)
        {
            _Write(stream, content);
        } 
    }
}
