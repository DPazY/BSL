using System;
using System.Collections.Generic;
using System.Text;

namespace BSL.Models
{
    public record class Newspaper : Editions
    {
        public Newspaper(string Name) : base(Name)
        {
        }
    }
}
