using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iTwinProjectSampleApp.Models
    {
    internal class changeset
        {
        internal class Href
            {
            public string href { get; set; }
            };

        internal class Links
            {
            public Href download { get; set; }
            public Href currentOrPrecedingCheckpoint { get; set; }
            };

        public string id { get; set; }
        public int index { get; set; }
        public Links _links { get; set; }
        }
    }
