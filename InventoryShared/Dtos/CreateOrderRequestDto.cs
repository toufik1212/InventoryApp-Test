using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryShared.Dtos
{
    public class CreateOrderRequestDto
    {
        public int CustomerId { get; set; }

        public string ShippingAddress { get; set; } = string.Empty;

        public List<CreateOrderItemDto> Items { get; set; } = [];
    }
}
