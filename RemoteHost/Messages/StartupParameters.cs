using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace RemoteHost.Messages
{

    [Serializable]
    [XmlRoot("StartupParameters")]
    public sealed class StartupParameters : List<StartupParameters.StringPair>
    {
        public const string UniqueIdentifier = "UniqueIdentifier";
        public const string MessageSerializer = "MessageSerializer";
        public const string HostedType = "HostedType";

        public void Add(string key, string value)
        {
            Add(new StringPair(key, value));
        }

        public string this[string key]
        {
            get { return this.Where(x=>x.Key == key).Select(x=>x.Value).FirstOrDefault(); }
        }

        public string Serialize()
        {
            var test = new XmlSerializer(typeof(StartupParameters));
            using (var stream = new MemoryStream())
            {
                test.Serialize(stream, this);
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        [Serializable]
        [XmlType(TypeName = "StringPair")]
        public struct StringPair
        {
            public StringPair(string key, string value) : this()
            {
                Key = key;
                Value = value;
            }

            public string Key { get; set; }

            public string Value { get; set; }
        }
    }
}
