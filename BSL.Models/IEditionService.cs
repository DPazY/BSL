namespace BSL.Models
{
    public interface IEditionService
    {
        public IEnumerable<Edition> SearchByName(string name);
    }

}