using System;
using System.Collections.Generic;
using System.Text;

namespace Entities
{
    public class Product
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public List<Image> ProductImages { get; set; }
        public List<Command> commands { get; set; }
        public Category ProductCategory { get; set; }
    }
}
