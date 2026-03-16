using BSL.App.Commands;
using McMaster.Extensions.CommandLineUtils;

namespace BSL.App
{

    [Subcommand(typeof(List))]
    public class App
    {
        public void OnExecute(CommandLineApplication app)
            => app.ShowHelp();
    }
}
