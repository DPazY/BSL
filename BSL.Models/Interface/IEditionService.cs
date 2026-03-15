namespace BSL.Models.Interface
{
    public interface IEditionService
    {
        IEnumerable<Edition> SearchByName(string name);
    }

}