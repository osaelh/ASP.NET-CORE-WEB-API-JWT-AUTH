using System;
using System.Collections.Generic;
using System.Text;

namespace Entities
{
    public class Command
    {
        public Guid Id { get; set; }
        public Client client { get; set; }
        public List<Product> products { get; set; }

    }
}
