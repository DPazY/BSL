namespace BSL.Models.Interface
{
    public interface IEditionService
    {
        Task<IEnumerable<Edition>> SearchByName(string name);
    }

}