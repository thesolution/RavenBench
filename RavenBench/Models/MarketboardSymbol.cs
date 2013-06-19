using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RavenBench.Models
{
    public class MarketboardSymbol
    {
        [Raven.Imports.Newtonsoft.Json.JsonIgnore]
        public string MarketboardId { get; set; }

        public string Symbol { get; set; }
        public int Position { get; set; }

        [Raven.Imports.Newtonsoft.Json.JsonIgnore]
        public ulong Revision { get; set; }

        public override bool Equals(object obj)
        {
            var symbol = obj as MarketboardSymbol;
            return (symbol != null && symbol.Symbol == this.Symbol && symbol.Position == this.Position);
        }

        public override int GetHashCode()
        {
            return String.Format("{0}:{1}", Position, Symbol).GetHashCode();
        }

    }
}
