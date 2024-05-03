using APBD_Task_6.Models;
using Microsoft.AspNetCore.Connections;
using System.Data.SqlClient;
using APBD_Zadanie_6.Exceptions;

namespace Zadanie5.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IConfiguration _configuration;

        public WarehouseService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<int> AddProduct(ProductWarehouse productWarehouse)
        {
            var connectionString = _configuration.GetConnectionString("Database");
            using var connection = new SqlConnection(connectionString);
            using var cmd = new SqlCommand();

            cmd.Connection = connection;

            await connection.OpenAsync();

            cmd.CommandText = "SELECT TOP 1 [Order].IdOrder FROM [Order] " +
                "LEFT JOIN Product_Warehouse ON [Order].IdOrder = Product_Warehouse.IdOrder " +
                "WHERE [Order].IdProduct = @IdProduct " +
                "AND [Order].Amount = @Amount " + 
                "AND Product_Warehouse.IdProductWarehouse IS NULL " +
                "AND [Order].CreatedAt < @CreatedAt";

            cmd.Parameters.AddWithValue("IdProduct", productWarehouse.IdProduct);
            cmd.Parameters.AddWithValue("Amount", productWarehouse.Amount);
            cmd.Parameters.AddWithValue("CreatedAt", productWarehouse.CreatedAt);

            var reader = await cmd.ExecuteReaderAsync();

            if (!reader.HasRows) throw new NotFoundException("Conditions not fulfilled"); //najlepiej customowy
            //jesli ma wiersze to bedziemy sobie sczytywac
            await reader.ReadAsync();

            int idOrder = int.Parse(reader["IdOrder"].ToString()); //ta wartosc jest wykorzystana pozniej
            await reader.CloseAsync();

            cmd.Parameters.Clear();

            //potrzebna jest cena do dalszej czesci zadania
            cmd.CommandText = "SELECT Price FROM Product WHERE IdProduct = @IdProduct";
            cmd.Parameters.AddWithValue("IdProduct",productWarehouse.IdProduct);

            reader = await cmd.ExecuteReaderAsync();

            if (!reader.HasRows) throw new NotFoundException("There is no product with given price in database");

            await reader.ReadAsync();
            double price = double.Parse(reader["Price"].ToString()); //ta wartosc jest wykorzystana pozniej
            await reader.CloseAsync();

            cmd.Parameters.Clear();

            //tresc zadania - sprawdzamy, czy magazyn o podanym identyfikatorze istnieje
            cmd.CommandText = "SELECT IdWarehouse FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
            cmd.Parameters.AddWithValue("IdWarehouse",productWarehouse.IdWarehouse);

            reader = await cmd.ExecuteReaderAsync();

            if (!reader.HasRows) throw new NotFoundException("There is no warehouse with given id in database");

            await reader.CloseAsync();

            cmd.Parameters.Clear();

            //rozpoczecie transakcji
            var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
            cmd.Transaction = transaction;

            try
            {
                cmd.CommandText = "UPDATE [Order] SET FulfilledAt = @CreatedAt WHERE IdOrder = @IdOrder";
                cmd.Parameters.AddWithValue("CreatedAt", productWarehouse.CreatedAt);
                cmd.Parameters.AddWithValue("IdOrder", idOrder); //wykorzytanie wcześniej zdefiniowanej zmiennej idOrder

                int rowsUpdated = await cmd.ExecuteNonQueryAsync();// X rows affected

                if(rowsUpdated < 1) throw new NoResultException();

                cmd.Parameters.Clear();

                cmd.CommandText = "INSERT INTO Product_Warehouse(IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt) " +
                    $"VALUES(@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Amount*{price}, @CreatedAt)";//wykorzytanie wcześniej zdefiniowanej zmiennej price
                cmd.Parameters.AddWithValue("IdWarehouse", productWarehouse.IdWarehouse);
                cmd.Parameters.AddWithValue("IdProduct", productWarehouse.IdProduct); 
                cmd.Parameters.AddWithValue("IdOrder", idOrder);//wykorzytanie wcześniej zdefiniowanej zmiennej idOrder
                cmd.Parameters.AddWithValue("Amount", productWarehouse.Amount);
                cmd.Parameters.AddWithValue("CreatedAt", productWarehouse.CreatedAt);

                int rowsInserted = await cmd.ExecuteNonQueryAsync();

                if (rowsInserted < 1) throw new NoResultException();

                cmd.Parameters.Clear();
                await transaction.CommitAsync();

            }
            catch (NoResultException)
            {
                await transaction.RollbackAsync();//wycofa zmiany (transakcje) jesli bedzie blad
                throw new TransactionInterruptedException("Something went wrong. Transaction interrupted.");
            }


            cmd.Parameters.Clear();

            cmd.CommandText = "SELECT TOP 1 IdProductWarehouse FROM Product_Warehouse ORDER BY IdProductWarehouse DESC";

            reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            int idProductWarehouse = int.Parse(reader["IdProductWarehouse"].ToString());

            await reader.CloseAsync();
            await connection.CloseAsync();

            return idProductWarehouse;
           
        }
    }
}
