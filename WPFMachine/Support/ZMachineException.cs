using System;

namespace WPFMachine
{
    class ZMachineException : Exception
    {
        public ZMachineException() { }

        public ZMachineException(string message) : base(message) { }

        public ZMachineException(string message, Exception innerException) : base(message, innerException) { }
    }
}
