using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Core.Models
{
    public sealed record Dish(string Name, string Price, string Weight, string ImageUrl);
}
