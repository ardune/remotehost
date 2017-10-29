using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;

namespace RemoteHost.Messages
{
    [Serializable]
    public class CallMethodMessage : RemoteHostMessage
    {
        public string MethodName { get; set; }

        [XmlArray]
        public object[] Arguments { get; set; }

        public bool Returns { get; set; }
    }
}