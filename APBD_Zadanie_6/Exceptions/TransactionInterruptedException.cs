namespace APBD_Zadanie_6.Exceptions
{
    public class TransactionInterruptedException : Exception
    {
        public TransactionInterruptedException() { }

        public TransactionInterruptedException(string message) : base(message) { }

    }
}
