namespace BSL.Models
{
    public interface IEditionService
    {
        IEnumerable<Edition> SearchByName(string name);
    }

}