using System;
using System.Collections.Generic;

namespace publickeyserver
{
    public class CreateKey
    {
        public string key { get; set; }

        public List<String> servers { get; set; }

        public List<String> data { get; set; }
    }
}