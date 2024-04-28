using APBD_Task_6.Models;
using APBD_Zadanie_6.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using Zadanie5.Services;

namespace APBD_Zadanie_6.Controllers
{

    [Route("api/[warehouse2]")]
    [ApiController]
    public class WarehouseProcedureController : ControllerBase
    {
        private readonly IWarehouseProcedureService _warehouseProcedureService;
      
        public WarehouseProcedureController(IWarehouseProcedureService warehouseProcedureService) {
            _warehouseProcedureService = warehouseProcedureService;
        }
        [HttpPost]
        public async Task<IActionResult> AddProductToWarehouse(ProductWarehouse productWarehouse) {
            int idProductWarehouse = await _warehouseProcedureService.AddProductToWarehouse(productWarehouse);
            return Ok();
        }

    }
}
