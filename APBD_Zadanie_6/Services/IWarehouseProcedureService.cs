using APBD_Task_6.Models;
using System.Net.Http.Headers;

namespace APBD_Zadanie_6.Services
{
    public interface IWarehouseProcedureService
    {

        Task<int> AddProductToWarehouse(ProductWarehouse productWarehouse);
    }
}
