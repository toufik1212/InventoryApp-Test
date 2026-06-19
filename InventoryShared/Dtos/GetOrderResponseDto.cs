using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryShared.Dtos
{
    public class GetOrderResponseDto
    {
        public long Id { get; set; }

        public int CustomerId { get; set; }

        public string ShippingAddress { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; }
        public string RowVersion { get; set; } = string.Empty;
        public List<GetOrderItemDto> Items { get; set; } = [];
    }
}
