namespace BSL.Models
{
    public interface IEditionService
    {
        public IEnumerable<Editions> SearchByName(string name);
    }

}