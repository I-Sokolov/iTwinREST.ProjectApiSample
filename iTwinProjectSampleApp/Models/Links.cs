using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static iTwinProjectSampleApp.Models.changeset;

namespace iTwinProjectSampleApp.Models
    {
    internal class Links
        {
        internal class Href
            {
            public string href { get; set; }
            };

        public Href download { get; set; }
        public Href currentOrPrecedingCheckpoint { get; set; }
        };
    }
