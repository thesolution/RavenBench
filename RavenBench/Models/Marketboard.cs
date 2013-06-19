using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RavenBench.Models
{
    public enum MarketboardType
    {
        User,
        Cash,
        Margin
    }

    public class Marketboard
    {
        public string Name { get; set; }
        public IList<MarketboardSymbol> Symbols { get; set; }
        public bool IsSelected { get; set; }
        public string UserId { get; set; }
        public int RowCount { get; set; }
        public int ColumnCount { get; set; }
        public MarketboardType Type { get; set; }

        public bool IsPosition { get { return Type == MarketboardType.Cash || Type == MarketboardType.Margin; } }

        public int FirstOpenPosition
        {
            get
            {
                var sorted = this.Symbols.OrderBy(f => f.Position);
                var lastPosition = -1;
                foreach (var symbol in sorted)
                {
                    if (symbol.Position - lastPosition > 1)
                    {
                        return lastPosition + 1;
                    }
                    lastPosition = symbol.Position;
                }
                return lastPosition + 1;
            }
        }

        public bool IsFull
        {
            get
            {
                var openings = Enumerable.Range(0, this.RowCount * this.ColumnCount)
                    .Except(this.Symbols.Select(s => s.Position));
                return openings.Count() == 0;
            }
        }

        public Marketboard()
        {
            Type = MarketboardType.User;
            Symbols = new List<MarketboardSymbol>();
        }

        public MarketboardSymbol AddSymbol(MarketboardSymbol symbol, int position = -1)
        {
            if (symbol == null)
                throw new ArgumentNullException("symbol");

            // Validate position
            if (position >= 0)
            {
                foreach (MarketboardSymbol s in this.Symbols)
                {
                    if (s.Position == position)
                    {
                        throw new InvalidOperationException(String.Format("Cannot add a symbol to position {0} because it is not empty.", position));
                    }
                }
            }
            symbol.Position = (position < 0 ? this.FirstOpenPosition : position);
            this.Symbols.Add(symbol);
            return symbol;
        }

        public IList<MarketboardSymbol> RemoveSymbol(int position)
        {
            IList<MarketboardSymbol> removed = new List<MarketboardSymbol>();
            for (int i = 0; i < this.Symbols.Count; i++)
            {
                var symbol = this.Symbols[i];
                if (symbol.Position == position)
                {
                    if (this.Symbols.Remove(symbol))
                    {
                        removed.Add(symbol);
                        i--;
                    }
                }
            }
            return removed;
        }


    }
}
