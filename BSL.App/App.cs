using BSL.App.Commands;
using McMaster.Extensions.CommandLineUtils;

namespace BSL.App
{

    [Subcommand(typeof(List), typeof(Dump), typeof(Upload))]
    public class App
    {
        public void OnExecute(CommandLineApplication app)
            => app.ShowHelp();
    }
}
