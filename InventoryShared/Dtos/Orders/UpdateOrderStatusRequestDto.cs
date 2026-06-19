using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryShared.Dtos.Orders
{
    public class UpdateOrderStatusRequestDto
    {
        public string Status { get; set; } = string.Empty;

        public string RowVersion { get; set; } = string.Empty;
    }
}
