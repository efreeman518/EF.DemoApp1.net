namespace Package.Infrastructure.Data.Contracts;
public enum OptimisticConcurrencyWinner
{
    ClientWins,
    DBWins,
    Throw
}
